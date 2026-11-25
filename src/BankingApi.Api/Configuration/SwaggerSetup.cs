using System.Reflection;
using Microsoft.OpenApi.Models;

namespace BankingApi.Api.Configuration;

/// <summary>
/// Configures Swagger / OpenAPI.
/// </summary>
public static class SwaggerSetup
{
    private const string BearerSchemeId = "Bearer";

    /// <summary>
    /// Adds Swagger with XML documentation and an <b>Authorize</b> button for pasting a bearer token.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddSwaggerWithBearerAuth(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Banking Transactions API", Version = "v1" });

            var xmlDocumentation = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            options.IncludeXmlComments(xmlDocumentation);

            options.AddSecurityDefinition(BearerSchemeId, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Description = "Paste the accessToken returned by POST /api/auth/login (without the 'Bearer ' prefix).",
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = BearerSchemeId },
                }] = [],
            });
        });

        return services;
    }
}
