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

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation };
}
