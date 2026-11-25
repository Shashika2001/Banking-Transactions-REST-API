using BankingApi.Application.Accounts;
using BankingApi.Application.AuditLogs;
using BankingApi.Application.Auth;
using BankingApi.Application.Auditing;
using BankingApi.Application.Common.Security;
using BankingApi.Application.Customers;
using BankingApi.Application.Seeding;
using BankingApi.Application.Transactions;
using BankingApi.Application.Transfers;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BankingApi.Application;

/// <summary>
/// Registers Application-layer services with the dependency injection container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Application-layer services, validators and options.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<TransferOptions>()
            .Bind(configuration.GetSection(TransferOptions.SectionName))
            .Validate(options => options.DailyLimitPerAccount > 0m, "Transfers:DailyLimitPerAccount must be greater than zero.")
            .ValidateOnStart();

        services.AddOptions<BootstrapAdminOptions>()
            .Bind(configuration.GetSection(BootstrapAdminOptions.SectionName));

        services.AddOptions<DemoDataOptions>()
            .Bind(configuration.GetSection(DemoDataOptions.SectionName));

        services.AddValidatorsFromAssemblyContaining<AuditTrail>(ServiceLifetime.Singleton);
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IAuditTrail, AuditTrail>();
        services.AddScoped<IAccessPolicy, AccessPolicy>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminSeeder, AdminSeeder>();
        services.AddScoped<IDemoDataSeeder, DemoDataSeeder>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ITransferService, TransferService>();

        return services;
    }
}
