using MCP.Common.Services;
using System.Text.Json;

namespace MCP.HTTP.EntraAuth.Services;

public class CustomMcpService
{
    private readonly ISnippetService _snippetService;
    private readonly ILogger<CustomMcpService> _logger;

    public CustomMcpService(ISnippetService snippetService, ILogger<CustomMcpService> logger)
    {
        _snippetService = snippetService;
        _logger = logger;
    }

    public async Task<object> HandleMcpRequestAsync(JsonDocument request, HttpContext context)
    {
        try
        {
            var method = request.RootElement.GetProperty("method").GetString();
            var idElement = request.RootElement.TryGetProperty("id", out var tempId) ? tempId : JsonDocument.Parse("null").RootElement;

            // Convert JsonElement to actual value to avoid disposal issues
            object? id = idElement.ValueKind switch
            {
                JsonValueKind.String => idElement.GetString(),
                JsonValueKind.Number => idElement.TryGetInt64(out var longVal) ? longVal : idElement.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => null
            };

            _logger.LogInformation("Handling MCP request: {Method}", method);

            return method switch
            {
                "initialize" => HandleInitialize(request, id),
                "initialized" => HandleInitialized(id),
                "tools/list" => await HandleToolsList(id),
                "tools/call" => await HandleToolsCall(request, id),
                "resources/list" => await HandleResourcesList(id),
                "resources/read" => await HandleResourcesRead(request, id),
                "prompts/list" => await HandlePromptsList(id),
                "prompts/get" => await HandlePromptsGet(request, id),
                _ => CreateErrorResponse(-32601, "Method not found", id)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP request");
            var idElement = request.RootElement.TryGetProperty("id", out var tempId) ? tempId : JsonDocument.Parse("null").RootElement;

            // Convert JsonElement to actual value to avoid disposal issues
            object? id = idElement.ValueKind switch
            {
                JsonValueKind.String => idElement.GetString(),
                JsonValueKind.Number => idElement.TryGetInt64(out var longVal) ? longVal : idElement.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => null
            };

            return CreateErrorResponse(-32603, "Internal error", id);
        }
    }

    private object HandleInitialize(JsonDocument request, object? id)
    {
        return new
        {
            jsonrpc = "2.0",
            id = id,
            result = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new
                {
                    tools = new { },
                    resources = new { },
                    prompts = new { }
                },
                serverInfo = new
                {
                    name = "MCP.SSE",
                    version = "1.0.0"
                }
            }
        };
    }

    private object HandleInitialized(object? id)
    {
        return new
        {
            jsonrpc = "2.0",
            id = id,
            result = new { }
        };
    }

    private Task<object> HandleToolsList(object? id)
    {
        var tools = new object[]
        {
            new
            {
                name = "get_snippet",
                description = "Retrieve a code snippet by name",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Name of the snippet to retrieve" }
                    },
                    required = new[] { "name" }
                }
            },
            new
            {
                name = "save_snippet",
                description = "Save a new code snippet",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Name of the snippet" },
                        content = new { type = "string", description = "Content of the snippet" }
                    },
                    required = new[] { "name", "content" }
                }
            },
            new
            {
                name = "list_snippets",
                description = "List all available code snippets",
                inputSchema = new
                {
                    type = "object",
                    properties = new { },
                    required = new string[] { }
                }
            }
        };

        return Task.FromResult<object>(new
        {
            jsonrpc = "2.0",
            id = id,
            result = new { tools = tools }
        });
    }

    private async Task<object> HandleToolsCall(JsonDocument request, object? id)
    {
        var paramsElement = request.RootElement.GetProperty("params");
        var toolName = paramsElement.GetProperty("name").GetString();
        var arguments = paramsElement.TryGetProperty("arguments", out var argsElement) ? argsElement : default;

        try
        {
            object result = toolName switch
            {
                "get_snippet" => await HandleGetSnippet(arguments),
                "save_snippet" => await HandleSaveSnippet(arguments),
                "list_snippets" => await HandleListSnippets(),
                _ => throw new InvalidOperationException($"Unknown tool: {toolName}")
            }; return new
            {
                jsonrpc = "2.0",
                id = id,
                result = new
                {
                    content = new[]
                        {
                        new
                        {
                            type = "text",
                            text = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })
                        }
                    }
                }
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(-32603, $"Tool execution failed: {ex.Message}", id);
        }
    }

    private async Task<object> HandleGetSnippet(JsonElement arguments)
    {
        var name = arguments.GetProperty("name").GetString();
        var snippet = await _snippetService.GetSnippetAsync(name!);
        return new { name = name, content = snippet };
    }

    private async Task<object> HandleSaveSnippet(JsonElement arguments)
    {
        var name = arguments.GetProperty("name").GetString()!;
        var content = arguments.GetProperty("content").GetString()!;

        await _snippetService.SaveSnippetAsync(name, content);
        return new { message = $"Snippet '{name}' saved successfully" };
    }

    private async Task<object> HandleListSnippets()
    {
        var snippets = await _snippetService.ListSnippetsAsync();
        return new { snippets = snippets.ToArray() };
    }

    private async Task<object> HandleResourcesList(object? id)
    {
        var snippetNames = await _snippetService.ListSnippetsAsync();
        var resources = snippetNames.Select(name => new
        {
            uri = $"snippet://{name}",
            name = name,
            description = $"Code snippet: {name}",
            mimeType = "text/plain"
        }).ToArray();

        return new
        {
            jsonrpc = "2.0",
            id = id,
            result = new { resources = resources }
        };
    }

    private async Task<object> HandleResourcesRead(JsonDocument request, object? id)
    {
        var paramsElement = request.RootElement.GetProperty("params");
        var uri = paramsElement.GetProperty("uri").GetString();

        if (!uri!.StartsWith("snippet://"))
        {
            return CreateErrorResponse(-32602, "Invalid resource URI", id);
        }

        var snippetName = uri.Substring("snippet://".Length);

        try
        {
            var content = await _snippetService.GetSnippetAsync(snippetName);

            return new
            {
                jsonrpc = "2.0",
                id = id,
                result = new
                {
                    contents = new[]
                    {
                        new
                        {
                            uri = uri,
                            mimeType = "text/plain",
                            text = content
                        }
                    }
                }
            };
        }
        catch (KeyNotFoundException)
        {
            return CreateErrorResponse(-32602, "Resource not found", id);
        }
    }

    private Task<object> HandlePromptsList(object? id)
    {
        var prompts = new[]
        {
            new
            {
                name = "code_review",
                description = "Generate a code review for the provided snippet",
                arguments = new[]
                {
                    new
                    {
                        name = "snippet_name",
                        description = "Name of the snippet to review",
                        required = true
                    }
                }
            }
        };

        return Task.FromResult<object>(new
        {
            jsonrpc = "2.0",
            id = id,
            result = new { prompts = prompts }
        });
    }

    private async Task<object> HandlePromptsGet(JsonDocument request, object? id)
    {
        var paramsElement = request.RootElement.GetProperty("params");
        var promptName = paramsElement.GetProperty("name").GetString();
        var arguments = paramsElement.TryGetProperty("arguments", out var argsElement) ? argsElement : default;

        if (promptName == "code_review")
        {
            var snippetName = arguments.GetProperty("snippet_name").GetString()!;

            try
            {
                var snippetDetails = await _snippetService.GetSnippetDetailsAsync(snippetName);

                if (snippetDetails == null)
                {
                    return CreateErrorResponse(-32602, "Snippet not found", id);
                }

                var prompt = $@"Please review the following code snippet:

**Snippet Name:** {snippetDetails.Name}
**Language:** {snippetDetails.Language ?? "Unknown"}

```{snippetDetails.Language ?? "text"}
{snippetDetails.Content}
```

Please provide feedback on:
1. Code quality and best practices
2. Potential bugs or issues
3. Performance considerations
4. Suggestions for improvement";

                return new
                {
                    jsonrpc = "2.0",
                    id = id,
                    result = new
                    {
                        description = $"Code review for {snippetDetails.Name}",
                        messages = new[]
                        {
                            new
                            {
                                role = "user",
                                content = new
                                {
                                    type = "text",
                                    text = prompt
                                }
                            }
                        }
                    }
                };
            }
            catch (KeyNotFoundException)
            {
                return CreateErrorResponse(-32602, "Snippet not found", id);
            }
        }

        return CreateErrorResponse(-32602, "Unknown prompt", id);
    }

    private object CreateErrorResponse(int code, string message, object? id)
    {
        return new
        {
            jsonrpc = "2.0",
            id = id,
            error = new
            {
                code = code,
                message = message
            }
        };
    }
}
