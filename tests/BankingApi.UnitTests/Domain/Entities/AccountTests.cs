using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;
using BankingApi.Domain.Exceptions;
using FluentAssertions;

namespace BankingApi.UnitTests.Domain.Entities;

public class AccountTests
{
    private static readonly DateTimeOffset OpenedAt = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 10, 30, 0, TimeSpan.Zero);
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static Account OpenAccount(decimal initialBalance = 0m)
    {
        var account = Account.Open("ACC-0001", CustomerId, "USD", OpenedAt);
        if (initialBalance > 0m)
        {
            account.Deposit(initialBalance, OpenedAt);
        }

        return account;
    }

    // ---- Open -------------------------------------------------------------------------------

    [Fact]
    public void Open_WithValidValues_CreatesActiveAccountWithZeroBalance()
    {
        var account = Account.Open("  ACC-0001  ", CustomerId, "EUR", OpenedAt);

        account.Id.Should().NotBeEmpty();
        account.AccountNumber.Should().Be("ACC-0001");
        account.CustomerId.Should().Be(CustomerId);
        account.Currency.Should().Be("EUR");
        account.Balance.Should().Be(0m);
        account.Status.Should().Be(AccountStatus.Active);
        account.CreatedAt.Should().Be(OpenedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Open_WithMissingAccountNumber_ThrowsDomainValidationException(string? accountNumber)
    {
        var act = () => Account.Open(accountNumber!, CustomerId, "USD", OpenedAt);

        act.Should().Throw<DomainValidationException>().WithMessage("*AccountNumber*");
    }

    [Fact]
    public void Open_WithTooLongAccountNumber_ThrowsDomainValidationException()
    {
        var accountNumber = new string('9', Account.AccountNumberMaxLength + 1);

        var act = () => Account.Open(accountNumber, CustomerId, "USD", OpenedAt);

        act.Should().Throw<DomainValidationException>().WithMessage("*AccountNumber*");
    }

    [Fact]
    public void Open_WithEmptyCustomerId_ThrowsDomainValidationException()
    {
        var act = () => Account.Open("ACC-0001", Guid.Empty, "USD", OpenedAt);

        act.Should().Throw<DomainValidationException>().WithMessage("*CustomerId*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDX")]
    [InlineData("usd")]
    [InlineData("U5D")]
    public void Open_WithInvalidCurrency_ThrowsDomainValidationException(string currency)
    {
        var act = () => Account.Open("ACC-0001", CustomerId, currency, OpenedAt);

        act.Should().Throw<DomainValidationException>().WithMessage("*Currency*");
    }

    // ---- Deposit ----------------------------------------------------------------------------

    [Fact]
    public void Deposit_WithPositiveAmount_IncreasesBalance()
    {
        var account = OpenAccount(initialBalance: 100m);

        account.Deposit(50.25m, Now);

        account.Balance.Should().Be(150.25m);
    }

    [Fact]
    public void Deposit_WithPositiveAmount_ReturnsDepositTransaction()
    {
        var account = OpenAccount(initialBalance: 100m);

        var transaction = account.Deposit(50.25m, Now, " Salary ", "key-123");

        transaction.Id.Should().NotBeEmpty();
        transaction.AccountId.Should().Be(account.Id);
        transaction.Type.Should().Be(TransactionType.Deposit);
        transaction.Amount.Should().Be(50.25m);
        transaction.BalanceAfter.Should().Be(150.25m);
        transaction.Reference.Should().Be("Salary");
        transaction.IdempotencyKey.Should().Be("key-123");
        transaction.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Deposit_WithoutReferenceOrIdempotencyKey_LeavesThemNull()
    {
        var account = OpenAccount();

        var transaction = account.Deposit(10m, Now);

        transaction.Reference.Should().BeNull();
        transaction.IdempotencyKey.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Deposit_WithNonPositiveAmount_ThrowsInvalidAmountException(decimal amount)
    {
        var account = OpenAccount(initialBalance: 100m);

        var act = () => account.Deposit(amount, Now);

        act.Should().Throw<InvalidAmountException>().Which.Amount.Should().Be(amount);
        account.Balance.Should().Be(100m);
    }

    [Fact]
    public void Deposit_WithMoreThanTwoDecimalPlaces_ThrowsInvalidAmountException()
    {
        var account = OpenAccount();

        var act = () => account.Deposit(10.001m, Now);

        act.Should().Throw<InvalidAmountException>().WithMessage("*decimal places*");
        account.Balance.Should().Be(0m);
    }

    [Fact]
    public void Deposit_WithTrailingZeroDecimals_IsAccepted()
    {
        var account = OpenAccount();

        account.Deposit(10.500m, Now);

        account.Balance.Should().Be(10.5m);
    }

    [Fact]
    public void Deposit_ThatWouldExceedMaximumBalance_ThrowsInvalidAmountException()
    {
        var account = OpenAccount(initialBalance: Account.MaxMoneyValue);

        var act = () => account.Deposit(0.01m, Now);

        act.Should().Throw<InvalidAmountException>().WithMessage("*maximum*");
        account.Balance.Should().Be(Account.MaxMoneyValue);
    }

    [Theory]
    [InlineData(AccountStatus.Frozen)]
    [InlineData(AccountStatus.Closed)]
    public void Deposit_OnInactiveAccount_ThrowsAccountNotActiveException(AccountStatus status)
    {
        var account = OpenAccount();
        MakeInactive(account, status);

        var act = () => account.Deposit(10m, Now);

        var exception = act.Should().Throw<AccountNotActiveException>().Which;
        exception.AccountId.Should().Be(account.Id);
        exception.Status.Should().Be(status);
        account.Balance.Should().Be(0m);
    }

    [Fact]
    public void Deposit_WithTooLongReference_ThrowsAndLeavesBalanceUnchanged()
    {
        var account = OpenAccount(initialBalance: 100m);
        var reference = new string('r', Transaction.ReferenceMaxLength + 1);

        var act = () => account.Deposit(10m, Now, reference);

        act.Should().Throw<DomainValidationException>().WithMessage("*Reference*");
        account.Balance.Should().Be(100m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Deposit_WithBlankIdempotencyKey_ThrowsAndLeavesBalanceUnchanged(string idempotencyKey)
    {
        var account = OpenAccount(initialBalance: 100m);

        var act = () => account.Deposit(10m, Now, idempotencyKey: idempotencyKey);

        act.Should().Throw<DomainValidationException>().WithMessage("*IdempotencyKey*");
        account.Balance.Should().Be(100m);
    }

    [Fact]
    public void Deposit_WithTooLongIdempotencyKey_ThrowsDomainValidationException()
    {
        var account = OpenAccount();
        var idempotencyKey = new string('k', Transaction.IdempotencyKeyMaxLength + 1);

        var act = () => account.Deposit(10m, Now, idempotencyKey: idempotencyKey);

        act.Should().Throw<DomainValidationException>().WithMessage("*IdempotencyKey*");
    }

    // ---- Withdraw ---------------------------------------------------------------------------

    [Fact]
    public void Withdraw_WithSufficientFunds_DecreasesBalance()
    {
        var account = OpenAccount(initialBalance: 100m);

        account.Withdraw(40.10m, Now);

        account.Balance.Should().Be(59.90m);
    }

    [Fact]
    public void Withdraw_WithSufficientFunds_ReturnsWithdrawalTransaction()
    {
        var account = OpenAccount(initialBalance: 100m);

        var transaction = account.Withdraw(40.10m, Now, "ATM", "key-456");

        transaction.AccountId.Should().Be(account.Id);
        transaction.Type.Should().Be(TransactionType.Withdrawal);
        transaction.Amount.Should().Be(40.10m);
        transaction.BalanceAfter.Should().Be(59.90m);
        transaction.Reference.Should().Be("ATM");
        transaction.IdempotencyKey.Should().Be("key-456");
        transaction.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Withdraw_EntireBalance_LeavesZeroBalance()
    {
        var account = OpenAccount(initialBalance: 100m);

        var transaction = account.Withdraw(100m, Now);

        account.Balance.Should().Be(0m);
        transaction.BalanceAfter.Should().Be(0m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Withdraw_WithNonPositiveAmount_ThrowsInvalidAmountException(decimal amount)
    {
        var account = OpenAccount(initialBalance: 100m);

        var act = () => account.Withdraw(amount, Now);

        act.Should().Throw<InvalidAmountException>().Which.Amount.Should().Be(amount);
        account.Balance.Should().Be(100m);
    }

    [Fact]
    public void Withdraw_WithMoreThanTwoDecimalPlaces_ThrowsInvalidAmountException()
    {
        var account = OpenAccount(initialBalance: 100m);

        var act = () => account.Withdraw(0.005m, Now);

        act.Should().Throw<InvalidAmountException>().WithMessage("*decimal places*");
        account.Balance.Should().Be(100m);
    }

    [Fact]
    public void Withdraw_MoreThanBalance_ThrowsInsufficientFundsException()
    {
        var account = OpenAccount(initialBalance: 100m);

        var act = () => account.Withdraw(100.01m, Now);

        var exception = act.Should().Throw<InsufficientFundsException>().Which;
        exception.AccountId.Should().Be(account.Id);
        exception.Balance.Should().Be(100m);
        exception.RequestedAmount.Should().Be(100.01m);
        account.Balance.Should().Be(100m);
    }

    [Fact]
    public void Withdraw_FromEmptyAccount_ThrowsInsufficientFundsException()
    {
        var account = OpenAccount();

        var act = () => account.Withdraw(1m, Now);

        act.Should().Throw<InsufficientFundsException>();
    }

    [Theory]
    [InlineData(AccountStatus.Frozen)]
    [InlineData(AccountStatus.Closed)]
    public void Withdraw_OnInactiveAccount_ThrowsAccountNotActiveException(AccountStatus status)
    {
        var account = OpenAccount(initialBalance: status == AccountStatus.Frozen ? 100m : 0m);
        MakeInactive(account, status);
        var balanceBefore = account.Balance;

        var act = () => account.Withdraw(10m, Now);

        act.Should().Throw<AccountNotActiveException>().Which.Status.Should().Be(status);
        account.Balance.Should().Be(balanceBefore);
    }

    [Fact]
    public void Withdraw_OnFrozenAccountWithInsufficientFunds_ReportsInactiveFirst()
    {
        var account = OpenAccount();
        account.Freeze();

        var act = () => account.Withdraw(10m, Now);

        act.Should().Throw<AccountNotActiveException>();
    }

    [Fact]
    public void Withdraw_WithTooLongReference_ThrowsAndLeavesBalanceUnchanged()
    {
        var account = OpenAccount(initialBalance: 100m);
        var reference = new string('r', Transaction.ReferenceMaxLength + 1);

        var act = () => account.Withdraw(10m, Now, reference);

        act.Should().Throw<DomainValidationException>();
        account.Balance.Should().Be(100m);
    }

    [Fact]
    public void DepositsAndWithdrawals_InSequence_TrackRunningBalance()
    {
        var account = OpenAccount();

        var first = account.Deposit(100m, Now);
        var second = account.Withdraw(30.50m, Now);
        var third = account.Deposit(0.75m, Now);

        first.BalanceAfter.Should().Be(100m);
        second.BalanceAfter.Should().Be(69.50m);
        third.BalanceAfter.Should().Be(70.25m);
        account.Balance.Should().Be(70.25m);
    }

    // ---- Status changes ---------------------------------------------------------------------

    [Fact]
    public void Freeze_ActiveAccount_SetsStatusToFrozen()
    {
        var account = OpenAccount();

        account.Freeze();

        account.Status.Should().Be(AccountStatus.Frozen);
    }

    [Fact]
    public void Freeze_FrozenAccount_ThrowsInvalidAccountStateException()
    {
        var account = OpenAccount();
        account.Freeze();

        var act = account.Freeze;

        act.Should().Throw<InvalidAccountStateException>();
    }

    [Fact]
    public void Unfreeze_FrozenAccount_SetsStatusToActive()
    {
        var account = OpenAccount();
        account.Freeze();

        account.Unfreeze();

        account.Status.Should().Be(AccountStatus.Active);
    }

    [Theory]
    [InlineData(AccountStatus.Active)]
    [InlineData(AccountStatus.Closed)]
    public void Unfreeze_AccountThatIsNotFrozen_ThrowsInvalidAccountStateException(AccountStatus status)
    {
        var account = OpenAccount();
        MakeInactiveOrKeepActive(account, status);

        var act = account.Unfreeze;

        act.Should().Throw<InvalidAccountStateException>();
    }

    [Fact]
    public void Close_AccountWithZeroBalance_SetsStatusToClosed()
    {
        var account = OpenAccount();

        account.Close();

        account.Status.Should().Be(AccountStatus.Closed);
    }

    [Fact]
    public void Close_FrozenAccountWithZeroBalance_SetsStatusToClosed()
    {
        var account = OpenAccount();
        account.Freeze();

        account.Close();

        account.Status.Should().Be(AccountStatus.Closed);
    }

    [Fact]
    public void Close_AccountWithBalance_ThrowsInvalidAccountStateException()
    {
        var account = OpenAccount(initialBalance: 0.01m);

        var act = account.Close;

        act.Should().Throw<InvalidAccountStateException>().WithMessage("*balance*");
        account.Status.Should().Be(AccountStatus.Active);
    }

    [Fact]
    public void Close_ClosedAccount_ThrowsInvalidAccountStateException()
    {
        var account = OpenAccount();
        account.Close();

        var act = account.Close;

        act.Should().Throw<InvalidAccountStateException>().WithMessage("*already closed*");
    }

    [Fact]
    public void Freeze_ClosedAccount_ThrowsInvalidAccountStateException()
    {
        var account = OpenAccount();
        account.Close();

        var act = account.Freeze;

        act.Should().Throw<InvalidAccountStateException>();
    }

    private static void MakeInactive(Account account, AccountStatus status)
    {
        if (status == AccountStatus.Frozen)
        {
            account.Freeze();
        }
        else if (status == AccountStatus.Closed)
        {
            account.Close();
        }
    }

    private static void MakeInactiveOrKeepActive(Account account, AccountStatus status)
    {
        if (status != AccountStatus.Active)
        {
            MakeInactive(account, status);
        }
    }
}
