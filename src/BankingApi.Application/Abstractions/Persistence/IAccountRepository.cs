using BankingApi.Domain.Entities;

namespace BankingApi.Application.Abstractions.Persistence;

/// <summary>
/// Data access for <see cref="Account"/> entities.
/// </summary>
public interface IAccountRepository : IRepository<Account>
{
    /// <summary>
    /// Gets an account by its account number.
    /// </summary>
    /// <param name="accountNumber">The account number.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The account, or <see langword="null"/> if it does not exist.</returns>
    Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all accounts owned by a customer.
    /// </summary>
    /// <param name="customerId">The owning customer.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The customer's accounts, oldest first. Returned entities are read-only (not tracked).</returns>
    Task<IReadOnlyList<Account>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an account number is already in use.
    /// </summary>
    /// <param name="accountNumber">The account number.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the number is in use; otherwise <see langword="false"/>.</returns>
    Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken = default);
}
