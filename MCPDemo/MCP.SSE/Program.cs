using MCP.Common;
using MCP.SSE.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure options and services
builder.Services
    .AddMcpConfiguration(builder.Configuration)
    .AddMcpServices(builder.Configuration)
    .AddMcpAuthentication(builder.Configuration);

var app = builder.Build();

// Configure middleware pipeline
app.UseMcpPipeline();

// Map endpoints
app.MapMcpEndpoints();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("MCP.SSE server started successfully");

app.Run();
