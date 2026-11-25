using BankingApi.Application.Customers;

namespace BankingApi.Application.Auth;

/// <summary>
/// Request to register as a new customer.
/// </summary>
/// <param name="FullName">The customer's full name.</param>
/// <param name="Email">The email address, also used to sign in.</param>
/// <param name="NationalId">The customer's national identification number.</param>
/// <param name="Password">The password (12–128 characters).</param>
public sealed record RegisterRequest(string FullName, string Email, string NationalId, string Password);

/// <summary>
/// Request to sign in.
/// </summary>
/// <param name="Email">The sign-in email address.</param>
/// <param name="Password">The password.</param>
public sealed record LoginRequest(string Email, string Password);

/// <summary>
/// An access token and the signed-in user.
/// </summary>
/// <param name="AccessToken">The bearer token to send in the <c>Authorization</c> header.</param>
/// <param name="TokenType">Always <c>Bearer</c>.</param>
/// <param name="ExpiresAt">When the token expires (UTC).</param>
/// <param name="UserId">The user identifier.</param>
/// <param name="Email">The sign-in email address.</param>
/// <param name="Role">The user's role (<c>Customer</c> or <c>Admin</c>).</param>
/// <param name="CustomerId">The linked customer, for customer users.</param>
public sealed record AuthResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string Email,
    string Role,
    Guid? CustomerId);

/// <summary>
/// The result of a successful registration.
/// </summary>
/// <param name="Auth">The access token for the new user.</param>
/// <param name="Customer">The created customer, with the national ID masked.</param>
public sealed record RegisterResponse(AuthResponse Auth, CustomerResponse Customer);
