using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore;
using MCP.HTTP.EntraAuth.Services;
using MCP.HTTP.EntraAuth.Configuration;
using MCP.HTTP.EntraAuth.MCP;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure options
builder.Services.Configure<McpServerConfig>(builder.Configuration.GetSection(McpServerConfig.SectionName));
builder.Services.Configure<AzureAdConfig>(builder.Configuration.GetSection(AzureAdConfig.SectionName));

// Add blob storage services
var connectionString = builder.Configuration.GetConnectionString("BlobStorage") ?? "UseDevelopmentStorage=true";
builder.Services.AddSingleton<IAzBlobService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<AzBlobService>>();
    return new AzBlobService(connectionString, logger);
});
builder.Services.AddSingleton<ISnippetService, SnippetService>();

// Add MCP services
// Keep the custom service for backward compatibility if needed
builder.Services.AddScoped<IMcpConnectService, McpConnectService>();

// Add native MCP server with authentication-aware components
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithPrompts<McpPrompts>()
    .WithResources<McpResources>()
    .WithTools<McpTools>();

// Add authentication services
builder.Services.AddSingleton<IAuthenticationStateService, AuthenticationStateService>();

// Add Entra ID JWT Bearer Authentication (native Azure AD tokens)
var azureAdConfig = builder.Configuration.GetSection("AzureAd");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"{azureAdConfig["Instance"]}{azureAdConfig["TenantId"]}/v2.0";
        options.Audience = azureAdConfig["ClientId"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Add common services
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

// Add Authentication and Authorization middleware (native .NET approach)
app.UseAuthentication();
app.UseAuthorization();

// Map native MCP endpoint (this is the standard MCP server endpoint)
app.MapMcp("/");

// Map controllers (includes health, capabilities, and custom mcp-connect endpoints for backward compatibility)
app.MapControllers();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("MCP.HTTP.EntraAuth server with device code authentication started successfully");

app.Run();