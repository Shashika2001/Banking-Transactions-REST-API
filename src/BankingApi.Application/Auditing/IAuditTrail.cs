namespace BankingApi.Application.Auditing;

/// <summary>
/// Records audit entries. Entries are saved together with the rest of the unit of work, so an operation and its
/// audit record are committed (or rolled back) together.
/// </summary>
public interface IAuditTrail
{
    /// <summary>
    /// Adds an audit entry for the current user to the pending unit of work.
    /// </summary>
    /// <param name="action">The action performed (see <see cref="AuditActions"/>).</param>
    /// <param name="entityName">The affected entity type.</param>
    /// <param name="entityId">The affected entity identifier.</param>
    /// <param name="details">Optional details, serialized to JSON.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the entry has been added.</returns>
    Task RecordAsync(
        string action,
        string entityName,
        Guid entityId,
        object? details = null,
        CancellationToken cancellationToken = default);
}
