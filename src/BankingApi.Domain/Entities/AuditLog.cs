using BankingApi.Domain.Common;

namespace BankingApi.Domain.Entities;

/// <summary>
/// An append-only record of who did what to which entity, and when.
/// </summary>
public sealed class AuditLog : Entity
{
    /// <summary>Maximum length of <see cref="UserId"/>.</summary>
    public const int UserIdMaxLength = 128;

    /// <summary>Maximum length of <see cref="Action"/>.</summary>
    public const int ActionMaxLength = 64;

    /// <summary>Maximum length of <see cref="EntityName"/>.</summary>
    public const int EntityNameMaxLength = 128;

    /// <summary>Maximum length of <see cref="EntityId"/>.</summary>
    public const int EntityIdMaxLength = 64;

    /// <summary>Maximum length of <see cref="Details"/>.</summary>
    public const int DetailsMaxLength = 4000;

    // Required by EF Core for materialization.
    private AuditLog()
    {
        UserId = string.Empty;
        Action = string.Empty;
        EntityName = string.Empty;
        EntityId = string.Empty;
    }

    private AuditLog(string userId, string action, string entityName, string entityId, string? details, DateTimeOffset timestamp)
    {
        UserId = userId;
        Action = action;
        EntityName = entityName;
        EntityId = entityId;
        Details = details;
        Timestamp = timestamp;
    }

    /// <summary>Gets the identifier of the user who performed the action.</summary>
    public string UserId { get; private set; }

    /// <summary>Gets the action performed (for example, <c>Deposit</c>).</summary>
    public string Action { get; private set; }

    /// <summary>Gets the name of the affected entity type (for example, <c>Account</c>).</summary>
    public string EntityName { get; private set; }

    /// <summary>Gets the identifier of the affected entity.</summary>
    public string EntityId { get; private set; }

    /// <summary>Gets optional details about the action.</summary>
    public string? Details { get; private set; }

    /// <summary>Gets when the action happened (UTC).</summary>
    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>
    /// Creates a new audit log entry.
    /// </summary>
    /// <param name="userId">The user who performed the action.</param>
    /// <param name="action">The action performed.</param>
    /// <param name="entityName">The affected entity type.</param>
    /// <param name="entityId">The affected entity identifier.</param>
    /// <param name="details">Optional details.</param>
    /// <param name="timestamp">When the action happened.</param>
    /// <returns>The new audit log entry.</returns>
    /// <exception cref="Exceptions.DomainValidationException">Thrown when a required value is missing or too long.</exception>
    public static AuditLog Create(
        string userId,
        string action,
        string entityName,
        string entityId,
        string? details,
        DateTimeOffset timestamp)
    {
        return new AuditLog(
            Guard.RequiredText(userId, nameof(UserId), UserIdMaxLength),
            Guard.RequiredText(action, nameof(Action), ActionMaxLength),
            Guard.RequiredText(entityName, nameof(EntityName), EntityNameMaxLength),
            Guard.RequiredText(entityId, nameof(EntityId), EntityIdMaxLength),
            Guard.OptionalText(details, nameof(Details), DetailsMaxLength),
            timestamp);
    }
}
