using BankingApi.Application.Common.Exceptions;
using FluentValidation;

namespace BankingApi.Application.Auth;

/// <summary>
/// Registration and sign-in.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new customer with a <c>Customer</c>-role login, and signs them in.
    /// </summary>
    /// <param name="request">The customer details and password.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The access token and the created customer.</returns>
    /// <exception cref="ValidationException">Thrown when the request is invalid.</exception>
    /// <exception cref="ConflictException">Thrown when the email or national ID is already registered.</exception>
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs a user in.
    /// </summary>
    /// <param name="request">The email address and password.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The access token.</returns>
    /// <exception cref="ValidationException">Thrown when the request is malformed.</exception>
    /// <exception cref="InvalidCredentialsException">Thrown when the email or password is wrong.</exception>
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
