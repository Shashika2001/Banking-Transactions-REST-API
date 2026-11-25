namespace BankingApi.Application.Common.Exceptions;

/// <summary>
/// Raised when data could not be saved because another request modified it first (optimistic concurrency).
/// The caller may reload the data and retry.
/// </summary>
public sealed class ConcurrencyConflictException : ConflictException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConflictException"/> class.
    /// </summary>
    public ConcurrencyConflictException()
        : base("The data was modified by another request. Reload and try again.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConflictException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConflictException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying persistence exception.</param>
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
