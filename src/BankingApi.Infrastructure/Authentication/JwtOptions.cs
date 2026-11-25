using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BankingApi.Infrastructure.Authentication;

/// <summary>
/// JWT settings, bound from the <c>Jwt</c> configuration section. <see cref="SigningKey"/> is a secret: set it with
/// user-secrets or the <c>Jwt__SigningKey</c> environment variable, never in appsettings or source code.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>The minimum signing key size for HMAC-SHA256 (256 bits).</summary>
    public const int MinimumSigningKeyBytes = 32;

    /// <summary>Gets or sets the token issuer.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Gets or sets the token audience.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Gets or sets the HMAC signing key (at least 32 bytes when UTF-8 encoded).</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Gets or sets how long access tokens are valid, in minutes.</summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 60;

    /// <summary>
    /// Creates the symmetric key used to sign and validate tokens.
    /// </summary>
    /// <returns>The signing key.</returns>
    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));

    /// <summary>
    /// Checks the settings.
    /// </summary>
    /// <returns><see langword="true"/> when every setting is usable.</returns>
    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(Issuer) &&
        !string.IsNullOrWhiteSpace(Audience) &&
        Encoding.UTF8.GetByteCount(SigningKey) >= MinimumSigningKeyBytes &&
        AccessTokenLifetimeMinutes is > 0 and <= 1440;
}
