namespace BankingApi.Infrastructure.Authentication;

/// <summary>
/// Claim names used in access tokens. Short JWT-style names are used, and inbound claim mapping is disabled,
/// so these names reach the API unchanged.
/// </summary>
public static class BankingClaimTypes
{
    /// <summary>The user identifier.</summary>
    public const string Subject = "sub";

    /// <summary>The user's email address.</summary>
    public const string Email = "email";

    /// <summary>The user's role.</summary>
    public const string Role = "role";

    /// <summary>The customer the user signs in as (customer users only).</summary>
    public const string CustomerId = "customer_id";

    /// <summary>A unique token identifier.</summary>
    public const string TokenId = "jti";
}
