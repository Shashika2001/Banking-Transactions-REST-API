using BankingApi.Application.Abstractions;
using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Infrastructure.Accounts;
using BankingApi.Infrastructure.Authentication;
using BankingApi.Infrastructure.Persistence;
using BankingApi.Infrastructure.Persistence.Repositories;
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

        services.AddDbContext<BankingDbContext>(options =>
            options.UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure()));

        services.AddHealthChecks().AddDbContextCheck<BankingDbContext>("database");

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IAccountNumberGenerator, RandomAccountNumberGenerator>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                options => options.IsValid(),
                $"Jwt settings are invalid: Issuer and Audience are required, Jwt:SigningKey must be at least " +
                $"{JwtOptions.MinimumSigningKeyBytes} bytes (set it with user-secrets or the Jwt__SigningKey " +
                "environment variable), and AccessTokenLifetimeMinutes must be 1-1440.")
            .ValidateOnStart();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
