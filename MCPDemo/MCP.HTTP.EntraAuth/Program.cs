using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.AspNetCore;
using MCP.HTTP.EntraAuth.Services;
using MCP.HTTP.EntraAuth.Configuration;
using MCP.HTTP.EntraAuth.MCP;

var builder = WebApplication.CreateBuilder(args);

// Configure options
builder.Services.Configure<McpServerConfig>(builder.Configuration.GetSection(McpServerConfig.SectionName));
builder.Services.Configure<AzureAdConfig>(builder.Configuration.GetSection(AzureAdConfig.SectionName));

// Add blob storage services
var connectionString = builder.Configuration.GetConnectionString("BlobStorage") ?? "UseDevelopmentStorage=true";
builder.Services.AddSingleton<IAzBlobService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<AzBlobService>>();
    return new AzBlobService(connectionString, logger);
});
builder.Services.AddSingleton<ISnippetService, SnippetService>();

// Add MCP services
// Keep the custom service for backward compatibility if needed
builder.Services.AddScoped<IMcpConnectService, McpConnectService>();

// Add native MCP server with authentication-aware components
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithPrompts<SnippetPrompts>()
    .WithResources<SnippetResources>()
    .WithTools<SnippetTools>();

// Add authentication services
builder.Services.AddSingleton<IAuthenticationStateService, AuthenticationStateService>();

// Add common services
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

// Map native MCP endpoint (this is the standard MCP server endpoint)
app.MapMcp("/");

// Map controllers (includes health, capabilities, and custom mcp-connect endpoints for backward compatibility)
app.MapControllers();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("MCP.HTTP.EntraAuth server with device code authentication started successfully");

app.Run();