using BankingApi.Application.Abstractions.Persistence;
using BankingApi.Application.Common.Models;
using BankingApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICustomerRepository"/>.
/// </summary>
internal sealed class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerRepository"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public CustomerRepository(BankingDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <inheritdoc />
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(customer => customer.Email == email, cancellationToken);

    /// <inheritdoc />
    public Task<bool> NationalIdExistsAsync(string nationalId, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(customer => customer.NationalId == nationalId, cancellationToken);

    /// <inheritdoc />
    public async Task<PagedResult<Customer>> ListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var totalCount = await Set.CountAsync(cancellationToken);
        var items = await Set.AsNoTracking()
            .OrderBy(customer => customer.CreatedAt)
            .ThenBy(customer => customer.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Customer>(items, pageNumber, pageSize, totalCount);
    }
}
