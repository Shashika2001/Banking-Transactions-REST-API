using BankingApi.Application.Abstractions;

namespace BankingApi.Api.Services;

/// <summary>
/// Reads the current user from the HTTP context. Until authentication is added, requests are attributed to
/// <see cref="AnonymousUserId"/>.
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
    public string UserId =>
        _httpContextAccessor.HttpContext?.User.Identity is { IsAuthenticated: true, Name: { Length: > 0 } name }
            ? name
            : AnonymousUserId;
}
