using MCP.Common;
using MCP.Common.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Get connection string
var connectionString = builder.Configuration.GetConnectionString("BlobStorage") ?? string.Empty;

// Add shared services using the extension method
builder.Services.AddSharedServices(connectionString);

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithPrompts<SnippetPrompts>()
    .WithResources<SnippetResources>()
    .WithTools<SnippetTools>();


builder.Services.AddHttpClient();

await builder.Build().RunAsync();

[McpServerToolType]
public class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"Hello from C#: {message}";

    [McpServerTool, Description("Echoes in reverse the message sent by the client.")]
    public static string ReverseEcho(string message) => new string(message.Reverse().ToArray());
}