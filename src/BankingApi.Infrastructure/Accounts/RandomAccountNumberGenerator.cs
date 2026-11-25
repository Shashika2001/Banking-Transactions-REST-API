using System.Security.Cryptography;
using BankingApi.Application.Abstractions;

namespace BankingApi.Infrastructure.Accounts;

/// <summary>
/// Generates random 12-digit account numbers using a cryptographically secure random number generator,
/// so account numbers cannot be guessed from one another.
/// </summary>
internal sealed class RandomAccountNumberGenerator : IAccountNumberGenerator
{
    /// <summary>The number of digits in a generated account number.</summary>
    public const int Length = 12;

    /// <inheritdoc />
    public string Generate()
    {
        Span<char> digits = stackalloc char[Length];
        digits[0] = (char)('1' + RandomNumberGenerator.GetInt32(9));
        for (var index = 1; index < Length; index++)
        {
            digits[index] = (char)('0' + RandomNumberGenerator.GetInt32(10));
        }

        return new string(digits);
    }
}
