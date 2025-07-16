using MCP.http.Services;
using MCP.http.Tools;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// Configure MCP Server with tools
builder.Services
    .AddMcpServer()
    .WithTools<RestaurantTools>()
    .WithHttpTransport();

builder.Services.AddSingleton<RestaurantService>();

// Configure HttpClient for external APIs
builder.Services.AddHttpClient("RestaurantApi", client =>
{
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("restaurant-tool", "1.0"));
});

var app = builder.Build();

// Map MCP endpoints
app.MapMcp();

Console.WriteLine($"Starting MCP Restaurant server at {app.Urls}");
Console.WriteLine("Press Ctrl+C to stop the server");

app.Run();
