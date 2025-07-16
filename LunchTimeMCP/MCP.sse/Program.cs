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

// Add CORS for MCP with streaming support
builder.Services.AddCors(options =>
{
    options.AddPolicy("McpStreamingPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Cache-Control", "Connection", "Transfer-Encoding");
    });
});

// Add MCP Services
builder.Services.AddSingleton<RestaurantService>();
builder.Services.AddSingleton<StreamingService>();

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
app.UseCors("McpStreamingPolicy");

app.UseAuthorization();
app.MapControllers();

// Add a simple health check endpoint for monitoring
app.MapGet("/health", () => new { 
    status = "healthy", 
    server_type = "mcp-streaming",
    protocol = "mcp-jsonrpc-2.0",
    streaming = "chunked-json",
    timestamp = DateTime.UtcNow 
});

// Add an endpoint to check MCP server info (for debugging)
app.MapGet("/", () => new { 
    name = "LunchTime MCP Streaming Server", 
    version = "1.0.0", 
    description = "HTTP-based Model Context Protocol server with chunked streaming for managing lunch restaurant choices",
    protocol = "JSON-RPC 2.0",
    streaming = new {
        supported = true,
        protocol = "chunked-json",
        features = new[] { "chunked-transfer-encoding", "progressive-loading", "real-time-streaming" }
    },
    endpoints = new { 
        mcp_jsonrpc = "/mcp (POST with JSON-RPC 2.0) - Main MCP endpoint with streaming support",
        mcp_initialize = "/mcp/initialize (GET) - Server initialization info",
        mcp_tools = "/mcp/tools (GET) - List available tools", 
        mcp_capabilities = "/mcp/capabilities (GET) - Streaming capabilities",
        health = "/health - Health check"
    },
    documentation = new {
        jsonrpc_usage = "Send POST requests to /mcp with JSON-RPC 2.0 format for all operations",
        streaming_usage = "Include 'streaming: true' in tool call params or use streaming tools directly",
        supported_methods = new[] { "initialize", "tools/list", "tools/call", "prompts/list", "prompts/get", "resources/list" },
        streaming_tools = new[] { "get_restaurants_stream", "analyze_restaurants_stream", "search_restaurants_stream" },
        streaming_activation = new[] { "Set 'streaming: true' in params", "Use streaming tool names", "Add 'Accept-Streaming' header", "Add 'X-MCP-Streaming' header" }
    }
});

app.Run();
