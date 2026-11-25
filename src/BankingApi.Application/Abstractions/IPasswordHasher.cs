namespace BankingApi.Application.Abstractions;

/// <summary>
/// Hashes and verifies passwords.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password for storage.
    /// </summary>
    /// <param name="password">The plain-text password.</param>
    /// <returns>The hash, including its salt and algorithm parameters.</returns>
    string Hash(string password);

    /// <summary>
    /// Verifies a password against a stored hash.
    /// </summary>
    /// <param name="hashedPassword">
    /// The stored hash, or <see langword="null"/> when the user does not exist. With <see langword="null"/> the
    /// password is checked against a dummy hash, so a missing user takes as long as a wrong password and response
    /// times do not reveal which email addresses are registered. The result is then always
    /// <see cref="PasswordVerificationResult.Failed"/>.
    /// </param>
    /// <param name="providedPassword">The password supplied by the caller.</param>
    /// <returns>The verification result.</returns>
    PasswordVerificationResult Verify(string? hashedPassword, string providedPassword);
}

/// <summary>
/// The outcome of verifying a password.
/// </summary>
public enum PasswordVerificationResult
{
    /// <summary>The password is wrong.</summary>
    Failed = 0,

    /// <summary>The password is correct.</summary>
    Success = 1,

    /// <summary>The password is correct, but the stored hash uses outdated settings and should be replaced.</summary>
    SuccessRehashNeeded = 2,
}
