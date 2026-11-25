using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRepository{TEntity}"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
internal class Repository<TEntity> : IRepository<TEntity>
    where TEntity : Entity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Repository{TEntity}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public Repository(BankingDbContext dbContext)
    {
        DbContext = dbContext;
    }

    /// <summary>Gets the database context.</summary>
    protected BankingDbContext DbContext { get; }

    /// <summary>Gets the entity set.</summary>
    protected DbSet<TEntity> Set => DbContext.Set<TEntity>();

    /// <inheritdoc />
    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(entity => entity.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await Set.AddAsync(entity, cancellationToken);
    }
}
