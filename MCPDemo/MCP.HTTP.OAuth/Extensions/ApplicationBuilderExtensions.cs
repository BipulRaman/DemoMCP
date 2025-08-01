using MCP.HTTP.OAuth.Configuration;
using MCP.HTTP.OAuth.Middleware;
using Microsoft.Extensions.Options;

namespace MCP.HTTP.OAuth.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseMcpPipeline(this IApplicationBuilder app)
    {
        // Add CORS support for VS Code MCP extension
        app.UseCors(policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());

        // Use the custom MCP authentication middleware (no built-in auth middleware)
        app.UseMcpAuthentication();

        return app;
    }

    public static IApplicationBuilder MapMcpEndpoints(this IApplicationBuilder app)
    {
        var appBuilder = (WebApplication)app;

        // Add health check endpoint for MCP clients
        appBuilder.MapGet("/health", () => new { status = "ok", timestamp = DateTime.UtcNow });

        // Add server capabilities endpoint (public, no auth required)
        appBuilder.MapGet("/capabilities", (IOptions<McpServerOptions> mcpOptions) =>
        {
            return new
            {
                name = mcpOptions.Value.Name,
                version = mcpOptions.Value.Version,
                // Completely remove authentication section to avoid triggering auth flows
                transport = mcpOptions.Value.Transport
            };
        });

        // Map MCP JSON-RPC SSE endpoint at root (authentication handled by middleware)
        appBuilder.MapMcp("/");

        return app;
    }
}
