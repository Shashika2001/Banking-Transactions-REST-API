using BankingApi.Domain.Entities;

namespace BankingApi.Application.Abstractions.Persistence;

/// <summary>
/// Data access for <see cref="AuditLog"/> entries.
/// </summary>
public interface IAuditLogRepository : IRepository<AuditLog>
{
    /// <summary>
    /// Gets the audit history of one entity, newest first.
    /// </summary>
    /// <param name="entityName">The entity type name.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The audit entries. Returned entities are read-only (not tracked).</returns>
    Task<IReadOnlyList<AuditLog>> GetByEntityAsync(string entityName, string entityId, CancellationToken cancellationToken = default);
}
