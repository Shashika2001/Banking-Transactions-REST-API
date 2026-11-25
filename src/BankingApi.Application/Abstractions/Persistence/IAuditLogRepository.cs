using BankingApi.Application.AuditLogs;
using BankingApi.Application.Common.Models;
using BankingApi.Domain.Entities;

namespace BankingApi.Application.Abstractions.Persistence;

/// <summary>
/// Data access for <see cref="AuditLog"/> entries.
/// </summary>
public interface IAuditLogRepository : IRepository<AuditLog>
{
    /// <summary>
    /// Searches audit entries, newest first. Every filter in <paramref name="query"/> is optional.
    /// </summary>
    /// <param name="query">Filters and paging.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The requested page. Not tracked.</returns>
    Task<PagedResult<AuditLog>> SearchAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
}
