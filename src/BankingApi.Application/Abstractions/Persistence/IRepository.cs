using BankingApi.Domain.Common;

namespace BankingApi.Application.Abstractions.Persistence;

/// <summary>
/// Generic data access for an entity type. Changes are persisted by <see cref="IUnitOfWork.SaveChangesAsync"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IRepository<TEntity>
    where TEntity : Entity
{
    /// <summary>
    /// Gets an entity by its identifier.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The entity, or <see langword="null"/> if it does not exist.</returns>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an entity with the given identifier exists.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the entity exists; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a new entity to be inserted on the next save.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the entity is tracked.</returns>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
}
