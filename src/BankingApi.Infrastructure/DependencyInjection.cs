using BankingApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BankingApi.Infrastructure;

/// <summary>
/// Registers Infrastructure-layer services with the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// The name of the connection string for the banking database.
    /// </summary>
    public const string ConnectionStringName = "BankingDb";

    /// <summary>
    /// Adds Infrastructure-layer services such as the database context and repositories.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <see cref="ConnectionStringName"/> connection string is not configured.
    /// </exception>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured. " +
                "Set it with user-secrets or the ConnectionStrings__BankingDb environment variable.");
        }

        services.AddDbContext<BankingDbContext>(options => options.UseSqlServer(connectionString));

        return services;
    }
}
