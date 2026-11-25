using BankingApi.Application.Common.Exceptions;
using BankingApi.Application.Common.Models;
using FluentValidation;

namespace BankingApi.Application.AuditLogs;

/// <summary>
/// Audit log queries (admin only).
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Searches audit entries, newest first.
    /// </summary>
    /// <param name="query">Filters and paging.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The requested page.</returns>
    /// <exception cref="ForbiddenAccessException">Thrown when the caller is not an admin.</exception>
    /// <exception cref="ValidationException">Thrown when the query is invalid.</exception>
    Task<PagedResult<AuditLogResponse>> SearchAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
}
