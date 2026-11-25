namespace BankingApi.Application.Auditing;

/// <summary>
/// Names of audited actions, as stored in <see cref="Domain.Entities.AuditLog.Action"/>.
/// </summary>
public static class AuditActions
{
    /// <summary>A customer was created.</summary>
    public const string CustomerCreated = "CustomerCreated";

    /// <summary>An account was opened.</summary>
    public const string AccountOpened = "AccountOpened";

    /// <summary>Money was deposited.</summary>
    public const string Deposit = "Deposit";

    /// <summary>Money was withdrawn.</summary>
    public const string Withdrawal = "Withdrawal";

    /// <summary>Money was transferred between accounts.</summary>
    public const string Transfer = "Transfer";
}
