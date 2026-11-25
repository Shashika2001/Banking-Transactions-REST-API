using BankingApi.Domain.Entities;

namespace BankingApi.Application.Abstractions.Persistence;

/// <summary>
/// Data access for <see cref="Transaction"/> entities.
/// </summary>
public interface ITransactionRepository : IRepository<Transaction>
{
    /// <summary>
    /// Gets the transaction recorded with an idempotency key, if any.
    /// </summary>
    /// <param name="idempotencyKey">The client-supplied idempotency key.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The transaction, or <see langword="null"/> if the key has not been used.</returns>
    Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one page of an account's transactions, newest first.
    /// </summary>
    /// <param name="accountId">The account.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The number of transactions per page.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The page of transactions. Returned entities are read-only (not tracked).</returns>
    Task<IReadOnlyList<Transaction>> GetByAccountIdAsync(
        Guid accountId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts an account's transactions.
    /// </summary>
    /// <param name="accountId">The account.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of transactions.</returns>
    Task<int> CountByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);
}
