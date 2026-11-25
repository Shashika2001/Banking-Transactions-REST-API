using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankingApi.Infrastructure.Persistence;

/// <summary>
/// Applies EF Core migrations at startup.
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Creates the database if needed and applies any pending migrations. Intended for Development and demos;
    /// production deployments should apply migrations as a separate, reviewed step.
    /// </summary>
    /// <param name="services">A scoped service provider.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the database is up to date.</returns>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var dbContext = services.GetRequiredService<BankingDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
