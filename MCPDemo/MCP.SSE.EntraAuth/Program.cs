using MCP.Common;
using MCP.Common.Tools;
using MCP.SSE.EntraAuth.Configuration;
using MCP.SSE.EntraAuth.Extensions;
using MCP.SSE.EntraAuth.Middleware;
using MCP.SSE.EntraAuth.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure options
builder.Services.Configure<AzureAdConfig>(builder.Configuration.GetSection(AzureAdConfig.SectionName));
builder.Services.Configure<AuthenticationConfig>(builder.Configuration.GetSection(AuthenticationConfig.SectionName));
builder.Services.Configure<McpServerConfig>(builder.Configuration.GetSection(McpServerConfig.SectionName));

// Register blob service with connection string
var connectionString = builder.Configuration.GetConnectionString("BlobStorage") ?? "UseDevelopmentStorage=true";
builder.Services.AddSharedServices(connectionString);

// Add MCP services
builder.Services.AddScoped<CustomMcpService>();

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
logger.LogInformation("MCP.SSE server started successfully");

app.Run();
