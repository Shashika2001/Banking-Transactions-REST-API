using System.Security.Claims;
using BankingApi.Application.Abstractions;
using BankingApi.Application.Common.Security;
using BankingApi.Infrastructure.Authentication;

namespace BankingApi.Api.Services;

/// <summary>
/// Reads the current user from the validated access token on the HTTP request.
/// </summary>
public sealed class HttpContextCurrentUser : ICurrentUser
{
    /// <summary>The user identifier recorded for unauthenticated requests.</summary>
    public const string AnonymousUserId = "anonymous";

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpContextCurrentUser"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">Provides the current HTTP context.</param>
    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    /// <inheritdoc />
    public string UserId =>
        IsAuthenticated && Principal!.FindFirstValue(BankingClaimTypes.Subject) is { Length: > 0 } subject
            ? subject
            : AnonymousUserId;

    /// <inheritdoc />
    public bool IsAdmin => IsAuthenticated && Principal!.IsInRole(Roles.Admin);

    /// <inheritdoc />
    public Guid? CustomerId =>
        IsAuthenticated && Guid.TryParse(Principal!.FindFirstValue(BankingClaimTypes.CustomerId), out var customerId)
            ? customerId
            : null;

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;
}
