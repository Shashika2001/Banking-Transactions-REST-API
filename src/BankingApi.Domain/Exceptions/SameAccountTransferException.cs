namespace BankingApi.Domain.Exceptions;

/// <summary>
/// Raised when a transfer names the same account as both source and destination.
/// </summary>
public sealed class SameAccountTransferException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SameAccountTransferException"/> class.
    /// </summary>
    /// <param name="accountId">The account used as both source and destination.</param>
    public SameAccountTransferException(Guid accountId)
        : base($"Cannot transfer from account {accountId} to itself.")
    {
        AccountId = accountId;
    }

    /// <summary>Gets the account used as both source and destination.</summary>
    public Guid AccountId { get; }
}
