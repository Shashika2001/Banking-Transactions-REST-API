namespace BankingApi.Domain.Enums;

/// <summary>
/// Lifecycle status of an <see cref="Entities.Account"/>.
/// </summary>
public enum AccountStatus
{
    /// <summary>The account can send and receive money.</summary>
    Active = 1,

    /// <summary>The account is temporarily blocked; no money movements are allowed.</summary>
    Frozen = 2,

    /// <summary>The account is permanently closed.</summary>
    Closed = 3,
}
