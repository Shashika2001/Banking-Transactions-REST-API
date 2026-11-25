namespace BankingApi.Application.Common.Exceptions;

/// <summary>
/// Raised when sign-in fails. The message never says whether the email or the password was wrong.
/// </summary>
public sealed class InvalidCredentialsException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCredentialsException"/> class.
    /// </summary>
    public InvalidCredentialsException()
        : base("Invalid email or password.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCredentialsException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidCredentialsException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCredentialsException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public InvalidCredentialsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
