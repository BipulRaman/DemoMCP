using MCP.Common;
using MCP.HTTP.EntraAuth.Services;
using MCP.HTTP.EntraAuth.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure options
builder.Services.Configure<McpServerConfig>(builder.Configuration.GetSection(McpServerConfig.SectionName));
builder.Services.Configure<AzureAdConfig>(builder.Configuration.GetSection(AzureAdConfig.SectionName));

// Register blob service with connection string
var connectionString = builder.Configuration.GetConnectionString("BlobStorage") ?? "UseDevelopmentStorage=true";
builder.Services.AddSharedServices(connectionString);

// Add MCP services
builder.Services.AddScoped<IMcpConnectService, McpConnectService>();

// Add authentication services
builder.Services.AddSingleton<IAuthenticationStateService, AuthenticationStateService>();

// Add common services
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

// Map controllers (includes health, capabilities, and mcp-connect endpoints)
app.MapControllers();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("MCP.HTTP.EntraAuth server with device code authentication started successfully");

app.Run();