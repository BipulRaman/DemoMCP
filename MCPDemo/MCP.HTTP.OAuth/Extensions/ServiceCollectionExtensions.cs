using MCP.HTTP.OAuth.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace MCP.HTTP.OAuth.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMcpAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var azureAdOptions = configuration.GetSection(AzureAdOptions.SectionName).Get<AzureAdOptions>()!;
        var authOptions = configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>()!;

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
                    },
                    RoleClaimType = "roles",
                    NameClaimType = "name"
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogError("JWT Authentication failed: {Exception} | Token: {Token}",
                            context.Exception.Message,
                            context.Request.Headers.Authorization.FirstOrDefault()?.Substring(0, Math.Min(50, context.Request.Headers.Authorization.FirstOrDefault()?.Length ?? 0)) + "...");
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
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                        logger.LogDebug("JWT Message received with auth header: {AuthHeader}",
                            authHeader?.Substring(0, Math.Min(50, authHeader.Length)) + "...");
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("McpAccess", policy =>
            {
                policy.RequireAuthenticatedUser();
                if (authOptions.RequiredRoles?.Any() == true)
                {
                    policy.RequireRole(authOptions.RequiredRoles);
                }
            });
        });

        return services;
    }
}