using BankingApi.Domain.Entities;

namespace BankingApi.Application.Abstractions.Persistence;

/// <summary>
/// Data access for <see cref="User"/> entities.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Gets a user by sign-in email address.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The user, or <see langword="null"/> if none exists.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a user with the given email address exists.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the email is registered; otherwise <see langword="false"/>.</returns>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
}
