namespace BankingApi.Domain.Exceptions;

/// <summary>
/// Raised when money is moved between accounts that hold different currencies.
/// </summary>
public sealed class CurrencyMismatchException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CurrencyMismatchException"/> class.
    /// </summary>
    /// <param name="sourceCurrency">The source account currency.</param>
    /// <param name="destinationCurrency">The destination account currency.</param>
    public CurrencyMismatchException(string sourceCurrency, string destinationCurrency)
        : base($"Cannot transfer between accounts in different currencies ({sourceCurrency} to {destinationCurrency}).")
    {
        SourceCurrency = sourceCurrency;
        DestinationCurrency = destinationCurrency;
    }

    /// <summary>Gets the source account currency.</summary>
    public string SourceCurrency { get; }

    /// <summary>Gets the destination account currency.</summary>
    public string DestinationCurrency { get; }
}
