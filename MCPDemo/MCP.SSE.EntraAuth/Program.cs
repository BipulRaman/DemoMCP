using MCP.Common;
using MCP.Common.Tools;
using MCP.SSE.EntraAuth.Configuration;
using MCP.SSE.EntraAuth.Extensions;
using MCP.SSE.EntraAuth.Services;
using MCP.SSE.Middleware;
using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure options
builder.Services.Configure<AzureAdOptions>(builder.Configuration.GetSection(AzureAdOptions.SectionName));
builder.Services.Configure<AuthenticationOptions>(builder.Configuration.GetSection(AuthenticationOptions.SectionName));
builder.Services.Configure<McpServerOptions>(builder.Configuration.GetSection(McpServerOptions.SectionName));

// Register blob service with connection string
var connectionString = builder.Configuration.GetConnectionString("BlobStorage") ?? "UseDevelopmentStorage=true";
builder.Services.AddSharedServices(connectionString);

// Add MCP services
builder.Services.AddScoped<CustomMcpService>();

// Add common services
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddCors();

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
app.UseMcpPipeline();

// Map endpoints
app.MapMcpEndpoints();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("MCP.SSE server started successfully");

app.Run();
