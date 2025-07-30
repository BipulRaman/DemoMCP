using MCP.Common;
using MCP.Common.Tools;
using MCP.HTTP.EntraAuth.Extensions;
using MCP.HTTP.EntraAuth.Middleware;
using MCP.HTTP.EntraAuth.Services;
using Azure.Monitor.OpenTelemetry.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry and configure it to use Azure Monitor.
builder.Services.AddOpenTelemetry().UseAzureMonitor();

// Configure and validate options
builder.Services.AddConfigs(builder.Configuration);

// Register blob service with connection string
var connectionString = builder.Configuration.GetConnectionString("BlobStorage") ?? "UseDevelopmentStorage=true";
builder.Services.AddSharedServices(connectionString);

// Add MCP services
builder.Services.AddScoped<IMcpConnectService, McpConnectService>();

// Add common services
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddCors();

// Add controllers
builder.Services.AddControllers();

// Configure MCP server endpoints
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithPrompts<SnippetPrompts>()
    .WithResources<SnippetResources>()
    .WithTools<SnippetTools>();

// Add authentication
builder.Services.AddMcpAuthentication(builder.Configuration);

var app = builder.Build();

// Configure middleware pipeline
// Add CORS support for VS Code MCP extension
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// Enable authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Use the custom MCP authentication middleware
app.UseMcpAuthentication();

// Map MCP JSON-RPC SSE endpoint at root (authentication handled by middleware)
app.MapMcp("/");

// Map controllers (includes all auth endpoints and mcp-connect)
app.MapControllers();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("{Class}_{Method} : MCP.HTTP.EntraAuth server started successfully",
    nameof(Program), "Main");

app.Run();
