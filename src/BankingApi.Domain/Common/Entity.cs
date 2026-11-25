namespace BankingApi.Domain.Common;

/// <summary>
/// Base type for entities identified by a <see cref="Guid"/>.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Entity"/> class with a new identifier.
    /// </summary>
    protected Entity()
        : this(Guid.NewGuid())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Entity"/> class with the given identifier.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    protected Entity(Guid id)
    {
        Id = id;
    }

    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    public Guid Id { get; private set; }
}
