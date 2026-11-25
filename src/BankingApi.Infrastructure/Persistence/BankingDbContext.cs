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

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BankingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
