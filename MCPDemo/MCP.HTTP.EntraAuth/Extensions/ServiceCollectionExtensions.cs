using MCP.HTTP.EntraAuth.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace MCP.HTTP.EntraAuth.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMcpAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var azureAdOptions = configuration.GetSection(AzureAdConfig.SectionName).Get<AzureAdConfig>()!;
        var authOptions = configuration.GetSection(AuthenticationConfig.SectionName).Get<AuthenticationConfig>()!;

        // Validate configuration
        if (!azureAdOptions.IsValid())
            throw new InvalidOperationException("Azure AD configuration is invalid");

        if (!authOptions.IsValid())
            throw new InvalidOperationException("Authentication configuration is invalid");

        var audience = $"api://{azureAdOptions.ClientId}";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"{azureAdOptions.Instance}{azureAdOptions.TenantId}/v2.0";
                options.Audience = audience;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = !Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? true,
                    RequireSignedTokens = !Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? true,
                    ClockSkew = TimeSpan.FromMinutes(5),
                    ValidAudiences = new[] { audience, azureAdOptions.ClientId },
                    ValidIssuers = new[]
                    {
                        $"{azureAdOptions.Instance}{azureAdOptions.TenantId}/v2.0",
                        $"https://sts.windows.net/{azureAdOptions.TenantId}/"
                    }
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogError("JWT Authentication failed: {Exception}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogInformation("JWT Token validated successfully for user: {User}",
                            context.Principal?.Identity?.Name ?? "unknown");
                        return Task.CompletedTask;
                    },
                    OnMessageReceived = context =>
                    {
                        // For SSE connections, also check for token in query string
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("McpAccess", policy => policy.RequireRole(authOptions.RequiredRoles));
        });

        return services;
    }
}