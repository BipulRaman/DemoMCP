using MCP.http.Services;
using MCP.http.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services
    .AddMcpServer()
    .WithTools<RestaurantTools>()
    .WithHttpTransport();

builder.Services.AddSingleton<RestaurantService>();

builder.Services.AddHttpClient("RestaurantApi");

var app = builder.Build();

// Map MCP endpoints
app.MapMcp();

app.Run();
