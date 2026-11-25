namespace BankingApi.Domain.Enums;

/// <summary>
/// The kind of money movement recorded by a <see cref="Entities.Transaction"/>.
/// </summary>
public enum TransactionType
{
    /// <summary>Money paid into the account.</summary>
    Deposit = 1,

    /// <summary>Money taken out of the account.</summary>
    Withdrawal = 2,

    /// <summary>Money received from another account.</summary>
    TransferIn = 3,

    /// <summary>Money sent to another account.</summary>
    TransferOut = 4,
}
