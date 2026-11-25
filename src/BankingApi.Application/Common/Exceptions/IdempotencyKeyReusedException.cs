namespace BankingApi.Application.Common.Exceptions;

/// <summary>
/// Raised when an idempotency key is reused for a request that differs from the original one.
/// </summary>
public sealed class IdempotencyKeyReusedException : ConflictException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotencyKeyReusedException"/> class.
    /// </summary>
    public IdempotencyKeyReusedException()
        : base("The idempotency key was already used for a different request.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotencyKeyReusedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public IdempotencyKeyReusedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotencyKeyReusedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public IdempotencyKeyReusedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
