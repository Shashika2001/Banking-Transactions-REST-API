using BankingApi.Domain.Entities;

namespace BankingApi.Application.Abstractions;

/// <summary>
/// Issues access tokens for signed-in users.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates a signed access token for a user.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <returns>The token and its expiry time.</returns>
    AccessToken CreateToken(User user);
}

/// <summary>
/// A signed access token.
/// </summary>
/// <param name="Token">The encoded token.</param>
/// <param name="ExpiresAt">When the token expires (UTC).</param>
public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);
