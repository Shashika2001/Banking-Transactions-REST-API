namespace BankingApi.Domain.Exceptions;

/// <summary>
/// Raised when a transfer would take an account's outgoing transfers for the day above the daily limit.
/// </summary>
public sealed class DailyTransferLimitExceededException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DailyTransferLimitExceededException"/> class.
    /// </summary>
    /// <param name="accountId">The source account.</param>
    /// <param name="dailyLimit">The configured daily limit.</param>
    /// <param name="transferredToday">The amount already transferred out today.</param>
    /// <param name="requestedAmount">The amount of the rejected transfer.</param>
    public DailyTransferLimitExceededException(Guid accountId, decimal dailyLimit, decimal transferredToday, decimal requestedAmount)
        : base(
            $"Transfer of {requestedAmount} from account {accountId} exceeds the daily limit of {dailyLimit} " +
            $"({transferredToday} already transferred today; {Math.Max(0m, dailyLimit - transferredToday)} remaining).")
    {
        AccountId = accountId;
        DailyLimit = dailyLimit;
        TransferredToday = transferredToday;
        RequestedAmount = requestedAmount;
    }

    /// <summary>Gets the source account.</summary>
    public Guid AccountId { get; }

    /// <summary>Gets the configured daily limit.</summary>
    public decimal DailyLimit { get; }

    /// <summary>Gets the amount already transferred out today.</summary>
    public decimal TransferredToday { get; }

    /// <summary>Gets the amount of the rejected transfer.</summary>
    public decimal RequestedAmount { get; }
}
