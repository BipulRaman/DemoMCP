using Azure.Storage.Blobs;
using MCP.Remote.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static MCP.Remote.Tools.ToolsInformation;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.EnableMcpToolMetadata();

// Register Azure Blob Service
builder.Services.AddSingleton<BlobServiceClient>(provider =>
{
    var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ??
                          throw new InvalidOperationException("AzureWebJobsStorage connection string is not configured");
    return new BlobServiceClient(connectionString);
});

builder.Services.AddScoped<IAzBlobService, AzBlobService>();
builder.Services.AddScoped<ISnippetService, SnippetService>();

// Demonstrate how you can define tool properties without requiring
// input bindings:
builder
    .ConfigureMcpTool(GetSnippetToolName)
    .WithProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription);

builder
    .ConfigureMcpTool(ListSnippetsToolName);

builder
    .ConfigureMcpTool(DeleteSnippetToolName)
    .WithProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription);

builder.Build().Run();
