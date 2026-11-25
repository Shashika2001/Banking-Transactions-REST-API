namespace BankingApi.Application.Transfers;

/// <summary>
/// Request to transfer money between two accounts.
/// </summary>
/// <param name="SourceAccountId">The account to take money from.</param>
/// <param name="DestinationAccountId">The account to pay money into.</param>
/// <param name="Amount">The positive amount, with at most two decimal places.</param>
/// <param name="Reference">An optional free-text reference.</param>
public sealed record TransferRequest(Guid SourceAccountId, Guid DestinationAccountId, decimal Amount, string? Reference);

/// <summary>
/// A completed transfer. The destination balance is deliberately not included.
/// </summary>
/// <param name="TransferId">The transfer identifier.</param>
/// <param name="SourceAccountId">The account money was taken from.</param>
/// <param name="DestinationAccountId">The account money was paid into.</param>
/// <param name="Amount">The amount transferred.</param>
/// <param name="SourceBalanceAfter">The source account balance after the transfer.</param>
/// <param name="Reference">The optional free-text reference.</param>
/// <param name="CreatedAt">When the transfer happened (UTC).</param>
public sealed record TransferResponse(
    Guid TransferId,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    decimal SourceBalanceAfter,
    string? Reference,
    DateTimeOffset CreatedAt);

/// <summary>
/// The outcome of a transfer request.
/// </summary>
/// <param name="Transfer">The transfer.</param>
/// <param name="IsReplay">
/// <see langword="true"/> when the idempotency key had already been used and the original result was returned
/// without moving money again.
/// </param>
public sealed record TransferResult(TransferResponse Transfer, bool IsReplay);
