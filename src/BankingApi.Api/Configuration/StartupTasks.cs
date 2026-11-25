using BankingApi.Application.Auth;
using BankingApi.Application.Seeding;
using BankingApi.Infrastructure.Persistence;

namespace BankingApi.Api.Configuration;

/// <summary>
/// One-off tasks run before the API starts accepting requests.
/// </summary>
public static partial class StartupTasks
{
    /// <summary>
    /// In Development, applies database migrations. In every environment, creates the bootstrap admin if configured.
    /// In Development, then creates demo data if enabled.
    /// </summary>
    /// <param name="app">The application.</param>
    /// <returns>A task that completes when all startup tasks have finished.</returns>
    public static async Task RunStartupTasksAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var isDevelopment = app.Environment.IsDevelopment();

        if (isDevelopment)
        {
            await services.MigrateDatabaseAsync();
            LogMigrationsApplied(app.Logger);
        }

        if (await services.GetRequiredService<IAdminSeeder>().SeedAsync())
        {
            LogAdminCreated(app.Logger);
        }

        if (isDevelopment && await services.GetRequiredService<IDemoDataSeeder>().SeedAsync())
        {
            LogDemoDataCreated(app.Logger);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Database migrations applied.")]
    private static partial void LogMigrationsApplied(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created the bootstrap admin account from configuration.")]
    private static partial void LogAdminCreated(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created demo customers, accounts and transactions.")]
    private static partial void LogDemoDataCreated(ILogger logger);
}
