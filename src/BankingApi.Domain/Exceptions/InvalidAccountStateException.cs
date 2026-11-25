namespace BankingApi.Domain.Exceptions;

/// <summary>
/// Raised when an account status change is not allowed from the account's current state.
/// </summary>
public sealed class InvalidAccountStateException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidAccountStateException"/> class.
    /// </summary>
    /// <param name="message">A description of the disallowed change.</param>
    public InvalidAccountStateException(string message)
        : base(message)
    {
    }
}
