namespace BankingApi.Application.Seeding;

/// <summary>
/// Creates demo customers, logins, accounts and transactions for local development and demos.
/// </summary>
public interface IDemoDataSeeder
{
    /// <summary>
    /// Creates the demo data if it is enabled and has not been created yet.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if demo data was created; otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the configured password is too short or too long.</exception>
    Task<bool> SeedAsync(CancellationToken cancellationToken = default);
}
