using BankingApi.Application.Abstractions.Persistence;
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
}
