using BankingApi.Domain.Common;
using BankingApi.Domain.Enums;
using BankingApi.Domain.Exceptions;

namespace BankingApi.Domain.Entities;

/// <summary>
/// A customer's bank account. All balance changes go through this entity so its rules are always enforced.
/// </summary>
public sealed class Account : Entity
{
    /// <summary>Maximum length of <see cref="AccountNumber"/> (the IBAN maximum).</summary>
    public const int AccountNumberMaxLength = 34;

    /// <summary>Length of an ISO 4217 currency code.</summary>
    public const int CurrencyCodeLength = 3;

    /// <summary>Number of decimal places stored for money amounts.</summary>
    public const int MoneyDecimalPlaces = 2;

    /// <summary>Largest amount or balance that fits the <c>decimal(18,2)</c> storage column.</summary>
    public const decimal MaxMoneyValue = 9_999_999_999_999_999.99m;

    // Required by EF Core for materialization.
    private Account()
    {
        AccountNumber = string.Empty;
        Currency = string.Empty;
        RowVersion = [];
    }

    private Account(string accountNumber, Guid customerId, string currency, DateTimeOffset createdAt)
    {
        AccountNumber = accountNumber;
        CustomerId = customerId;
        Currency = currency;
        Status = AccountStatus.Active;
        CreatedAt = createdAt;
        RowVersion = [];
    }

    /// <summary>Gets the unique, human-facing account number.</summary>
    public string AccountNumber { get; private set; }

    /// <summary>Gets the identifier of the owning customer.</summary>
    public Guid CustomerId { get; private set; }

    /// <summary>Gets the current balance.</summary>
    public decimal Balance { get; private set; }

    /// <summary>Gets the ISO 4217 currency code of the account (for example, <c>USD</c>).</summary>
    public string Currency { get; private set; }

    /// <summary>Gets the account's lifecycle status.</summary>
    public AccountStatus Status { get; private set; }

    /// <summary>Gets the concurrency token, set by the database on every update.</summary>
    public byte[] RowVersion { get; private set; }

    /// <summary>Gets when the account was opened (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Opens a new, active account with a zero balance.
    /// </summary>
    /// <param name="accountNumber">The unique account number.</param>
    /// <param name="customerId">The owning customer.</param>
    /// <param name="currency">A three-letter ISO 4217 currency code.</param>
    /// <param name="createdAt">When the account was opened.</param>
    /// <returns>The new account.</returns>
    /// <exception cref="DomainValidationException">Thrown when any value is missing or malformed.</exception>
    public static Account Open(string accountNumber, Guid customerId, string currency, DateTimeOffset createdAt)
    {
        return new Account(
            Guard.RequiredText(accountNumber, nameof(AccountNumber), AccountNumberMaxLength),
            Guard.NotEmpty(customerId, nameof(CustomerId)),
            ValidateCurrency(currency),
            createdAt);
    }

    /// <summary>
    /// Pays money into the account.
    /// </summary>
    /// <param name="amount">The positive amount to deposit, with at most two decimal places.</param>
    /// <param name="occurredAt">When the deposit happened.</param>
    /// <param name="reference">An optional free-text reference.</param>
    /// <param name="idempotencyKey">An optional client-supplied key that prevents duplicate processing.</param>
    /// <returns>The transaction recording the deposit.</returns>
    /// <exception cref="InvalidAmountException">
    /// Thrown when the amount is not positive, has too many decimal places, or would push the balance above <see cref="MaxMoneyValue"/>.
    /// </exception>
    /// <exception cref="AccountNotActiveException">Thrown when the account is not active.</exception>
    /// <exception cref="DomainValidationException">Thrown when the reference or idempotency key is blank or too long.</exception>
    public Transaction Deposit(decimal amount, DateTimeOffset occurredAt, string? reference = null, string? idempotencyKey = null) =>
        Credit(TransactionType.Deposit, amount, occurredAt, reference, idempotencyKey);

    /// <summary>
    /// Takes money out of the account.
    /// </summary>
    /// <param name="amount">The positive amount to withdraw, with at most two decimal places.</param>
    /// <param name="occurredAt">When the withdrawal happened.</param>
    /// <param name="reference">An optional free-text reference.</param>
    /// <param name="idempotencyKey">An optional client-supplied key that prevents duplicate processing.</param>
    /// <returns>The transaction recording the withdrawal.</returns>
    /// <exception cref="InvalidAmountException">Thrown when the amount is not positive or has too many decimal places.</exception>
    /// <exception cref="AccountNotActiveException">Thrown when the account is not active.</exception>
    /// <exception cref="InsufficientFundsException">Thrown when the balance is lower than the amount.</exception>
    /// <exception cref="DomainValidationException">Thrown when the reference or idempotency key is blank or too long.</exception>
    public Transaction Withdraw(decimal amount, DateTimeOffset occurredAt, string? reference = null, string? idempotencyKey = null) =>
        Debit(TransactionType.Withdrawal, amount, occurredAt, reference, idempotencyKey);

    /// <summary>
    /// Moves money from this account to another account of the same currency. Every rule is checked before either
    /// balance changes, so a rejected transfer leaves both accounts untouched.
    /// </summary>
    /// <param name="destination">The account receiving the money.</param>
    /// <param name="amount">The positive amount to transfer, with at most two decimal places.</param>
    /// <param name="occurredAt">When the transfer happened.</param>
    /// <param name="reference">An optional free-text reference recorded on both legs.</param>
    /// <param name="idempotencyKey">An optional client-supplied key, recorded on the outgoing leg only.</param>
    /// <returns>The outgoing and incoming transactions, linked by a shared transfer identifier.</returns>
    /// <exception cref="SameAccountTransferException">Thrown when <paramref name="destination"/> is this account.</exception>
    /// <exception cref="CurrencyMismatchException">Thrown when the accounts use different currencies.</exception>
    /// <exception cref="InvalidAmountException">
    /// Thrown when the amount is not positive, has too many decimal places, or would push the destination balance
    /// above <see cref="MaxMoneyValue"/>.
    /// </exception>
    /// <exception cref="AccountNotActiveException">Thrown when either account is not active.</exception>
    /// <exception cref="InsufficientFundsException">Thrown when this account's balance is lower than the amount.</exception>
    /// <exception cref="DomainValidationException">Thrown when the reference or idempotency key is blank or too long.</exception>
    public TransferLegs TransferTo(
        Account destination,
        decimal amount,
        DateTimeOffset occurredAt,
        string? reference = null,
        string? idempotencyKey = null)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (destination.Id == Id)
        {
            throw new SameAccountTransferException(Id);
        }

        if (!string.Equals(Currency, destination.Currency, StringComparison.Ordinal))
        {
            throw new CurrencyMismatchException(Currency, destination.Currency);
        }

        EnsureValidAmount(amount);
        EnsureActive();
        destination.EnsureActive();

        if (amount > Balance)
        {
            throw new InsufficientFundsException(Id, Balance, amount);
        }

        if (amount > MaxMoneyValue - destination.Balance)
        {
            throw new InvalidAmountException(amount, $"the destination balance would exceed the maximum of {MaxMoneyValue}.");
        }

        var transferId = Guid.NewGuid();
        var outgoing = Transaction.Record(
            Id, TransactionType.TransferOut, amount, Balance - amount, occurredAt, reference, idempotencyKey, transferId);
        var incoming = Transaction.Record(
            destination.Id, TransactionType.TransferIn, amount, destination.Balance + amount, occurredAt, reference, null, transferId);

        Balance = outgoing.BalanceAfter;
        destination.Balance = incoming.BalanceAfter;

        return new TransferLegs(transferId, outgoing, incoming);
    }

    /// <summary>
    /// Freezes an active account, blocking all money movements.
    /// </summary>
    /// <exception cref="InvalidAccountStateException">Thrown when the account is not active.</exception>
    public void Freeze()
    {
        EnsureStatus(AccountStatus.Active, "freeze");
        Status = AccountStatus.Frozen;
    }

    /// <summary>
    /// Re-activates a frozen account.
    /// </summary>
    /// <exception cref="InvalidAccountStateException">Thrown when the account is not frozen.</exception>
    public void Unfreeze()
    {
        EnsureStatus(AccountStatus.Frozen, "unfreeze");
        Status = AccountStatus.Active;
    }

    /// <summary>
    /// Permanently closes the account. The balance must be zero.
    /// </summary>
    /// <exception cref="InvalidAccountStateException">Thrown when the account is already closed or still holds money.</exception>
    public void Close()
    {
        if (Status == AccountStatus.Closed)
        {
            throw new InvalidAccountStateException($"Account {Id} is already closed.");
        }

        if (Balance != 0m)
        {
            throw new InvalidAccountStateException($"Account {Id} cannot be closed while its balance is {Balance}.");
        }

        Status = AccountStatus.Closed;
    }

    private Transaction Credit(TransactionType type, decimal amount, DateTimeOffset occurredAt, string? reference, string? idempotencyKey)
    {
        EnsureValidAmount(amount);
        EnsureActive();

        if (amount > MaxMoneyValue - Balance)
        {
            throw new InvalidAmountException(amount, $"the resulting balance would exceed the maximum of {MaxMoneyValue}.");
        }

        return Apply(type, amount, Balance + amount, occurredAt, reference, idempotencyKey);
    }

    private Transaction Debit(TransactionType type, decimal amount, DateTimeOffset occurredAt, string? reference, string? idempotencyKey)
    {
        EnsureValidAmount(amount);
        EnsureActive();

        if (amount > Balance)
        {
            throw new InsufficientFundsException(Id, Balance, amount);
        }

        return Apply(type, amount, Balance - amount, occurredAt, reference, idempotencyKey);
    }

    // Builds (and so validates) the transaction before touching the balance, so a rejected
    // transaction never leaves the account in a changed state.
    private Transaction Apply(
        TransactionType type,
        decimal amount,
        decimal newBalance,
        DateTimeOffset occurredAt,
        string? reference,
        string? idempotencyKey)
    {
        var transaction = Transaction.Record(Id, type, amount, newBalance, occurredAt, reference, idempotencyKey);
        Balance = newBalance;
        return transaction;
    }

    private static void EnsureValidAmount(decimal amount)
    {
        if (amount <= 0m)
        {
            throw new InvalidAmountException(amount, "the amount must be greater than zero.");
        }

        if (decimal.Round(amount, MoneyDecimalPlaces) != amount)
        {
            throw new InvalidAmountException(amount, $"the amount must have at most {MoneyDecimalPlaces} decimal places.");
        }
    }

    private void EnsureActive()
    {
        if (Status != AccountStatus.Active)
        {
            throw new AccountNotActiveException(Id, Status);
        }
    }

    private void EnsureStatus(AccountStatus required, string operation)
    {
        if (Status != required)
        {
            throw new InvalidAccountStateException($"Cannot {operation} account {Id} because it is {Status}.");
        }
    }

    private static string ValidateCurrency(string currency)
    {
        var code = Guard.RequiredText(currency, nameof(Currency), CurrencyCodeLength);
        if (code.Length != CurrencyCodeLength || !code.All(char.IsAsciiLetterUpper))
        {
            throw new DomainValidationException($"{nameof(Currency)} must be a three-letter uppercase ISO 4217 code.");
        }

        return code;
    }
}
