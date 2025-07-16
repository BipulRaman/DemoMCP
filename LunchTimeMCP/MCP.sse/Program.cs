using MCP.sse.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Add CORS for MCP with SSE support
builder.Services.AddCors(options =>
{
    options.AddPolicy("McpSsePolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Cache-Control", "Connection", "Transfer-Encoding");
    });
});

// Add MCP Services
builder.Services.AddSingleton<RestaurantService>();
builder.Services.AddSingleton<SseStreamingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Only use HTTPS redirection if explicitly running with HTTPS
    var urls = builder.Configuration["ASPNETCORE_URLS"] ?? "";
    if (urls.Contains("https://"))
    {
        app.UseHttpsRedirection();
    }
}
else
{
    app.UseHttpsRedirection();
}

// Enable CORS
app.UseCors("McpSsePolicy");

app.UseAuthorization();
app.MapControllers();

// Add a simple health check endpoint for monitoring
app.MapGet("/health", () => new { 
    status = "healthy", 
    server_type = "sse",
    streaming = "server-sent-events",
    timestamp = DateTime.UtcNow 
});

// Add an endpoint to check MCP server info (for debugging)
app.MapGet("/", () => new { 
    name = "LunchTime MCP SSE Server", 
    version = "1.0.0", 
    description = "Server-Sent Events based Model Context Protocol server for managing lunch restaurant choices",
    server_type = "sse",
    streaming = new {
        supported = true,
        protocol = "server-sent-events",
        features = new[] { "real-time-streaming", "auto-reconnection", "event-based-communication" }
    },
    endpoints = new { 
        mcp_jsonrpc = "/mcp (POST with JSON-RPC 2.0)",
        mcp_initialize = "/mcp/initialize (GET)",
        mcp_tools = "/mcp/tools (GET)",
        sse_stream = "/sse/stream (GET)",
        sse_tools = "/sse/tools/{toolName} (GET)",
        health = "/health"
    },
    documentation = new {
        jsonrpc_usage = "Send POST requests to /mcp with JSON-RPC 2.0 format",
        http_usage = "Use GET endpoints for direct HTTP access",
        sse_usage = "Connect to /sse/stream for real-time Server-Sent Events",
        supported_methods = new[] { "initialize", "tools/list", "tools/call", "prompts/list", "prompts/get" },
        sse_events = new[] { "tool-result", "restaurant-update", "error", "heartbeat" }
    }
});

app.Run();
