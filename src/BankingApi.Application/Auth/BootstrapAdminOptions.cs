namespace BankingApi.Application.Auth;

/// <summary>
/// Optional first admin account, created at startup if it does not exist. Bound from the <c>BootstrapAdmin</c>
/// configuration section; set the values with user-secrets or environment variables, never in source control.
/// </summary>
public sealed class BootstrapAdminOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "BootstrapAdmin";

    /// <summary>Gets or sets the admin's sign-in email address. Leave empty to skip seeding.</summary>
    public string? Email { get; set; }

    /// <summary>Gets or sets the admin's password. Leave empty to skip seeding.</summary>
    public string? Password { get; set; }
}
