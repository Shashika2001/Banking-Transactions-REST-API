using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Application.Common.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/>. Translates EF Core and SQL Server errors into
/// Application-layer exceptions so callers do not depend on persistence details.
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    // SQL Server error numbers for unique index (2601) and unique constraint (2627) violations.
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    private readonly BankingDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    public UnitOfWork(
        BankingDbContext dbContext,
        ICustomerRepository customers,
        IAccountRepository accounts,
        ITransactionRepository transactions,
        IAuditLogRepository auditLogs)
    {
        _dbContext = dbContext;
        Customers = customers;
        Accounts = accounts;
        Transactions = transactions;
        AuditLogs = auditLogs;
    }

    /// <inheritdoc />
    public ICustomerRepository Customers { get; }

    /// <inheritdoc />
    public IAccountRepository Accounts { get; }

    /// <inheritdoc />
    public ITransactionRepository Transactions { get; }

    /// <inheritdoc />
    public IAuditLogRepository AuditLogs { get; }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(
                "The data was modified by another request. Reload and try again.",
                exception);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw new DuplicateKeyException("A record with the same unique key already exists.", exception);
        }
    }

    /// <inheritdoc />
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // With retry-on-failure enabled, a user transaction must run inside the execution strategy so the
        // whole unit (reads, writes and commit) is retried together on transient failures.
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        var attempt = 0;

        try
        {
            return await strategy.ExecuteAsync(
                async token =>
                {
                    if (attempt++ > 0)
                    {
                        // Discard entities tracked by the failed attempt before re-running.
                        _dbContext.ChangeTracker.Clear();
                    }

                    await using var transaction = await _dbContext.Database.BeginTransactionAsync(token);
                    var result = await operation(token);
                    await transaction.CommitAsync(token);
                    return result;
                },
                cancellationToken);
        }
        catch
        {
            // The transaction was rolled back; drop the in-memory changes so they cannot be saved later.
            _dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation };
}
