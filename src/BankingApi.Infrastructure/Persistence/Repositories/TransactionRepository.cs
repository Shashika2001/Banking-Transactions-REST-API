using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ITransactionRepository"/>.
/// </summary>
internal sealed class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public TransactionRepository(BankingDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <inheritdoc />
    public Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().FirstOrDefaultAsync(transaction => transaction.IdempotencyKey == idempotencyKey, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Transaction>> GetByAccountIdAsync(
        Guid accountId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        return await Set.AsNoTracking()
            .Where(transaction => transaction.AccountId == accountId)
            .OrderByDescending(transaction => transaction.CreatedAt)
            .ThenByDescending(transaction => transaction.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> CountByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        Set.CountAsync(transaction => transaction.AccountId == accountId, cancellationToken);
}
