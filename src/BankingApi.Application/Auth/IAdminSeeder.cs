namespace BankingApi.Application.Auth;

/// <summary>
/// Creates the first admin account from <see cref="BootstrapAdminOptions"/>.
/// </summary>
public interface IAdminSeeder
{
    /// <summary>
    /// Creates the configured admin if one is configured and no user with that email exists.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if an admin was created; otherwise <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the configured password is too short or too long.</exception>
    Task<bool> SeedAsync(CancellationToken cancellationToken = default);
}
