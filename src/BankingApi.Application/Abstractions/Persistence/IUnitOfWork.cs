using BankingApi.Application.Common.Exceptions;

namespace BankingApi.Application.Abstractions.Persistence;

/// <summary>
/// Groups repository changes so they are saved atomically.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Gets the customer repository.</summary>
    ICustomerRepository Customers { get; }

    /// <summary>Gets the account repository.</summary>
    IAccountRepository Accounts { get; }

    /// <summary>Gets the transaction repository.</summary>
    ITransactionRepository Transactions { get; }

    /// <summary>Gets the audit log repository.</summary>
    IAuditLogRepository AuditLogs { get; }

    /// <summary>
    /// Saves all pending changes in a single database transaction.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of rows written.</returns>
    /// <exception cref="ConcurrencyConflictException">Thrown when another request changed the same data first.</exception>
    /// <exception cref="DuplicateKeyException">Thrown when a unique constraint (such as an idempotency key) is violated.</exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
