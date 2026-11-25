using BankingApi.Application.AuditLogs;
using BankingApi.Application.Common.Models;
using BankingApi.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Api.Controllers;

/// <summary>
/// Reads the audit log. Admin only.
/// </summary>
[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = Roles.Admin)]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogsController"/> class.
    /// </summary>
    /// <param name="auditLogService">The audit log service.</param>
    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Searches audit entries, newest first.
    /// </summary>
    /// <param name="query">
    /// Optional filters (<c>userId</c>, <c>action</c>, <c>entityName</c>, <c>entityId</c>, <c>from</c> inclusive,
    /// <c>to</c> exclusive) and paging (<c>pageNumber</c>, <c>pageSize</c> up to 100).
    /// </param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>One page of audit entries.</returns>
    [HttpGet]
    [ProducesResponseType<PagedResult<AuditLogResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AuditLogResponse>>> Search(
        [FromQuery] AuditLogQuery query,
        CancellationToken cancellationToken) =>
        await _auditLogService.SearchAsync(query, cancellationToken);
}
