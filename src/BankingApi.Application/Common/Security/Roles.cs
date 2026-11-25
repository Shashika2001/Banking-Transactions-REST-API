using BankingApi.Domain.Enums;

namespace BankingApi.Application.Common.Security;

/// <summary>
/// Role names as they appear in access tokens and <c>[Authorize(Roles = ...)]</c> attributes.
/// </summary>
public static class Roles
{
    /// <summary>The <see cref="UserRole.Customer"/> role.</summary>
    public const string Customer = nameof(UserRole.Customer);

    /// <summary>The <see cref="UserRole.Admin"/> role.</summary>
    public const string Admin = nameof(UserRole.Admin);
}
