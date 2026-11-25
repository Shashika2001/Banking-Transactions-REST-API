using BankingApi.Domain.Enums;

namespace BankingApi.Domain.Exceptions;

/// <summary>
/// Raised when money is moved on an account that is not <see cref="AccountStatus.Active"/>.
/// </summary>
public sealed class AccountNotActiveException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountNotActiveException"/> class.
    /// </summary>
    /// <param name="accountId">The inactive account.</param>
    /// <param name="status">The account's current status.</param>
    public AccountNotActiveException(Guid accountId, AccountStatus status)
        : base($"Account {accountId} is {status} and cannot be used for transactions.")
    {
        AccountId = accountId;
        Status = status;
    }

    /// <summary>Gets the inactive account.</summary>
    public Guid AccountId { get; }

    /// <summary>Gets the account's current status.</summary>
    public AccountStatus Status { get; }
}
