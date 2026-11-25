namespace BankingApi.Domain.Exceptions;

/// <summary>
/// Raised when a debit would take an account balance below zero.
/// </summary>
public sealed class InsufficientFundsException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InsufficientFundsException"/> class.
    /// </summary>
    /// <param name="accountId">The account that lacks funds.</param>
    /// <param name="balance">The current balance.</param>
    /// <param name="requestedAmount">The amount that was requested.</param>
    public InsufficientFundsException(Guid accountId, decimal balance, decimal requestedAmount)
        : base($"Account {accountId} has insufficient funds: balance {balance}, requested {requestedAmount}.")
    {
        AccountId = accountId;
        Balance = balance;
        RequestedAmount = requestedAmount;
    }

    /// <summary>Gets the account that lacks funds.</summary>
    public Guid AccountId { get; }

    /// <summary>Gets the balance at the time of the request.</summary>
    public decimal Balance { get; }

    /// <summary>Gets the amount that was requested.</summary>
    public decimal RequestedAmount { get; }
}
