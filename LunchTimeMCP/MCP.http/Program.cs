using MCP.http.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add CORS for MCP
builder.Services.AddCors(options =>
{
    options.AddPolicy("McpPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Restaurant Service
builder.Services.AddSingleton<RestaurantService>();

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
app.UseCors("McpPolicy");

app.UseAuthorization();
app.MapControllers();

// Add a simple health check endpoint for monitoring
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

// Add an endpoint to check MCP server info (for debugging)
app.MapGet("/", () => new { 
    name = "LunchTime MCP Server", 
    version = "1.0.0", 
    description = "HTTP-based Model Context Protocol server for managing lunch restaurant choices",
    endpoints = new { 
        mcp = "/mcp",
        health = "/health"
    }
});

app.Run();
