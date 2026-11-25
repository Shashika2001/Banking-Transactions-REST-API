namespace BankingApi.Application.Accounts;

/// <summary>
/// Request to open an account.
/// </summary>
/// <param name="CustomerId">The owning customer.</param>
/// <param name="Currency">A three-letter ISO 4217 currency code (for example, <c>USD</c>).</param>
public sealed record OpenAccountRequest(Guid CustomerId, string Currency);

/// <summary>
/// Request to deposit or withdraw money.
/// </summary>
/// <param name="Amount">The positive amount, with at most two decimal places.</param>
/// <param name="Reference">An optional free-text reference.</param>
public sealed record MoneyOperationRequest(decimal Amount, string? Reference);

/// <summary>
/// An account as returned by the API.
/// </summary>
/// <param name="Id">The account identifier.</param>
/// <param name="AccountNumber">The account number.</param>
/// <param name="CustomerId">The owning customer.</param>
/// <param name="Balance">The current balance.</param>
/// <param name="Currency">The ISO 4217 currency code.</param>
/// <param name="Status">The account status (<c>Active</c>, <c>Frozen</c> or <c>Closed</c>).</param>
/// <param name="CreatedAt">When the account was opened (UTC).</param>
public sealed record AccountResponse(
    Guid Id,
    string AccountNumber,
    Guid CustomerId,
    decimal Balance,
    string Currency,
    string Status,
    DateTimeOffset CreatedAt);

/// <summary>
/// An account balance.
/// </summary>
/// <param name="AccountId">The account identifier.</param>
/// <param name="AccountNumber">The account number.</param>
/// <param name="Balance">The current balance.</param>
/// <param name="Currency">The ISO 4217 currency code.</param>
/// <param name="AsOf">When the balance was read (UTC).</param>
public sealed record BalanceResponse(Guid AccountId, string AccountNumber, decimal Balance, string Currency, DateTimeOffset AsOf);
