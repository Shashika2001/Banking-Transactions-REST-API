using BankingApi.Application.Auth;

namespace BankingApi.Api.Configuration;

/// <summary>
/// Startup task that creates the first admin account.
/// </summary>
public static partial class AdminSeeding
{
    /// <summary>
    /// Creates the admin configured in <see cref="BootstrapAdminOptions"/> (user-secrets / environment variables),
    /// if one is configured and does not exist yet.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <returns>A task that completes when seeding has finished.</returns>
    public static async Task SeedBootstrapAdminAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        await using var scope = app.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IAdminSeeder>();
        if (await seeder.SeedAsync())
        {
            LogAdminCreated(app.Logger);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Created the bootstrap admin account from configuration.")]
    private static partial void LogAdminCreated(ILogger logger);
}
