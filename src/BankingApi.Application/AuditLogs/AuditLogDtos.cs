using BankingApi.Application.Common.Models;

namespace BankingApi.Application.AuditLogs;

/// <summary>
/// Filters and paging for searching audit logs. Every filter is optional.
/// </summary>
public sealed record AuditLogQuery : PageRequest
{
    /// <summary>Gets the user who performed the action.</summary>
    public string? UserId { get; init; }

    /// <summary>Gets the action (for example, <c>Transfer</c>).</summary>
    public string? Action { get; init; }

    /// <summary>Gets the affected entity type (for example, <c>Account</c>).</summary>
    public string? EntityName { get; init; }

    /// <summary>Gets the affected entity identifier.</summary>
    public string? EntityId { get; init; }

    /// <summary>Gets the inclusive start of the time range.</summary>
    public DateTimeOffset? From { get; init; }

    /// <summary>Gets the exclusive end of the time range.</summary>
    public DateTimeOffset? To { get; init; }
}

/// <summary>
/// An audit log entry as returned by the API.
/// </summary>
/// <param name="Id">The entry identifier.</param>
/// <param name="UserId">The user who performed the action.</param>
/// <param name="Action">The action performed.</param>
/// <param name="EntityName">The affected entity type.</param>
/// <param name="EntityId">The affected entity identifier.</param>
/// <param name="Details">Optional JSON details.</param>
/// <param name="Timestamp">When the action happened (UTC).</param>
public sealed record AuditLogResponse(
    Guid Id,
    string UserId,
    string Action,
    string EntityName,
    string EntityId,
    string? Details,
    DateTimeOffset Timestamp);
