namespace BankingApi.Application.Common.Exceptions;

/// <summary>
/// Raised when saving would violate a unique constraint, such as a reused idempotency key or account number.
/// </summary>
public sealed class DuplicateKeyException : ConflictException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateKeyException"/> class.
    /// </summary>
    public DuplicateKeyException()
        : base("A record with the same unique key already exists.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateKeyException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DuplicateKeyException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateKeyException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying persistence exception.</param>
    public DuplicateKeyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
