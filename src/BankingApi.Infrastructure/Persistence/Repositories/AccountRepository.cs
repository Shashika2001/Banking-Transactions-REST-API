using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAccountRepository"/>.
/// </summary>
internal sealed class AccountRepository : Repository<Account>, IAccountRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public AccountRepository(BankingDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <inheritdoc />
    public Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(account => account.AccountNumber == accountNumber, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Account>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(account => account.CustomerId == customerId)
            .OrderBy(account => account.CreatedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(account => account.AccountNumber == accountNumber, cancellationToken);
}
