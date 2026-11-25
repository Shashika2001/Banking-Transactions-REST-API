namespace BankingApi.Application.Seeding;

/// <summary>
/// Settings for the demo data created in Development, bound from the <c>DemoData</c> configuration section.
/// </summary>
public sealed class DemoDataOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "DemoData";

    /// <summary>Gets or sets a value indicating whether demo data should be created.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the password given to every demo customer login. Set it with user-secrets or an environment
    /// variable; demo data is skipped when it is empty.
    /// </summary>
    public string? UserPassword { get; set; }
}
