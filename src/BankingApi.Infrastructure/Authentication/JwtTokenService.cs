using BankingApi.Application.Abstractions;
using BankingApi.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BankingApi.Infrastructure.Authentication;

/// <summary>
/// Issues HMAC-SHA256 signed JWT access tokens.
/// </summary>
internal sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly SigningCredentials _signingCredentials;
    private readonly JsonWebTokenHandler _tokenHandler = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
    /// </summary>
    /// <param name="options">The JWT settings.</param>
    /// <param name="timeProvider">The clock.</param>
    public JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
        _timeProvider = timeProvider;
        _signingCredentials = new SigningCredentials(_options.CreateSigningKey(), SecurityAlgorithms.HmacSha256);
    }

    /// <inheritdoc />
    public AccessToken CreateToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = _timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var claims = new Dictionary<string, object>
        {
            [BankingClaimTypes.Subject] = user.Id.ToString(),
            [BankingClaimTypes.Email] = user.Email,
            [BankingClaimTypes.Role] = user.Role.ToString(),
            [BankingClaimTypes.TokenId] = Guid.NewGuid().ToString(),
        };
        if (user.CustomerId is { } customerId)
        {
            claims[BankingClaimTypes.CustomerId] = customerId.ToString();
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Claims = claims,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = _signingCredentials,
        };

        return new AccessToken(_tokenHandler.CreateToken(descriptor), expiresAt);
    }
}
