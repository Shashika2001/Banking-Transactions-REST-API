namespace BankingApi.Domain.Exceptions;

/// <summary>
/// Raised when data supplied to the domain does not satisfy an invariant (for example, a required field is missing).
/// </summary>
public sealed class DomainValidationException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainValidationException"/> class.
    /// </summary>
    /// <param name="message">A description of the validation failure.</param>
    public DomainValidationException(string message)
        : base(message)
    {
    }
}
