using System.Text.Json;
using BankingApi.Application.Abstractions;
using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Domain.Entities;

namespace BankingApi.Application.Auditing;

/// <summary>
/// Default <see cref="IAuditTrail"/> that stores entries through <see cref="IAuditLogRepository"/>.
/// </summary>
public sealed class AuditTrail : IAuditTrail
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IAuditLogRepository _auditLogs;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditTrail"/> class.
    /// </summary>
    /// <param name="auditLogs">The audit log repository.</param>
    /// <param name="currentUser">The current user.</param>
    /// <param name="timeProvider">The clock.</param>
    public AuditTrail(IAuditLogRepository auditLogs, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _auditLogs = auditLogs;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public Task RecordAsync(
        string action,
        string entityName,
        Guid entityId,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        var serializedDetails = details is null ? null : JsonSerializer.Serialize(details, SerializerOptions);
        var entry = AuditLog.Create(
            _currentUser.UserId,
            action,
            entityName,
            entityId.ToString(),
            serializedDetails,
            _timeProvider.GetUtcNow());

        return _auditLogs.AddAsync(entry, cancellationToken);
    }
}
