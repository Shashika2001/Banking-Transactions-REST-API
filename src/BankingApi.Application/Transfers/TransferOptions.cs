namespace BankingApi.Application.Transfers;

/// <summary>
/// Configuration for transfers, bound from the <c>Transfers</c> configuration section.
/// </summary>
public sealed class TransferOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Transfers";

    /// <summary>
    /// Gets or sets the maximum total amount each account may transfer out per UTC calendar day.
    /// </summary>
    public decimal DailyLimitPerAccount { get; set; } = 10_000m;
}
