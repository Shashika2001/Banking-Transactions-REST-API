using BankingApi.Application.Abstractions;
using Microsoft.AspNetCore.Identity;
using IdentityResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult;
using PasswordVerificationResult = BankingApi.Application.Abstractions.PasswordVerificationResult;

namespace BankingApi.Infrastructure.Authentication;

/// <summary>
/// <see cref="IPasswordHasher"/> backed by ASP.NET Core Identity's <see cref="PasswordHasher{TUser}"/>
/// (PBKDF2 with HMAC-SHA512, a random salt per password, and versioned hashes that can be upgraded on sign-in).
/// </summary>
internal sealed class IdentityPasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<HashSubject> Hasher = new();
    private static readonly HashSubject Subject = new();

    // Hash of a random throwaway password, computed once, used to equalize timing for unknown users.
    private static readonly Lazy<string> DummyHash = new(() => Hasher.HashPassword(Subject, Guid.NewGuid().ToString()));

    /// <inheritdoc />
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        return Hasher.HashPassword(Subject, password);
    }

    /// <inheritdoc />
    public PasswordVerificationResult Verify(string? hashedPassword, string providedPassword)
    {
        ArgumentNullException.ThrowIfNull(providedPassword);

        if (hashedPassword is null)
        {
            Hasher.VerifyHashedPassword(Subject, DummyHash.Value, providedPassword);
            return PasswordVerificationResult.Failed;
        }

        return Hasher.VerifyHashedPassword(Subject, hashedPassword, providedPassword) switch
        {
            IdentityResult.Success => PasswordVerificationResult.Success,
            IdentityResult.SuccessRehashNeeded => PasswordVerificationResult.SuccessRehashNeeded,
            _ => PasswordVerificationResult.Failed,
        };
    }

    // PasswordHasher<TUser> requires a user type but does not use the instance.
    private sealed class HashSubject
    {
    }
}
