using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static MCP.Remote.ToolsInformation;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Add console logging for local development
builder.Logging.AddConsole();

// Application Insights is automatically configured for Azure Functions
// Just need the connection string in settings

builder.EnableMcpToolMetadata();

// Demonstrate how you can define tool properties without requiring
// input bindings:
builder
    .ConfigureMcpTool(GetSnippetToolName)
    .WithProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription);

builder.Build().Run();
