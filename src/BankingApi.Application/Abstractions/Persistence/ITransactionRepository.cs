using BankingApi.Application.Common.Models;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;

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
    /// <returns>The transaction, or <see langword="null"/> if the key has not been used. Not tracked.</returns>
    Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets both legs of a transfer.
    /// </summary>
    /// <param name="transferId">The transfer identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The transfer's transactions. Not tracked.</returns>
    Task<IReadOnlyList<Transaction>> GetByTransferIdAsync(Guid transferId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one page of an account's transactions, newest first, optionally limited to a time range.
    /// </summary>
    /// <param name="accountId">The account.</param>
    /// <param name="fromInclusive">Optional inclusive lower bound on <see cref="Transaction.CreatedAt"/>.</param>
    /// <param name="toExclusive">Optional exclusive upper bound on <see cref="Transaction.CreatedAt"/>.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The number of transactions per page.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The page of transactions. Not tracked.</returns>
    Task<PagedResult<Transaction>> GetHistoryAsync(
        Guid accountId,
        DateTimeOffset? fromInclusive,
        DateTimeOffset? toExclusive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sums the amounts of an account's transactions of one type within a time range.
    /// </summary>
    /// <param name="accountId">The account.</param>
    /// <param name="type">The transaction type to include.</param>
    /// <param name="fromInclusive">Inclusive lower bound on <see cref="Transaction.CreatedAt"/>.</param>
    /// <param name="toExclusive">Exclusive upper bound on <see cref="Transaction.CreatedAt"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The total amount, or zero when there are no matching transactions.</returns>
    Task<decimal> GetTotalAmountAsync(
        Guid accountId,
        TransactionType type,
        DateTimeOffset fromInclusive,
        DateTimeOffset toExclusive,
        CancellationToken cancellationToken = default);
}
