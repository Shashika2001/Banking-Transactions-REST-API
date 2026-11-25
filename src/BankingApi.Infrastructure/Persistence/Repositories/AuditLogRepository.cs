using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAuditLogRepository"/>.
/// </summary>
internal sealed class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public AuditLogRepository(BankingDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditLog>> GetByEntityAsync(
        string entityName,
        string entityId,
        CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking()
            .Where(auditLog => auditLog.EntityName == entityName && auditLog.EntityId == entityId)
            .OrderByDescending(auditLog => auditLog.Timestamp)
            .ToListAsync(cancellationToken);
}
