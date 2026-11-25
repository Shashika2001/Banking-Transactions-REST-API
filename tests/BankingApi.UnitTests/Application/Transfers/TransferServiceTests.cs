using BankingApi.Application.Auditing;
using BankingApi.Application.Common.Exceptions;
using BankingApi.Application.Transfers;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;
using BankingApi.Domain.Exceptions;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Options;
using Moq;

namespace BankingApi.UnitTests.Application.Transfers;

public class TransferServiceTests
{
    private const string Key = "7c9e6679-7425-40de-944b-e07fc1f90ae7";
    private const decimal DailyLimit = 5_000m;

    private readonly ApplicationTestContext _context = new();
    private readonly Account _source = ApplicationTestContext.CreateAccount(balance: 1_000m);
    private readonly Account _destination = ApplicationTestContext.CreateAccount();

    public TransferServiceTests()
    {
        _context.AddAccount(_source);
        _context.AddAccount(_destination);
        _context.RunTransactionsInline<TransferResponse>();
    }

    private TransferService CreateService() =>
        new(
            _context.UnitOfWork.Object,
            _context.AuditTrail.Object,
            _context.AccessPolicy,
            _context.TimeProvider.Object,
            new TransferRequestValidator(),
            Options.Create(new TransferOptions { DailyLimitPerAccount = DailyLimit }));

    private TransferRequest Request(decimal amount = 100m, string? reference = "Rent") =>
        new(_source.Id, _destination.Id, amount, reference);

    private void SetTransferredToday(decimal amount) =>
        _context.Transactions
            .Setup(repository => repository.GetTotalAmountAsync(
                _source.Id,
                TransactionType.TransferOut,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(amount);

    // Simulates a transfer already stored under Key, and returns its legs.
    private TransferLegs ArrangeEarlierTransfer(decimal amount = 100m)
    {
        var legs = _source.TransferTo(_destination, amount, ApplicationTestContext.Now.AddMinutes(-5), "Rent", Key);
        _context.Transactions
            .Setup(repository => repository.GetByIdempotencyKeyAsync(Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(legs.Outgoing);
        _context.Transactions
            .Setup(repository => repository.GetByTransferIdAsync(legs.TransferId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([legs.Outgoing, legs.Incoming]);
        return legs;
    }

    private void VerifyNoTransferExecuted()
    {
        _context.UnitOfWork.Verify(
            unitOfWork => unitOfWork.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<TransferResponse>>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _context.VerifyNothingSaved();
    }

    // ---- Successful transfers ---------------------------------------------------------------

    [Fact]
    public async Task TransferAsync_WithValidRequest_MovesMoneyAndReturnsTransfer()
    {
        var result = await CreateService().TransferAsync(Request(), Key);

        result.IsReplay.Should().BeFalse();
        result.Transfer.TransferId.Should().NotBeEmpty();
        result.Transfer.SourceAccountId.Should().Be(_source.Id);
        result.Transfer.DestinationAccountId.Should().Be(_destination.Id);
        result.Transfer.Amount.Should().Be(100m);
        result.Transfer.SourceBalanceAfter.Should().Be(900m);
        result.Transfer.Reference.Should().Be("Rent");
        result.Transfer.CreatedAt.Should().Be(ApplicationTestContext.Now);
        _source.Balance.Should().Be(900m);
        _destination.Balance.Should().Be(100m);
    }

    [Fact]
    public async Task TransferAsync_WithValidRequest_RecordsBothLegsWithKeyOnOutgoingOnly()
    {
        var added = new List<Transaction>();
        _context.Transactions
            .Setup(repository => repository.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((transaction, _) => added.Add(transaction))
            .Returns(Task.CompletedTask);

        var result = await CreateService().TransferAsync(Request(), Key);

        added.Should().HaveCount(2);
        var outgoing = added.Single(transaction => transaction.Type == TransactionType.TransferOut);
        var incoming = added.Single(transaction => transaction.Type == TransactionType.TransferIn);
        outgoing.AccountId.Should().Be(_source.Id);
        outgoing.IdempotencyKey.Should().Be(Key);
        outgoing.TransferId.Should().Be(result.Transfer.TransferId);
        incoming.AccountId.Should().Be(_destination.Id);
        incoming.IdempotencyKey.Should().BeNull();
        incoming.TransferId.Should().Be(result.Transfer.TransferId);
    }

    [Fact]
    public async Task TransferAsync_WithValidRequest_SavesOnceInsideSingleDatabaseTransaction()
    {
        await CreateService().TransferAsync(Request(), Key);

        _context.UnitOfWork.Verify(
            unitOfWork => unitOfWork.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<TransferResponse>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _context.VerifySavedOnce();
    }

    [Fact]
    public async Task TransferAsync_WithValidRequest_RecordsAuditEntry()
    {
        await CreateService().TransferAsync(Request(), Key);

        _context.AuditTrail.Verify(
            audit => audit.RecordAsync(AuditActions.Transfer, nameof(Account), _source.Id, It.IsNotNull<object>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TransferAsync_WithPaddedKey_StoresTrimmedKey()
    {
        Transaction? outgoing = null;
        _context.Transactions
            .Setup(repository => repository.AddAsync(
                It.Is<Transaction>(transaction => transaction.Type == TransactionType.TransferOut),
                It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((transaction, _) => outgoing = transaction)
            .Returns(Task.CompletedTask);

        await CreateService().TransferAsync(Request(), $"  {Key}  ");

        outgoing!.IdempotencyKey.Should().Be(Key);
    }

    // ---- Validation -------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TransferAsync_WithoutIdempotencyKey_ThrowsValidationException(string? idempotencyKey)
    {
        var act = () => CreateService().TransferAsync(Request(), idempotencyKey);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().ContainSingle(error => error.PropertyName == IdempotencyKey.HeaderName);
        VerifyNoTransferExecuted();
    }

    [Fact]
    public async Task TransferAsync_WithTooLongIdempotencyKey_ThrowsValidationException()
    {
        var idempotencyKey = new string('k', Transaction.IdempotencyKeyMaxLength + 1);

        var act = () => CreateService().TransferAsync(Request(), idempotencyKey);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().ContainSingle(error => error.PropertyName == IdempotencyKey.HeaderName);
        VerifyNoTransferExecuted();
    }

    [Fact]
    public async Task TransferAsync_WithSameSourceAndDestination_ThrowsValidationException()
    {
        var request = new TransferRequest(_source.Id, _source.Id, 100m, null);

        var act = () => CreateService().TransferAsync(request, Key);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().Contain(error => error.PropertyName == nameof(TransferRequest.DestinationAccountId));
        _source.Balance.Should().Be(1_000m);
        VerifyNoTransferExecuted();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(10.001)]
    public async Task TransferAsync_WithInvalidAmount_ThrowsValidationException(decimal amount)
    {
        var act = () => CreateService().TransferAsync(Request(amount), Key);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().Contain(error => error.PropertyName == nameof(TransferRequest.Amount));
        VerifyNoTransferExecuted();
    }

    [Fact]
    public async Task TransferAsync_WithEmptyAccountIds_ThrowsValidationException()
    {
        var act = () => CreateService().TransferAsync(new TransferRequest(Guid.Empty, Guid.Empty, 10m, null), Key);

        await act.Should().ThrowAsync<ValidationException>();
        VerifyNoTransferExecuted();
    }

    // ---- Missing accounts -------------------------------------------------------------------

    [Fact]
    public async Task TransferAsync_WhenSourceAccountDoesNotExist_ThrowsNotFoundException()
    {
        var request = new TransferRequest(Guid.NewGuid(), _destination.Id, 100m, null);

        var act = () => CreateService().TransferAsync(request, Key);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage($"*{request.SourceAccountId}*");
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task TransferAsync_WhenDestinationAccountDoesNotExist_ThrowsNotFoundException()
    {
        var request = new TransferRequest(_source.Id, Guid.NewGuid(), 100m, null);

        var act = () => CreateService().TransferAsync(request, Key);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage($"*{request.DestinationAccountId}*");
        _source.Balance.Should().Be(1_000m);
        _context.VerifyNothingSaved();
    }

    // ---- Business rules ---------------------------------------------------------------------

    [Fact]
    public async Task TransferAsync_WithInsufficientFunds_ThrowsAndChangesNothing()
    {
        var act = () => CreateService().TransferAsync(Request(1_000.01m), Key);

        await act.Should().ThrowAsync<InsufficientFundsException>();
        _source.Balance.Should().Be(1_000m);
        _destination.Balance.Should().Be(0m);
        _context.Transactions.Verify(
            repository => repository.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task TransferAsync_WhenDailyLimitWouldBeExceeded_ThrowsDailyTransferLimitExceededException()
    {
        SetTransferredToday(4_950m);

        var act = () => CreateService().TransferAsync(Request(100m), Key);

        var exception = (await act.Should().ThrowAsync<DailyTransferLimitExceededException>()).Which;
        exception.AccountId.Should().Be(_source.Id);
        exception.DailyLimit.Should().Be(DailyLimit);
        exception.TransferredToday.Should().Be(4_950m);
        exception.RequestedAmount.Should().Be(100m);
        _source.Balance.Should().Be(1_000m);
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task TransferAsync_SingleTransferAboveDailyLimit_ThrowsDailyTransferLimitExceededException()
    {
        var richSource = ApplicationTestContext.CreateAccount(balance: 10_000m);
        _context.AddAccount(richSource);

        var act = () => CreateService().TransferAsync(new TransferRequest(richSource.Id, _destination.Id, DailyLimit + 0.01m, null), Key);

        await act.Should().ThrowAsync<DailyTransferLimitExceededException>();
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task TransferAsync_ThatExactlyReachesDailyLimit_Succeeds()
    {
        SetTransferredToday(4_900m);

        var result = await CreateService().TransferAsync(Request(100m), Key);

        result.Transfer.Amount.Should().Be(100m);
        _context.VerifySavedOnce();
    }

    [Fact]
    public async Task TransferAsync_ChecksDailyLimitAgainstCurrentUtcDay()
    {
        var startOfDay = new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero);

        await CreateService().TransferAsync(Request(), Key);

        _context.Transactions.Verify(
            repository => repository.GetTotalAmountAsync(
                _source.Id,
                TransactionType.TransferOut,
                startOfDay,
                startOfDay.AddDays(1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TransferAsync_BetweenDifferentCurrencies_ThrowsCurrencyMismatchException()
    {
        var euroAccount = ApplicationTestContext.CreateAccount(currency: "EUR");
        _context.AddAccount(euroAccount);

        var act = () => CreateService().TransferAsync(new TransferRequest(_source.Id, euroAccount.Id, 100m, null), Key);

        await act.Should().ThrowAsync<CurrencyMismatchException>();
        _source.Balance.Should().Be(1_000m);
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task TransferAsync_ToFrozenAccount_ThrowsAccountNotActiveException()
    {
        _destination.Freeze();

        var act = () => CreateService().TransferAsync(Request(), Key);

        (await act.Should().ThrowAsync<AccountNotActiveException>()).Which.AccountId.Should().Be(_destination.Id);
        _source.Balance.Should().Be(1_000m);
        _context.VerifyNothingSaved();
    }

    // ---- Idempotency ------------------------------------------------------------------------

    [Fact]
    public async Task TransferAsync_WithReusedKeyForSameRequest_ReturnsOriginalResultWithoutMovingMoney()
    {
        var earlier = ArrangeEarlierTransfer();
        var balanceAfterEarlierTransfer = _source.Balance;

        var result = await CreateService().TransferAsync(Request(), Key);

        result.IsReplay.Should().BeTrue();
        result.Transfer.TransferId.Should().Be(earlier.TransferId);
        result.Transfer.SourceBalanceAfter.Should().Be(earlier.Outgoing.BalanceAfter);
        result.Transfer.CreatedAt.Should().Be(earlier.Outgoing.CreatedAt);
        _source.Balance.Should().Be(balanceAfterEarlierTransfer);
        VerifyNoTransferExecuted();
        _context.AuditTrail.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TransferAsync_WithReusedKeyForDifferentAmount_ThrowsIdempotencyKeyReusedException()
    {
        ArrangeEarlierTransfer(amount: 100m);

        var act = () => CreateService().TransferAsync(Request(amount: 200m), Key);

        await act.Should().ThrowAsync<IdempotencyKeyReusedException>();
        VerifyNoTransferExecuted();
    }

    [Fact]
    public async Task TransferAsync_WithReusedKeyForDifferentDestination_ThrowsIdempotencyKeyReusedException()
    {
        ArrangeEarlierTransfer();
        var otherAccount = ApplicationTestContext.CreateAccount();
        _context.AddAccount(otherAccount);

        var act = () => CreateService().TransferAsync(new TransferRequest(_source.Id, otherAccount.Id, 100m, "Rent"), Key);

        await act.Should().ThrowAsync<IdempotencyKeyReusedException>();
        VerifyNoTransferExecuted();
    }

    [Fact]
    public async Task TransferAsync_WithKeyAlreadyUsedByDeposit_ThrowsIdempotencyKeyReusedException()
    {
        var deposit = _source.Deposit(10m, ApplicationTestContext.Now, idempotencyKey: Key);
        _context.Transactions
            .Setup(repository => repository.GetByIdempotencyKeyAsync(Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deposit);

        var act = () => CreateService().TransferAsync(Request(), Key);

        await act.Should().ThrowAsync<IdempotencyKeyReusedException>().WithMessage("*different operation*");
        VerifyNoTransferExecuted();
    }

    [Fact]
    public async Task TransferAsync_WhenConcurrentRequestWithSameKeyWins_ReturnsWinningResult()
    {
        var winner = _source.TransferTo(_destination, 100m, ApplicationTestContext.Now, "Rent", Key);
        _context.Transactions
            .SetupSequence(repository => repository.GetByIdempotencyKeyAsync(Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null)
            .ReturnsAsync(winner.Outgoing);
        _context.Transactions
            .Setup(repository => repository.GetByTransferIdAsync(winner.TransferId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([winner.Outgoing, winner.Incoming]);
        _context.UnitOfWork
            .Setup(unitOfWork => unitOfWork.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<TransferResponse>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateKeyException());

        var result = await CreateService().TransferAsync(Request(), Key);

        result.IsReplay.Should().BeTrue();
        result.Transfer.TransferId.Should().Be(winner.TransferId);
    }

    [Fact]
    public async Task TransferAsync_WhenDuplicateKeyIsNotTheIdempotencyKey_RethrowsDuplicateKeyException()
    {
        _context.UnitOfWork
            .Setup(unitOfWork => unitOfWork.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<TransferResponse>>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateKeyException());

        var act = () => CreateService().TransferAsync(Request(), Key);

        await act.Should().ThrowAsync<DuplicateKeyException>();
    }

    [Fact]
    public async Task TransferAsync_WhenAccountChangedConcurrently_PropagatesConcurrencyConflict()
    {
        _context.UnitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException());

        var act = () => CreateService().TransferAsync(Request(), Key);

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
    }
}
