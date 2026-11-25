using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace BankingApi.Api.Configuration;

/// <summary>
/// Settings for the login rate limit, bound from <c>RateLimiting:Login</c>.
/// </summary>
public sealed class LoginRateLimitOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "RateLimiting:Login";

    /// <summary>Gets or sets how many login attempts each client IP may make per window.</summary>
    public int PermitLimit { get; set; } = 5;

    /// <summary>Gets or sets the window length in seconds.</summary>
    public int WindowSeconds { get; set; } = 60;
}

/// <summary>
/// Configures rate limiting.
/// </summary>
public static class RateLimitingSetup
{
    /// <summary>The policy applied to the login endpoint.</summary>
    public const string LoginPolicy = "login";

    /// <summary>
    /// Adds a fixed-window limit per client IP for <see cref="LoginPolicy"/>. Rejected requests get 429 with
    /// a problem details body and a <c>Retry-After</c> header.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddLoginRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LoginRateLimitOptions>()
            .Bind(configuration.GetSection(LoginRateLimitOptions.SectionName))
            .Validate(
                options => options.PermitLimit > 0 && options.WindowSeconds > 0,
                "RateLimiting:Login:PermitLimit and WindowSeconds must be greater than zero.")
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;
            options.AddPolicy(LoginPolicy, httpContext =>
            {
                var limits = httpContext.RequestServices.GetRequiredService<IOptions<LoginRateLimitOptions>>().Value;

                // Note: behind a reverse proxy, configure forwarded headers so this is the real client IP.
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.PermitLimit,
                    Window = TimeSpan.FromSeconds(limits.WindowSeconds),
                    QueueLimit = 0,
                });
            });
        });

        return services;
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(
                System.Globalization.CultureInfo.InvariantCulture);
        }

        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests",
                Detail = "Too many attempts. Wait before trying again.",
            },
        });
    }
}
