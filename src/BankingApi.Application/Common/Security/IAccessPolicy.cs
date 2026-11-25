using BankingApi.Application.Common.Exceptions;
using BankingApi.Domain.Entities;

namespace BankingApi.Application.Common.Security;

/// <summary>
/// Resource-level authorization rules: customers may only access their own data; admins may access everything.
/// Services call this before returning or changing data, so the rules hold no matter how a service is called.
/// </summary>
public interface IAccessPolicy
{
    /// <summary>
    /// Ensures the caller is an admin.
    /// </summary>
    /// <exception cref="ForbiddenAccessException">Thrown when the caller is not an admin.</exception>
    void EnsureAdmin();

    /// <summary>
    /// Ensures the caller may access a customer's data: an admin, or the customer themselves.
    /// </summary>
    /// <param name="customerId">The customer whose data is requested.</param>
    /// <exception cref="ForbiddenAccessException">Thrown when the caller may not access the customer.</exception>
    void EnsureCanAccessCustomer(Guid customerId);

    /// <summary>
    /// Ensures the caller may access an account: an admin, or the customer who owns it.
    /// </summary>
    /// <param name="account">The account.</param>
    /// <exception cref="ForbiddenAccessException">Thrown when the caller may not access the account.</exception>
    void EnsureCanAccessAccount(Account account);
}
