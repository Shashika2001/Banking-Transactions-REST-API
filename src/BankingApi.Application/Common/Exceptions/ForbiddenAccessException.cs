namespace BankingApi.Application.Common.Exceptions;

/// <summary>
/// Raised when the caller is authenticated but not allowed to access a resource.
/// </summary>
public sealed class ForbiddenAccessException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenAccessException"/> class.
    /// </summary>
    public ForbiddenAccessException()
        : base("The caller is not allowed to access this resource.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenAccessException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ForbiddenAccessException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenAccessException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public ForbiddenAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
