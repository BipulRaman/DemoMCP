using MCP.Common;
using MCP.Common.Tools;
using MCP.HTTP.OAuth.Configuration;
using MCP.HTTP.OAuth.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure options
builder.Services.Configure<McpServerOptions>(builder.Configuration.GetSection(McpServerOptions.SectionName));

// Register blob service with connection string
var connectionString = builder.Configuration.GetConnectionString("BlobStorage") ?? "UseDevelopmentStorage=true";
builder.Services.AddSharedServices(connectionString);

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

// Do not add any authentication services to avoid triggering OAuth flows

var app = builder.Build();

// Configure middleware pipeline
app.UseMcpPipeline();

// Map endpoints
app.MapMcpEndpoints();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("MCP.HTTP.OAuth started successfully");

app.Run();
