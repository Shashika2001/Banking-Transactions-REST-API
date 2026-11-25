using BankingApi.Application.Common.Models;
using BankingApi.Domain.Entities;

namespace BankingApi.Application.Abstractions.Persistence;

/// <summary>
/// Data access for <see cref="Customer"/> entities.
/// </summary>
public interface ICustomerRepository : IRepository<Customer>
{
    /// <summary>
    /// Checks whether a customer with the given email address exists.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if a customer uses the email; otherwise <see langword="false"/>.</returns>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a customer with the given national identification number exists.
    /// </summary>
    /// <param name="nationalId">The national identification number.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if a customer uses the national ID; otherwise <see langword="false"/>.</returns>
    Task<bool> NationalIdExistsAsync(string nationalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one page of customers, oldest first.
    /// </summary>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The number of customers per page.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The page of customers. Returned entities are read-only (not tracked).</returns>
    Task<PagedResult<Customer>> ListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
