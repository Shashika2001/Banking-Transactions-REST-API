using BankingApi.Api.Configuration;
using BankingApi.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BankingApi.Api.Controllers;

/// <summary>
/// Registration and sign-in.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="authService">The authentication service.</param>
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Registers as a new customer and returns an access token.
    /// </summary>
    /// <param name="request">The customer details and a password of 12–128 characters.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The access token and the created customer (national ID masked).</returns>
    [HttpPost("register")]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(CustomersController.GetById),
            "Customers",
            new { customerId = response.Customer.Id },
            response);
    }

    /// <summary>
    /// Signs in and returns an access token.
    /// </summary>
    /// <remarks>Rate limited per client IP address.</remarks>
    /// <param name="request">The email address and password.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The access token.</returns>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitingSetup.LoginPolicy)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        await _authService.LoginAsync(request, cancellationToken);
}
