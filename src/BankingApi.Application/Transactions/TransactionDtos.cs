using BankingApi.Application.Common.Models;

namespace BankingApi.Application.Transactions;

/// <summary>
/// A transaction as returned by the API.
/// </summary>
/// <param name="Id">The transaction identifier.</param>
/// <param name="AccountId">The account the transaction belongs to.</param>
/// <param name="Type">The transaction type (<c>Deposit</c>, <c>Withdrawal</c>, <c>TransferIn</c> or <c>TransferOut</c>).</param>
/// <param name="Amount">The amount moved (always positive).</param>
/// <param name="BalanceAfter">The account balance immediately after the transaction.</param>
/// <param name="Reference">The optional free-text reference.</param>
/// <param name="TransferId">The transfer this transaction is part of, if any.</param>
/// <param name="CreatedAt">When the transaction happened (UTC).</param>
public sealed record TransactionResponse(
    Guid Id,
    Guid AccountId,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    string? Reference,
    Guid? TransferId,
    DateTimeOffset CreatedAt);

/// <summary>
/// Paging and date filter for an account's transaction history.
/// </summary>
public sealed record TransactionHistoryQuery : PageRequest
{
    /// <summary>Gets the optional inclusive start of the time range.</summary>
    public DateTimeOffset? From { get; init; }

    /// <summary>Gets the optional exclusive end of the time range.</summary>
    public DateTimeOffset? To { get; init; }
}
