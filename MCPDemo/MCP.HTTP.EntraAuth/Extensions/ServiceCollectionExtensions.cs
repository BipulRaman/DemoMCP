using MCP.HTTP.EntraAuth.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace MCP.HTTP.EntraAuth.Extensions;

/// <summary>
/// The ServiceCollectionExtensions class provides extension methods for configuring
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds authentication services to the specified IServiceCollection using Azure AD and JWT Bearer tokens.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
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
                        logger.LogError("{Class}_{Method} : JWT Authentication failed: {Exception}",
                            nameof(ServiceCollectionExtensions), nameof(AddMcpAuthentication), context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogInformation("{Class}_{Method} : JWT Token validated successfully for user: {User}",
                            nameof(ServiceCollectionExtensions), nameof(AddMcpAuthentication), context.Principal?.Identity?.Name ?? "unknown");
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

    /// <summary>
    /// Adds configuration services to the specified IServiceCollection.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IServiceCollection AddConfigs(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.Configure<AzureAdConfig>(configuration.GetSection(AzureAdConfig.SectionName));
        services.Configure<AuthenticationConfig>(configuration.GetSection(AuthenticationConfig.SectionName));
        services.Configure<McpServerConfig>(configuration.GetSection(McpServerConfig.SectionName));

        // Validate configurations at startup
        var azureAdConfig = configuration.GetSection(AzureAdConfig.SectionName).Get<AzureAdConfig>();
        var authConfig = configuration.GetSection(AuthenticationConfig.SectionName).Get<AuthenticationConfig>();
        var mcpServerConfig = configuration.GetSection(McpServerConfig.SectionName).Get<McpServerConfig>();

        if (azureAdConfig != null && !azureAdConfig.IsValid())
        {
            throw new InvalidOperationException("Azure AD configuration is invalid");
        }

        if (authConfig != null && !authConfig.IsValid())
        {
            throw new InvalidOperationException("Authentication configuration is invalid");
        }

        if (mcpServerConfig != null && !mcpServerConfig.IsValid())
        {
            throw new InvalidOperationException("MCP Server configuration is invalid");
        }

        return services;
    }
}