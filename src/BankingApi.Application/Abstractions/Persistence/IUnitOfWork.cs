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

    /// <summary>Gets the user repository.</summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Saves all pending changes in a single database transaction.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of rows written.</returns>
    /// <exception cref="ConcurrencyConflictException">Thrown when another request changed the same data first.</exception>
    /// <exception cref="DuplicateKeyException">Thrown when a unique constraint (such as an idempotency key) is violated.</exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs an operation inside a single database transaction. The transaction is committed when the operation
    /// completes and rolled back if it throws, so either all of its changes are saved or none are.
    /// The operation must call <see cref="SaveChangesAsync"/> itself, and may be re-run on transient failures.
    /// </summary>
    /// <typeparam name="TResult">The operation's result type.</typeparam>
    /// <param name="operation">The operation to run.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The operation's result.</returns>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}
