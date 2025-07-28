using MCP.Common;
using MCP.Common.Tools;
using MCP.SSE.Configuration;
using MCP.SSE.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore;

namespace MCP.SSE.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMcpConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure options
        services.Configure<AzureAdOptions>(configuration.GetSection(AzureAdOptions.SectionName));
        services.Configure<AuthenticationOptions>(configuration.GetSection(AuthenticationOptions.SectionName));
        services.Configure<McpServerOptions>(configuration.GetSection(McpServerOptions.SectionName));
        
        return services;
    }
    
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
                    }
                };
                
                ConfigureJwtBearerEvents(options);
            });
            
        services.AddAuthorization(options =>
        {
            options.AddPolicy("McpAccess", policy => policy.RequireRole(authOptions.RequiredRoles));
        });
        
        return services;
    }
    
    public static IServiceCollection AddMcpServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register blob service with connection string
        var connectionString = configuration.GetConnectionString("BlobStorage") ?? "UseDevelopmentStorage=true";
        services.AddSharedServices(connectionString);
        
        // Add common services
        services.AddHttpContextAccessor();
        services.AddHttpClient();
        services.AddCors();
        
        // Configure MCP server endpoints
        services.AddMcpServer()
            .WithHttpTransport()
            .WithPrompts<SnippetPrompts>()
            .WithResources<SnippetResources>()
            .WithTools<SnippetTools>();
            
        return services;
    }
    
    private static void ConfigureJwtBearerEvents(JwtBearerOptions options)
    {
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
            }
        };
    }
}