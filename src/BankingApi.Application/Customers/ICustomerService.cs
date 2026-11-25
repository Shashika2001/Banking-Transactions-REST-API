using BankingApi.Application.Common.Exceptions;
using BankingApi.Application.Common.Models;
using FluentValidation;

namespace BankingApi.Application.Customers;

/// <summary>
/// Customer use cases.
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// Creates a customer.
    /// </summary>
    /// <param name="request">The customer details.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created customer.</returns>
    /// <exception cref="ValidationException">Thrown when the request is invalid.</exception>
    /// <exception cref="ConflictException">Thrown when the email or national ID is already registered.</exception>
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a customer by identifier.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The customer.</returns>
    /// <exception cref="NotFoundException">Thrown when the customer does not exist.</exception>
    Task<CustomerResponse> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists customers one page at a time, oldest first.
    /// </summary>
    /// <param name="request">The paging parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The requested page.</returns>
    /// <exception cref="ValidationException">Thrown when the paging parameters are invalid.</exception>
    Task<PagedResult<CustomerResponse>> ListAsync(PageRequest request, CancellationToken cancellationToken = default);
}
