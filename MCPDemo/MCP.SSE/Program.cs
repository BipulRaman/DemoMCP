using MCP.Common;
using MCP.Common.Tools;

var builder = WebApplication.CreateBuilder(args);

// Register blob service with connection string
var connectionString = builder.Configuration.GetConnectionString("BlobStorage") ?? "UseDevelopmentStorage=true";
builder.Services.AddSharedServices(connectionString);

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithPrompts<SnippetPrompts>()
    .WithResources<SnippetResources>()
    .WithTools<SnippetTools>();

builder.Services.AddHttpClient();

var app = builder.Build();

app.MapMcp();

app.Run();
