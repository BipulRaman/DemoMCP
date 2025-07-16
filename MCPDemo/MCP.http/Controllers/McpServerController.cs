using MCP.http.Models;
using MCP.http.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace MCP.http.Controllers;

/// <summary>
/// MCP (Model Context Protocol) server implementation with streaming support using JSON-RPC 2.0
/// </summary>
[ApiController]
[Route("mcp")]
[Produces("application/json")]
public class McpServerController : ControllerBase
{
    private readonly RestaurantService _restaurantService;
    private readonly StreamingService _streamingService;
    private readonly ILogger<McpServerController> _logger;

    public McpServerController(
        RestaurantService restaurantService,
        StreamingService streamingService,
        ILogger<McpServerController> logger)
    {
        _restaurantService = restaurantService;
        _streamingService = streamingService;
        _logger = logger;
    }

    /// <summary>
    /// Main MCP protocol endpoint supporting both regular and streaming requests
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> HandleMcpRequest([FromBody] JsonRpcRequest request)
    {
        try
        {
            _logger.LogInformation("Received MCP request: {Method} with ID: {Id}", request.Method, request.Id);

            // Handle notifications (requests without ID)
            if (request.Id == null && request.Method == "notifications/initialized")
            {
                _logger.LogInformation("Client initialization complete");
                return Ok(); // No response for notifications
            }

            var response = request.Method switch
            {
                "initialize" => HandleInitialize(request),
                "tools/list" => HandleToolsList(request),
                "tools/call" => await HandleToolCallWithStreaming(request),
                "prompts/list" => HandlePromptsList(request),
                "prompts/get" => HandlePromptsGet(request),
                "resources/list" => HandleResourcesList(request),
                _ => CreateErrorResponse(request.Id, -32601, $"Method not found: {request.Method}")
            };

            // For streaming responses, return directly (they handle their own response)
            if (response is IActionResult actionResult)
                return actionResult;

            _logger.LogInformation("Sending response for method: {Method}", request.Method);
            return Ok(response);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error in MCP request");
            return Ok(CreateErrorResponse(request?.Id, -32700, "Parse error", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP request");
            return Ok(CreateErrorResponse(request?.Id, -32603, "Internal error", ex.Message));
        }
    }

    /// <summary>
    /// HTTP GET endpoint for MCP initialization (for direct HTTP access)
    /// </summary>
    [HttpGet("initialize")]
    public IActionResult GetInitialize()
    {
        try
        {
            _logger.LogInformation("Received HTTP GET request for MCP initialization");
            
            var result = new McpInitializeResult
            {
                ProtocolVersion = "2024-11-05",
                Capabilities = new McpServerCapabilities
                {
                    Tools = new McpToolsCapability { ListChanged = true, Streaming = true },
                    Resources = new McpResourcesCapability { Subscribe = true, ListChanged = true },
                    Prompts = new McpPromptsCapability { ListChanged = true },
                    Streaming = new McpStreamingServerCapability
                    {
                        Supported = true,
                        Protocols = new[] { "chunked-json" },
                        ToolStreaming = true,
                        PromptStreaming = true
                    }
                },
                ServerInfo = new McpServerInfo
                {
                    Name = "LunchTime MCP Streaming Server",
                    Version = "1.0.0",
                    Description = "HTTP-based MCP server with chunked streaming for lunch restaurant management"
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP initialization");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// HTTP GET endpoint for listing available tools
    /// </summary>
    [HttpGet("tools")]
    public IActionResult GetTools()
    {
        try
        {
            var tools = GetAvailableTools();
            return Ok(new McpToolsListResult { Tools = tools });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing tools");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// HTTP GET endpoint for streaming capabilities
    /// </summary>
    [HttpGet("capabilities")]
    public IActionResult GetCapabilities()
    {
        return Ok(new
        {
            name = "LunchTime MCP Streaming Server",
            version = "1.0.0",
            protocol = "chunked-json",
            streaming = new
            {
                supported = true,
                protocols = new[] { "chunked-json" },
                features = new[] { "chunked-transfer-encoding", "progressive-loading", "real-time-streaming" }
            },
            endpoints = new
            {
                mcp_jsonrpc = "/mcp (POST) - Main MCP protocol endpoint with streaming support",
                mcp_tools = "/mcp/tools (GET) - List available tools",
                mcp_capabilities = "/mcp/capabilities (GET) - This endpoint",
                mcp_initialize = "/mcp/initialize (GET) - Server initialization info"
            },
            tools = new
            {
                streaming = new[] { "get_restaurants_stream", "analyze_restaurants_stream", "search_restaurants_stream" },
                standard = new[] { "get_restaurants", "add_restaurant", "pick_random_restaurant", "get_visit_stats" }
            }
        });
    }

    private JsonRpcResponse HandleInitialize(JsonRpcRequest request)
    {
        var result = new McpInitializeResult
        {
            ProtocolVersion = "2024-11-05",
            Capabilities = new McpServerCapabilities
            {
                Tools = new McpToolsCapability { ListChanged = true, Streaming = true },
                Resources = new McpResourcesCapability { Subscribe = true, ListChanged = true },
                Prompts = new McpPromptsCapability { ListChanged = true },
                Streaming = new McpStreamingServerCapability
                {
                    Supported = true,
                    Protocols = new[] { "chunked-json" },
                    ToolStreaming = true,
                    PromptStreaming = true
                }
            },
            ServerInfo = new McpServerInfo
            {
                Name = "LunchTime MCP Streaming Server",
                Version = "1.0.0",
                Description = "HTTP-based MCP server with chunked streaming for lunch restaurant management"
            }
        };

        return new JsonRpcResponse { Id = request.Id, Result = result };
    }

    private JsonRpcResponse HandleToolsList(JsonRpcRequest request)
    {
        var tools = GetAvailableTools();
        var result = new McpToolsListResult { Tools = tools };
        return new JsonRpcResponse { Id = request.Id, Result = result };
    }

    private async Task<object> HandleToolCallWithStreaming(JsonRpcRequest request)
    {
        try
        {
            var paramsJson = JsonSerializer.Serialize(request.Params);
            var toolCallParams = JsonSerializer.Deserialize<McpToolCallParams>(paramsJson);

            if (toolCallParams == null)
                return new JsonRpcResponse { Id = request.Id, Result = CreateErrorResult("Invalid tool call parameters") };

            // Check if this is a streaming request
            bool shouldStream = toolCallParams.Streaming == true || 
                               IsStreamingTool(toolCallParams.Name) ||
                               Request.Headers.ContainsKey("Accept-Streaming") ||
                               Request.Headers.ContainsKey("X-MCP-Streaming");

            if (shouldStream)
            {
                return await HandleStreamingToolCall(toolCallParams, request.Id?.ToString() ?? Guid.NewGuid().ToString());
            }

            // Handle regular tool call
            var resultText = await _streamingService.ExecuteToolAsync(toolCallParams.Name, toolCallParams.Arguments);
            
            var result = new McpToolCallResult
            {
                Content = new[]
                {
                    new McpContent { Type = "text", Text = resultText }
                },
                Metadata = new McpToolCallMetadata
                {
                    Timestamp = DateTime.UtcNow,
                    Streaming = new StreamingMetadata
                    {
                        ChunkNumber = 1,
                        TotalChunks = 1,
                        IsLast = true
                    }
                }
            };

            return new JsonRpcResponse { Id = request.Id, Result = result };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tool call");
            return new JsonRpcResponse { Id = request.Id, Result = CreateErrorResult($"Tool execution error: {ex.Message}") };
        }
    }

    private async Task<IActionResult> HandleStreamingToolCall(McpToolCallParams toolCallParams, string requestId)
    {
        try
        {
            _logger.LogInformation("Starting MCP streaming for {ToolName}, RequestId: {RequestId}", toolCallParams.Name, requestId);

            // Set chunked transfer encoding headers
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";
            Response.Headers["Transfer-Encoding"] = "chunked";
            Response.ContentType = "application/json";

            var writer = new StreamWriter(Response.Body, Encoding.UTF8);

            // Stream tool execution
            await foreach (var chunk in _streamingService.StreamToolCallAsync(toolCallParams.Name, toolCallParams.Arguments, requestId, HttpContext.RequestAborted))
            {
                if (HttpContext.RequestAborted.IsCancellationRequested)
                    break;

                await writer.WriteLineAsync(chunk);
                await writer.FlushAsync();
            }

            _logger.LogInformation("Completed MCP streaming for {ToolName}, RequestId: {RequestId}", toolCallParams.Name, requestId);
            return new EmptyResult();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("MCP streaming cancelled for {ToolName}, RequestId: {RequestId}", toolCallParams.Name, requestId);
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MCP streaming for {ToolName}, RequestId: {RequestId}", toolCallParams.Name, requestId);
            return StatusCode(500, new { error = "Streaming failed", message = ex.Message });
        }
    }

    private bool IsStreamingTool(string toolName)
    {
        var streamingTools = new[] { "get_restaurants_stream", "analyze_restaurants_stream", "search_restaurants_stream" };
        return streamingTools.Contains(toolName.ToLowerInvariant());
    }

    private JsonRpcResponse HandlePromptsList(JsonRpcRequest request)
    {
        var prompts = new McpPrompt[]
        {
            new()
            {
                Name = "restaurant_recommendation",
                Description = "Get a personalized restaurant recommendation based on preferences",
                Arguments = new[]
                {
                    new McpPromptArgument { Name = "cuisine_type", Description = "Preferred cuisine type", Required = false },
                    new McpPromptArgument { Name = "location_preference", Description = "Preferred location or area", Required = false }
                }
            }
        };

        var result = new McpPromptsListResult { Prompts = prompts };
        return new JsonRpcResponse { Id = request.Id, Result = result };
    }

    private JsonRpcResponse HandlePromptsGet(JsonRpcRequest request)
    {
        try
        {
            var paramsJson = JsonSerializer.Serialize(request.Params);
            var promptParams = JsonSerializer.Deserialize<McpPromptGetParams>(paramsJson);

            if (promptParams?.Name == "restaurant_recommendation")
            {
                var cuisineType = promptParams.Arguments?.GetValueOrDefault("cuisine_type", "any cuisine");
                var location = promptParams.Arguments?.GetValueOrDefault("location_preference", "any location");

                var result = new McpPromptGetResult
                {
                    Description = "Restaurant recommendation prompt",
                    Messages = new[]
                    {
                        new McpPromptMessage
                        {
                            Role = "user",
                            Content = new McpContent
                            {
                                Type = "text",
                                Text = $"I'm looking for a restaurant recommendation. I prefer {cuisineType} cuisine and would like something in {location}. Can you suggest a restaurant from your database and explain why it would be a good choice?"
                            }
                        }
                    }
                };

                return new JsonRpcResponse { Id = request.Id, Result = result };
            }

            return new JsonRpcResponse { Id = request.Id, Result = CreateErrorResult("Prompt not found") };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in prompts/get");
            return new JsonRpcResponse { Id = request.Id, Result = CreateErrorResult($"Prompt error: {ex.Message}") };
        }
    }

    private JsonRpcResponse HandleResourcesList(JsonRpcRequest request)
    {
        var resources = new McpResource[]
        {
            new()
            {
                Uri = "restaurants://data",
                Name = "Restaurant Database",
                Description = "Local restaurant database with visit tracking",
                MimeType = "application/json"
            }
        };

        var result = new McpResourcesListResult { Resources = resources };
        return new JsonRpcResponse { Id = request.Id, Result = result };
    }

    private McpTool[] GetAvailableTools()
    {
        return new McpTool[]
        {
            new()
            {
                Name = "get_restaurants",
                Description = "Get all restaurants in the database",
                InputSchema = new { type = "object", properties = new { } },
                Streaming = false
            },
            new()
            {
                Name = "add_restaurant",
                Description = "Add a new restaurant to the database",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Restaurant name" },
                        location = new { type = "string", description = "Restaurant location/address" },
                        foodType = new { type = "string", description = "Type of cuisine" }
                    },
                    required = new[] { "name", "location", "foodType" }
                },
                Streaming = false
            },
            new()
            {
                Name = "pick_random_restaurant",
                Description = "Randomly select a restaurant for lunch and track the visit",
                InputSchema = new { type = "object", properties = new { } },
                Streaming = false
            },
            new()
            {
                Name = "get_visit_stats",
                Description = "Get statistics about restaurant visits",
                InputSchema = new { type = "object", properties = new { } },
                Streaming = false
            },
            new()
            {
                Name = "get_restaurants_stream",
                Description = "Stream all restaurants with progressive loading (chunked JSON)",
                InputSchema = new { type = "object", properties = new { } },
                Streaming = true
            },
            new()
            {
                Name = "analyze_restaurants_stream",
                Description = "Stream real-time analysis of restaurant data (chunked JSON)",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        type = new { type = "string", description = "Type of analysis to perform", @default = "general" }
                    }
                },
                Streaming = true
            },
            new()
            {
                Name = "search_restaurants_stream",
                Description = "Stream search results as they're found (chunked JSON)",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Search query for restaurants" }
                    },
                    required = new[] { "query" }
                },
                Streaming = true
            }
        };
    }

    private JsonRpcErrorResponse CreateErrorResponse(object id, int code, string message, object data = null)
    {
        return new JsonRpcErrorResponse
        {
            Id = id,
            Error = new JsonRpcError { Code = code, Message = message, Data = data }
        };
    }

    private McpToolCallResult CreateErrorResult(string message)
    {
        return new McpToolCallResult
        {
            Content = new[] { new McpContent { Type = "text", Text = message } },
            IsError = true
        };
    }
}