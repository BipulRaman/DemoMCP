using MCP.http.Services;
using MCP.http.Tools;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Configure for Azure deployment
builder.WebHost.ConfigureKestrel(options =>
{
    var port = Environment.GetEnvironmentVariable("PORT");
    if (!string.IsNullOrEmpty(port))
    {
        options.ListenAnyIP(int.Parse(port));
    }
});

builder.Services.AddHttpContextAccessor();

// Configure MCP Server with tools
builder.Services
    .AddMcpServer(options =>
    {
        // Configure for Azure deployment
        options.ServerInfo = new()
        {
            Name = "restaurant-server",
            Version = "1.0.0"
        };
    })
    .WithTools<RestaurantTools>()
    .WithHttpTransport();

builder.Services.AddSingleton<RestaurantService>();

// Add health checks for Azure
builder.Services.AddHealthChecks();

// Add CORS for web clients
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Configure HttpClient for external APIs
builder.Services.AddHttpClient("RestaurantApi", client =>
{
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("restaurant-tool", "1.0"));
});

var app = builder.Build();

// Enable CORS
app.UseCors();

// Handle MCP header requirements automatically
app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
    
    // For MCP requests, automatically fix missing Accept headers
    if (context.Request.Path == "/" && context.Request.Method == "POST")
    {
        var acceptHeader = context.Request.Headers.Accept.ToString();
        
        // If Accept header is missing or doesn't include required types, fix it
        if (string.IsNullOrEmpty(acceptHeader) || 
            (!acceptHeader.Contains("application/json") || !acceptHeader.Contains("text/event-stream")))
        {
            // Set the correct Accept header
            context.Request.Headers["Accept"] = "application/json, text/event-stream";
            Console.WriteLine("Auto-corrected Accept header for MCP compatibility");
        }
    }
    
    await next();
    Console.WriteLine($"Response: {context.Response.StatusCode}");
});

// Map MCP endpoints
app.MapMcp();

// Add health check endpoint for Azure
app.MapHealthChecks("/health");

app.Run();
