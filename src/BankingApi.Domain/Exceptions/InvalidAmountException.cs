namespace BankingApi.Domain.Exceptions;

/// <summary>
/// Raised when a monetary amount is not valid for an operation (for example, zero or negative).
/// </summary>
public sealed class InvalidAmountException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidAmountException"/> class.
    /// </summary>
    /// <param name="amount">The rejected amount.</param>
    /// <param name="reason">Why the amount was rejected.</param>
    public InvalidAmountException(decimal amount, string reason)
        : base($"Amount {amount} is invalid: {reason}")
    {
        Amount = amount;
    }

    /// <summary>
    /// Gets the rejected amount.
    /// </summary>
    public decimal Amount { get; }
}
