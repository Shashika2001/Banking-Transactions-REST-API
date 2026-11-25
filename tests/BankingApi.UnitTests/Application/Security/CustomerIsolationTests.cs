using BankingApi.Application.Abstractions;
using BankingApi.Application.Accounts;
using BankingApi.Application.Common.Exceptions;
using BankingApi.Application.Common.Models;
using BankingApi.Application.Common.Validation;
using BankingApi.Application.Customers;
using BankingApi.Application.Transactions;
using BankingApi.Application.Transfers;
using BankingApi.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace BankingApi.UnitTests.Application.Security;

/// <summary>
/// End-to-end checks, through the real services and <see cref="BankingApi.Application.Common.Security.AccessPolicy"/>,
/// that a customer cannot read or move money in another customer's accounts.
/// </summary>
public class CustomerIsolationTests
{
    private const string Key = "isolation-test-key";

    private readonly ApplicationTestContext _context = new();
    private readonly Account _ownAccount = ApplicationTestContext.CreateAccount(balance: 500m);
    private readonly Account _otherCustomersAccount = ApplicationTestContext.CreateAccount(balance: 500m);

    public CustomerIsolationTests()
    {
        _context.AddAccount(_ownAccount);
        _context.AddAccount(_otherCustomersAccount);
        _context.RunTransactionsInline<TransferResponse>();
        _context.ActAsCustomer(_ownAccount.CustomerId);
    }

    private AccountService CreateAccountService()
    {
        var generator = new Mock<IAccountNumberGenerator>();
        generator.Setup(g => g.Generate()).Returns("123456789012");
        return new AccountService(
            _context.UnitOfWork.Object,
            _context.AuditTrail.Object,
            _context.AccessPolicy,
            _context.TimeProvider.Object,
            generator.Object,
            new OpenAccountRequestValidator(),
            new MoneyOperationRequestValidator());
    }

    private TransferService CreateTransferService() =>
        new(
            _context.UnitOfWork.Object,
            _context.AuditTrail.Object,
            _context.AccessPolicy,
            _context.TimeProvider.Object,
            new TransferRequestValidator(),
            Options.Create(new TransferOptions()));

    private TransactionService CreateTransactionService() =>
        new(_context.UnitOfWork.Object, _context.AccessPolicy, new TransactionHistoryQueryValidator());

    private CustomerService CreateCustomerService() =>
        new(
            _context.UnitOfWork.Object,
            _context.AuditTrail.Object,
            _context.AccessPolicy,
            _context.TimeProvider.Object,
            new CreateCustomerRequestValidator(),
            new PageRequestValidator());

    private void VerifyOtherAccountUntouched()
    {
        _otherCustomersAccount.Balance.Should().Be(500m);
        _context.VerifyNothingSaved();
        _context.Transactions.Verify(
            repository => repository.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---- Reading another customer's account -------------------------------------------------

    [Fact]
    public async Task GetAccount_OtherCustomersAccount_ThrowsForbiddenAccessException()
    {
        var act = () => CreateAccountService().GetByIdAsync(_otherCustomersAccount.Id);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task GetBalance_OtherCustomersAccount_ThrowsForbiddenAccessException()
    {
        var act = () => CreateAccountService().GetBalanceAsync(_otherCustomersAccount.Id);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task GetTransactionHistory_OtherCustomersAccount_ThrowsForbiddenAccessException()
    {
        var act = () => CreateTransactionService().GetHistoryAsync(_otherCustomersAccount.Id, new TransactionHistoryQuery());

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _context.Transactions.Verify(
            repository => repository.GetHistoryAsync(
                It.IsAny<Guid>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetCustomer_OtherCustomer_ThrowsForbiddenAccessExceptionWithoutLookingUp()
    {
        var act = () => CreateCustomerService().GetByIdAsync(_otherCustomersAccount.CustomerId);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _context.Customers.Verify(
            repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ListCustomers_AsCustomer_ThrowsForbiddenAccessException()
    {
        var act = () => CreateCustomerService().ListAsync(new PageRequest());

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    // ---- Changing another customer's account ------------------------------------------------

    [Fact]
    public async Task Deposit_IntoOtherCustomersAccount_ThrowsForbiddenAccessException()
    {
        var act = () => CreateAccountService().DepositAsync(_otherCustomersAccount.Id, new MoneyOperationRequest(10m, null));

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        VerifyOtherAccountUntouched();
    }

    [Fact]
    public async Task Withdraw_FromOtherCustomersAccount_ThrowsForbiddenAccessException()
    {
        var act = () => CreateAccountService().WithdrawAsync(_otherCustomersAccount.Id, new MoneyOperationRequest(10m, null));

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        VerifyOtherAccountUntouched();
    }

    [Fact]
    public async Task Transfer_FromOtherCustomersAccount_ThrowsForbiddenAccessException()
    {
        var request = new TransferRequest(_otherCustomersAccount.Id, _ownAccount.Id, 100m, null);

        var act = () => CreateTransferService().TransferAsync(request, Key);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _ownAccount.Balance.Should().Be(500m);
        VerifyOtherAccountUntouched();
    }

    [Fact]
    public async Task Transfer_ReplayingOtherCustomersIdempotencyKey_ThrowsForbiddenAccessException()
    {
        // The other customer made this transfer earlier; replaying their key and request must not reveal it.
        var earlier = _otherCustomersAccount.TransferTo(_ownAccount, 100m, ApplicationTestContext.Now, null, Key);
        _context.Transactions
            .Setup(repository => repository.GetByIdempotencyKeyAsync(Key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(earlier.Outgoing);
        _context.Transactions
            .Setup(repository => repository.GetByTransferIdAsync(earlier.TransferId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([earlier.Outgoing, earlier.Incoming]);
        var request = new TransferRequest(_otherCustomersAccount.Id, _ownAccount.Id, 100m, null);

        var act = () => CreateTransferService().TransferAsync(request, Key);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task OpenAccount_ForOtherCustomer_ThrowsForbiddenAccessExceptionWithoutLookingUp()
    {
        var act = () => CreateAccountService().OpenAsync(new OpenAccountRequest(_otherCustomersAccount.CustomerId, "USD"));

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _context.Customers.Verify(
            repository => repository.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _context.VerifyNothingSaved();
    }

    // ---- Own accounts still work ------------------------------------------------------------

    [Fact]
    public async Task GetBalance_OwnAccount_ReturnsBalance()
    {
        var balance = await CreateAccountService().GetBalanceAsync(_ownAccount.Id);

        balance.Balance.Should().Be(500m);
    }

    [Fact]
    public async Task Withdraw_FromOwnAccount_Succeeds()
    {
        var transaction = await CreateAccountService().WithdrawAsync(_ownAccount.Id, new MoneyOperationRequest(50m, null));

        transaction.BalanceAfter.Should().Be(450m);
        _context.VerifySavedOnce();
    }

    [Fact]
    public async Task Transfer_FromOwnAccountToOtherCustomersAccount_Succeeds()
    {
        var request = new TransferRequest(_ownAccount.Id, _otherCustomersAccount.Id, 100m, null);

        var result = await CreateTransferService().TransferAsync(request, Key);

        result.Transfer.SourceBalanceAfter.Should().Be(400m);
        _otherCustomersAccount.Balance.Should().Be(600m);
        _context.VerifySavedOnce();
    }

    [Fact]
    public async Task GetCustomer_Themselves_ReturnsCustomer()
    {
        var customer = Customer.Create("Own Customer", "own@example.com", "123456789", ApplicationTestContext.Now);
        _context.ActAsCustomer(customer.Id);
        _context.Customers
            .Setup(repository => repository.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var response = await CreateCustomerService().GetByIdAsync(customer.Id);

        response.Id.Should().Be(customer.Id);
    }
}
