using BankingApi.Application.Abstractions;
using BankingApi.Application.Accounts;
using BankingApi.Application.Auditing;
using BankingApi.Application.Common.Exceptions;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Exceptions;
using FluentAssertions;
using FluentValidation;
using Moq;

namespace BankingApi.UnitTests.Application.Accounts;

public class AccountServiceTests
{
    private readonly ApplicationTestContext _context = new();
    private readonly Mock<IAccountNumberGenerator> _accountNumberGenerator = new();

    public AccountServiceTests()
    {
        _accountNumberGenerator.Setup(generator => generator.Generate()).Returns("100000000001");
    }

    private AccountService CreateService() =>
        new(
            _context.UnitOfWork.Object,
            _context.AuditTrail.Object,
            _context.TimeProvider.Object,
            _accountNumberGenerator.Object,
            new OpenAccountRequestValidator(),
            new MoneyOperationRequestValidator());

    // ---- OpenAsync --------------------------------------------------------------------------

    [Fact]
    public async Task OpenAsync_WithValidRequest_SavesActiveAccountAndReturnsResponse()
    {
        var customerId = Guid.NewGuid();
        _context.Customers.Setup(repository => repository.ExistsAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Account? added = null;
        _context.Accounts
            .Setup(repository => repository.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Callback<Account, CancellationToken>((account, _) => added = account)
            .Returns(Task.CompletedTask);

        var response = await CreateService().OpenAsync(new OpenAccountRequest(customerId, "usd"));

        added.Should().NotBeNull();
        response.Id.Should().Be(added!.Id);
        response.AccountNumber.Should().Be("100000000001");
        response.CustomerId.Should().Be(customerId);
        response.Currency.Should().Be("USD");
        response.Balance.Should().Be(0m);
        response.Status.Should().Be("Active");
        response.CreatedAt.Should().Be(ApplicationTestContext.Now);
        _context.AuditTrail.Verify(
            audit => audit.RecordAsync(AuditActions.AccountOpened, nameof(Account), response.Id, It.IsAny<object?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _context.VerifySavedOnce();
    }

    [Fact]
    public async Task OpenAsync_WhenCustomerDoesNotExist_ThrowsNotFoundException()
    {
        var act = () => CreateService().OpenAsync(new OpenAccountRequest(Guid.NewGuid(), "USD"));

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Customer*");
        _context.VerifyNothingSaved();
    }

    [Theory]
    [InlineData(false, "USD", "CustomerId")]
    [InlineData(true, "US", "Currency")]
    [InlineData(true, "U5D", "Currency")]
    [InlineData(true, "", "Currency")]
    public async Task OpenAsync_WithInvalidRequest_ThrowsValidationException(bool hasCustomerId, string currency, string invalidProperty)
    {
        var request = new OpenAccountRequest(hasCustomerId ? Guid.NewGuid() : Guid.Empty, currency);

        var act = () => CreateService().OpenAsync(request);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().Contain(error => error.PropertyName == invalidProperty);
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task OpenAsync_WhenGeneratedNumberIsTaken_RetriesWithNewNumber()
    {
        var customerId = Guid.NewGuid();
        _context.Customers.Setup(repository => repository.ExistsAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _accountNumberGenerator.SetupSequence(generator => generator.Generate()).Returns("111111111111").Returns("222222222222");
        _context.Accounts
            .Setup(repository => repository.AccountNumberExistsAsync("111111111111", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var response = await CreateService().OpenAsync(new OpenAccountRequest(customerId, "USD"));

        response.AccountNumber.Should().Be("222222222222");
    }

    [Fact]
    public async Task OpenAsync_WhenEveryGeneratedNumberIsTaken_ThrowsConflictException()
    {
        var customerId = Guid.NewGuid();
        _context.Customers.Setup(repository => repository.ExistsAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _context.Accounts
            .Setup(repository => repository.AccountNumberExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => CreateService().OpenAsync(new OpenAccountRequest(customerId, "USD"));

        await act.Should().ThrowAsync<ConflictException>();
        _accountNumberGenerator.Verify(generator => generator.Generate(), Times.Exactly(AccountService.MaxAccountNumberAttempts));
        _context.VerifyNothingSaved();
    }

    // ---- GetByIdAsync / GetBalanceAsync -----------------------------------------------------

    [Fact]
    public async Task GetByIdAsync_ExistingAccount_ReturnsResponse()
    {
        var account = ApplicationTestContext.CreateAccount(balance: 250m);
        _context.AddAccount(account);

        var response = await CreateService().GetByIdAsync(account.Id);

        response.Id.Should().Be(account.Id);
        response.AccountNumber.Should().Be(account.AccountNumber);
        response.Balance.Should().Be(250m);
    }

    [Fact]
    public async Task GetByIdAsync_MissingAccount_ThrowsNotFoundException()
    {
        var act = () => CreateService().GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Account*");
    }

    [Fact]
    public async Task GetBalanceAsync_ExistingAccount_ReturnsCurrentBalance()
    {
        var account = ApplicationTestContext.CreateAccount(balance: 99.95m, currency: "EUR");
        _context.AddAccount(account);

        var response = await CreateService().GetBalanceAsync(account.Id);

        response.AccountId.Should().Be(account.Id);
        response.Balance.Should().Be(99.95m);
        response.Currency.Should().Be("EUR");
        response.AsOf.Should().Be(ApplicationTestContext.Now);
    }

    [Fact]
    public async Task GetBalanceAsync_MissingAccount_ThrowsNotFoundException()
    {
        var act = () => CreateService().GetBalanceAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---- DepositAsync -----------------------------------------------------------------------

    [Fact]
    public async Task DepositAsync_WithValidRequest_UpdatesBalanceAndRecordsTransaction()
    {
        var account = ApplicationTestContext.CreateAccount(balance: 100m);
        _context.AddAccount(account);

        var response = await CreateService().DepositAsync(account.Id, new MoneyOperationRequest(50.25m, "Salary"));

        account.Balance.Should().Be(150.25m);
        response.AccountId.Should().Be(account.Id);
        response.Type.Should().Be("Deposit");
        response.Amount.Should().Be(50.25m);
        response.BalanceAfter.Should().Be(150.25m);
        response.Reference.Should().Be("Salary");
        response.CreatedAt.Should().Be(ApplicationTestContext.Now);
        _context.Transactions.Verify(
            repository => repository.AddAsync(It.Is<Transaction>(transaction => transaction.Id == response.Id), It.IsAny<CancellationToken>()),
            Times.Once);
        _context.VerifySavedOnce();
    }

    [Fact]
    public async Task DepositAsync_WithValidRequest_RecordsAuditEntry()
    {
        var account = ApplicationTestContext.CreateAccount();
        _context.AddAccount(account);

        await CreateService().DepositAsync(account.Id, new MoneyOperationRequest(10m, null));

        _context.AuditTrail.Verify(
            audit => audit.RecordAsync(AuditActions.Deposit, nameof(Account), account.Id, It.IsNotNull<object>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1.234)]
    public async Task DepositAsync_WithInvalidAmount_ThrowsValidationException(decimal amount)
    {
        var account = ApplicationTestContext.CreateAccount();
        _context.AddAccount(account);

        var act = () => CreateService().DepositAsync(account.Id, new MoneyOperationRequest(amount, null));

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Errors.Should().Contain(error => error.PropertyName == "Amount");
        account.Balance.Should().Be(0m);
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task DepositAsync_WithTooLongReference_ThrowsValidationException()
    {
        var account = ApplicationTestContext.CreateAccount();
        _context.AddAccount(account);
        var reference = new string('r', Transaction.ReferenceMaxLength + 1);

        var act = () => CreateService().DepositAsync(account.Id, new MoneyOperationRequest(10m, reference));

        await act.Should().ThrowAsync<ValidationException>();
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task DepositAsync_MissingAccount_ThrowsNotFoundException()
    {
        var act = () => CreateService().DepositAsync(Guid.NewGuid(), new MoneyOperationRequest(10m, null));

        await act.Should().ThrowAsync<NotFoundException>();
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task DepositAsync_OnFrozenAccount_ThrowsAccountNotActiveException()
    {
        var account = ApplicationTestContext.CreateAccount();
        account.Freeze();
        _context.AddAccount(account);

        var act = () => CreateService().DepositAsync(account.Id, new MoneyOperationRequest(10m, null));

        await act.Should().ThrowAsync<AccountNotActiveException>();
        _context.VerifyNothingSaved();
        _context.AuditTrail.VerifyNoOtherCalls();
    }

    // ---- WithdrawAsync ----------------------------------------------------------------------

    [Fact]
    public async Task WithdrawAsync_WithSufficientFunds_UpdatesBalanceAndRecordsTransaction()
    {
        var account = ApplicationTestContext.CreateAccount(balance: 100m);
        _context.AddAccount(account);

        var response = await CreateService().WithdrawAsync(account.Id, new MoneyOperationRequest(40m, "ATM"));

        account.Balance.Should().Be(60m);
        response.Type.Should().Be("Withdrawal");
        response.Amount.Should().Be(40m);
        response.BalanceAfter.Should().Be(60m);
        _context.Transactions.Verify(
            repository => repository.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _context.AuditTrail.Verify(
            audit => audit.RecordAsync(AuditActions.Withdrawal, nameof(Account), account.Id, It.IsNotNull<object>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _context.VerifySavedOnce();
    }

    [Fact]
    public async Task WithdrawAsync_WithInsufficientFunds_ThrowsAndChangesNothing()
    {
        var account = ApplicationTestContext.CreateAccount(balance: 100m);
        _context.AddAccount(account);

        var act = () => CreateService().WithdrawAsync(account.Id, new MoneyOperationRequest(100.01m, null));

        await act.Should().ThrowAsync<InsufficientFundsException>();
        account.Balance.Should().Be(100m);
        _context.Transactions.Verify(
            repository => repository.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task WithdrawAsync_WithInvalidAmount_ThrowsValidationException()
    {
        var account = ApplicationTestContext.CreateAccount(balance: 100m);
        _context.AddAccount(account);

        var act = () => CreateService().WithdrawAsync(account.Id, new MoneyOperationRequest(-1m, null));

        await act.Should().ThrowAsync<ValidationException>();
        _context.VerifyNothingSaved();
    }

    [Fact]
    public async Task WithdrawAsync_MissingAccount_ThrowsNotFoundException()
    {
        var act = () => CreateService().WithdrawAsync(Guid.NewGuid(), new MoneyOperationRequest(10m, null));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task WithdrawAsync_WhenAccountChangedConcurrently_PropagatesConcurrencyConflict()
    {
        var account = ApplicationTestContext.CreateAccount(balance: 100m);
        _context.AddAccount(account);
        _context.UnitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException());

        var act = () => CreateService().WithdrawAsync(account.Id, new MoneyOperationRequest(10m, null));

        await act.Should().ThrowAsync<ConcurrencyConflictException>();
    }
}
