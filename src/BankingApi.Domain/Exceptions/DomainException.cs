namespace BankingApi.Domain.Exceptions;

/// <summary>
/// Base type for exceptions raised when a domain rule is violated.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">A description of the violated rule.</param>
    protected DomainException(string message)
        : base(message)
    {
    }
}
