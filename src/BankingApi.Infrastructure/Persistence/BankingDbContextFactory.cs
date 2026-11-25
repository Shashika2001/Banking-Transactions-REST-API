using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BankingApi.Infrastructure.Persistence;

/// <summary>
/// Creates a <see cref="BankingDbContext"/> for EF Core command-line tools (for example, <c>dotnet ef migrations add</c>),
/// without starting the web host.
/// </summary>
/// <remarks>
/// Commands that connect to the database (such as <c>dotnet ef database update</c>) read the connection string from
/// the <c>ConnectionStrings__BankingDb</c> environment variable, or the <c>--connection</c> option.
/// </remarks>
public sealed class BankingDbContextFactory : IDesignTimeDbContextFactory<BankingDbContext>
{
    // Used only to build the model when no connection string is supplied; contains no credentials.
    private const string DesignTimeFallbackConnectionString =
        "Server=localhost;Database=BankingDb;Trusted_Connection=True;TrustServerCertificate=True";

    /// <inheritdoc />
    public BankingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__BankingDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = DesignTimeFallbackConnectionString;
        }

        var options = new DbContextOptionsBuilder<BankingDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new BankingDbContext(options);
    }
}
