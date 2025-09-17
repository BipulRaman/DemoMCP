using MCP.Common.Services;
using MCP.HTTP.EntraAuth.Services;
using System.Text.Json;

namespace MCP.HTTP.EntraAuth.Services;

/// <summary>
/// The McpConnectService class implements the IMcpConnectService interface
/// </summary>
public class McpConnectService : IMcpConnectService
{
    private readonly ISnippetService _snippetService;
    private readonly ILogger<McpConnectService> _logger;
    private readonly IAuthenticationStateService _authService;

    /// <summary>
    /// The constructor for McpConnectService
    /// </summary>
    /// <param name="snippetService"></param>
    /// <param name="logger"></param>
    /// <param name="authService"></param>
    public McpConnectService(ISnippetService snippetService, ILogger<McpConnectService> logger, IAuthenticationStateService authService)
    {
        _snippetService = snippetService;
        _logger = logger;
        _authService = authService;
    }

    /// <inheritdoc/>
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

            _logger.LogInformation("{Class}_{Method} : Handling MCP request: {Method}",
                nameof(McpConnectService), nameof(HandleMcpRequestAsync), method);

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
            _logger.LogError(ex, "{Class}_{Method} : Error handling MCP request: {ErrorMessage}",
                nameof(McpConnectService), nameof(HandleMcpRequestAsync), ex.Message);
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

    /// <summary>
    /// Handles the "initialize" method of the MCP protocol.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="id"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Handles the "initialized" method of the MCP protocol.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private object HandleInitialized(object? id)
    {
        return new
        {
            jsonrpc = "2.0",
            id = id,
            result = new { }
        };
    }

    /// <summary>
    /// Handles the "tools/list" method of the MCP protocol.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private Task<object> HandleToolsList(object? id)
    {
        var tools = new object[]
        {
            // Authentication tools - always available
            new
            {
                name = "start_authentication",
                description = "Start Entra ID device code authentication flow. This will provide you with a verification URL and code to complete authentication in your browser.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        sessionId = new { type = "string", description = "Optional session ID for this authentication session. If not provided, a new one will be generated." }
                    }
                }
            },
            new
            {
                name = "check_authentication_status",
                description = "Check if the device code authentication is complete. Use this after completing authentication in your browser.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        sessionId = new { type = "string", description = "Session ID from the start_authentication response" }
                    },
                    required = new[] { "sessionId" }
                }
            },
            new
            {
                name = "get_authenticated_connection",
                description = "Get the authenticated MCP connection details after successful authentication.",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        sessionId = new { type = "string", description = "Session ID from successful authentication" }
                    },
                    required = new[] { "sessionId" }
                }
            },
            // Protected tools - require authentication
            new
            {
                name = "get_snippet",
                description = "Retrieve a code snippet by name (requires authentication - use session ID from authentication)",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Name of the snippet to retrieve" },
                        sessionId = new { type = "string", description = "Session ID from successful authentication" }
                    },
                    required = new[] { "name", "sessionId" }
                }
            },
            new
            {
                name = "save_snippet",
                description = "Save a new code snippet (requires authentication - use session ID from authentication)",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Name of the snippet" },
                        content = new { type = "string", description = "Content of the snippet" },
                        sessionId = new { type = "string", description = "Session ID from successful authentication" }
                    },
                    required = new[] { "name", "content", "sessionId" }
                }
            },
            new
            {
                name = "list_snippets",
                description = "List all available code snippets (requires authentication - use session ID from authentication)",
                inputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        sessionId = new { type = "string", description = "Session ID from successful authentication" }
                    },
                    required = new[] { "sessionId" }
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

    /// <summary>
    /// Handles the "tools/call" method of the MCP protocol.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task<object> HandleToolsCall(JsonDocument request, object? id)
    {
        var paramsElement = request.RootElement.GetProperty("params");
        var toolName = paramsElement.GetProperty("name").GetString();
        var arguments = paramsElement.TryGetProperty("arguments", out var argsElement) ? argsElement : default;

        try
        {
            object result = toolName switch
            {
                // Authentication tools
                "start_authentication" => await HandleStartAuthentication(arguments),
                "check_authentication_status" => await HandleCheckAuthenticationStatus(arguments),
                "get_authenticated_connection" => await HandleGetAuthenticatedConnection(arguments),
                // Protected tools
                "get_snippet" => await HandleGetSnippet(arguments),
                "save_snippet" => await HandleSaveSnippet(arguments),
                "list_snippets" => await HandleListSnippets(arguments),
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
                            text = result?.ToString() ?? "null"
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

    /// <summary>
    /// Handles the "get_snippet" tool call.
    /// </summary>
    /// <param name="arguments"></param>
    /// <returns></returns>
    private async Task<object> HandleGetSnippet(JsonElement arguments)
    {
        // Check authentication first
        var sessionId = arguments.TryGetProperty("sessionId", out var sessionIdElement) 
            ? sessionIdElement.GetString() 
            : null;

        if (string.IsNullOrEmpty(sessionId) || !await _authService.IsSessionAuthenticatedAsync(sessionId))
        {
            return "🔒 **Authentication Required**\n\n" +
                   "To access snippets, you need to authenticate first:\n\n" +
                   "1. Use `start_authentication` to begin device code flow\n" +
                   "2. Complete authentication in your browser\n" +
                   "3. Use `check_authentication_status` to verify completion\n" +
                   "4. Include the sessionId parameter in your request\n\n" +
                   "Example: Use your authenticated session ID with this tool.";
        }

        var name = arguments.GetProperty("name").GetString();
        var snippet = await _snippetService.GetSnippetAsync(name!);
        return $"📄 **Snippet: {name}**\n\n```\n{snippet}\n```";
    }

    private async Task<object> HandleSaveSnippet(JsonElement arguments)
    {
        // Check authentication first
        var sessionId = arguments.TryGetProperty("sessionId", out var sessionIdElement) 
            ? sessionIdElement.GetString() 
            : null;

        if (string.IsNullOrEmpty(sessionId) || !await _authService.IsSessionAuthenticatedAsync(sessionId))
        {
            return "🔒 **Authentication Required**\n\n" +
                   "To save snippets, you need to authenticate first:\n\n" +
                   "1. Use `start_authentication` to begin device code flow\n" +
                   "2. Complete authentication in your browser\n" +
                   "3. Use `check_authentication_status` to verify completion\n" +
                   "4. Include the sessionId parameter in your request\n\n" +
                   "Example: Use your authenticated session ID with this tool.";
        }

        var name = arguments.GetProperty("name").GetString()!;
        var content = arguments.GetProperty("content").GetString()!;

        await _snippetService.SaveSnippetAsync(name, content);
        return $"✅ **Snippet Saved**\n\nSnippet '{name}' has been saved successfully!\nUse `get_snippet` to retrieve it or `list_snippets` to see all available snippets.";
    }

    private async Task<object> HandleListSnippets(JsonElement arguments)
    {
        // Check authentication first
        var sessionId = arguments.TryGetProperty("sessionId", out var sessionIdElement) 
            ? sessionIdElement.GetString() 
            : null;

        if (string.IsNullOrEmpty(sessionId) || !await _authService.IsSessionAuthenticatedAsync(sessionId))
        {
            return "🔒 **Authentication Required**\n\n" +
                   "To list snippets, you need to authenticate first:\n\n" +
                   "1. Use `start_authentication` to begin device code flow\n" +
                   "2. Complete authentication in your browser\n" +
                   "3. Use `check_authentication_status` to verify completion\n" +
                   "4. Include the sessionId parameter in your request\n\n" +
                   "Example: Use your authenticated session ID with this tool.";
        }

        var snippets = await _snippetService.ListSnippetsAsync();
        if (!snippets.Any())
        {
            return "📂 **Available Snippets**\n\nNo snippets found. Use `save_snippet` to create your first snippet!";
        }
        
        var snippetList = string.Join("\n", snippets.Select((snippet, index) => $"{index + 1}. {snippet}"));
        return $"📂 **Available Snippets** ({snippets.Count()} total)\n\n{snippetList}\n\nUse `get_snippet` with the snippet name to retrieve content.";
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
            catch (Exception ex)
            {
                return CreateErrorResponse(-32603, "Internal error " + ex.Message, id);
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

    // Authentication tool handlers
    private async Task<object> HandleStartAuthentication(JsonElement arguments)
    {
        var sessionId = arguments.TryGetProperty("sessionId", out var sessionIdElement) 
            ? sessionIdElement.GetString() 
            : Guid.NewGuid().ToString();

        var authResult = await _authService.StartDeviceCodeFlowAsync(sessionId!);
        
        return $"🔐 **Authentication Required**\n\n" +
               $"To authenticate with your Azure account:\n\n" +
               $"1. 📋 Copy this code: **{authResult.UserCode}**\n" +
               $"2. 🌐 Open: https://microsoft.com/devicelogin\n" +
               $"3. ✏️ Enter the code when prompted\n" +
               $"4. ✅ Complete the sign-in process\n\n" +
               $"Alternative URL: {authResult.VerificationUri}\n\n" +
               $"Session ID: {sessionId}\n" +
               $"Once completed, use `check_authentication_status` with this session ID to verify authentication.";
    }

    private async Task<object> HandleCheckAuthenticationStatus(JsonElement arguments)
    {
        var sessionId = arguments.GetProperty("sessionId").GetString()!;
        var isComplete = await _authService.PollForCompletionAsync(sessionId);
        
        if (isComplete)
        {
            return "✅ **Authentication Successful!**\n\n" +
                   "You are now authenticated and can access all MCP tools and features.\n" +
                   "Use `get_authenticated_connection` to get the full connection details if needed.";
        }
        else
        {
            return "⏳ **Authentication Pending**\n\n" +
                   "Please complete the authentication in your browser, then check again.\n" +
                   "If you haven't started authentication yet, use `start_authentication` first.";
        }
    }

    private Task<object> HandleGetAuthenticatedConnection(JsonElement arguments)
    {
        var sessionId = arguments.GetProperty("sessionId").GetString()!;
        
        return Task.FromResult<object>("🔗 **Authenticated Connection Details**\n\n" +
                                      $"Session ID: {sessionId}\n" +
                                      "Connection URL: http://localhost:5120/mcp-connect\n" +
                                      "Status: Authenticated\n\n" +
                                      "You are connected with full authentication. All MCP tools and features are now available to you.");
    }
}
