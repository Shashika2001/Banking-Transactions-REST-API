using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for the banking database.
/// </summary>
public sealed class BankingDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BankingDbContext"/> class.
    /// </summary>
    /// <param name="options">The options used to configure the context.</param>
    public BankingDbContext(DbContextOptions<BankingDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets the customers.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Gets the accounts.</summary>
    public DbSet<Account> Accounts => Set<Account>();

    /// <summary>Gets the transactions.</summary>
    public DbSet<Transaction> Transactions => Set<Transaction>();

    /// <summary>Gets the audit log entries.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BankingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
