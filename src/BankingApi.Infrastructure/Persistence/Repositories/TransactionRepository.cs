using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Application.Common.Models;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;
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
    public async Task<IReadOnlyList<Transaction>> GetByTransferIdAsync(Guid transferId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(transaction => transaction.TransferId == transferId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResult<Transaction>> GetHistoryAsync(
        Guid accountId,
        DateTimeOffset? fromInclusive,
        DateTimeOffset? toExclusive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var query = Set.AsNoTracking().Where(transaction => transaction.AccountId == accountId);
        if (fromInclusive.HasValue)
        {
            query = query.Where(transaction => transaction.CreatedAt >= fromInclusive.Value);
        }

        if (toExclusive.HasValue)
        {
            query = query.Where(transaction => transaction.CreatedAt < toExclusive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(transaction => transaction.CreatedAt)
            .ThenByDescending(transaction => transaction.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Transaction>(items, pageNumber, pageSize, totalCount);
    }

    /// <inheritdoc />
    public Task<decimal> GetTotalAmountAsync(
        Guid accountId,
        TransactionType type,
        DateTimeOffset fromInclusive,
        DateTimeOffset toExclusive,
        CancellationToken cancellationToken = default) =>
        Set.Where(transaction =>
                transaction.AccountId == accountId &&
                transaction.Type == type &&
                transaction.CreatedAt >= fromInclusive &&
                transaction.CreatedAt < toExclusive)
            .SumAsync(transaction => transaction.Amount, cancellationToken);
}
