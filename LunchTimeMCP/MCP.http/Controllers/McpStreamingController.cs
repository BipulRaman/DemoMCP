using MCP.http.Models;
using MCP.http.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace MCP.http.Controllers;

/// <summary>
/// Streamable MCP (Model Context Protocol) server controller with Server-Sent Events and chunked responses
/// </summary>
[ApiController]
[Route("mcp/stream")]
[Produces("application/json", "text/event-stream")]
public class McpStreamingController : ControllerBase
{
    private readonly StreamingService _streamingService;
    private readonly RestaurantService _restaurantService;
    private readonly ILogger<McpStreamingController> _logger;

    public McpStreamingController(
        StreamingService streamingService, 
        RestaurantService restaurantService,
        ILogger<McpStreamingController> logger)
    {
        _streamingService = streamingService;
        _restaurantService = restaurantService;
        _logger = logger;
    }

    /// <summary>
    /// Server-Sent Events endpoint for streaming tool calls
    /// </summary>
    [HttpPost("tools/call/sse")]
    public async Task<IActionResult> StreamToolCallSSE([FromBody] StreamingToolCallRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting SSE stream for tool: {ToolName}, RequestId: {RequestId}", request.ToolName, request.RequestId);

            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";
            Response.ContentType = "text/event-stream";

            var writer = new StreamWriter(Response.Body, Encoding.UTF8);

            // Send initial connection event
            await writer.WriteLineAsync($"event: connected");
            await writer.WriteLineAsync($"data: {{\"status\": \"connected\", \"requestId\": \"{request.RequestId}\", \"timestamp\": \"{DateTime.UtcNow:O}\"}}");
            await writer.WriteLineAsync();
            await writer.FlushAsync();

            // Stream tool call results
            await foreach (var chunk in _streamingService.StreamToolCallAsync(request.ToolName, request.Arguments, request.RequestId, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await writer.WriteLineAsync($"event: chunk");
                await writer.WriteLineAsync($"data: {chunk}");
                await writer.WriteLineAsync();
                await writer.FlushAsync();
            }

            // Send completion event
            await writer.WriteLineAsync($"event: complete");
            await writer.WriteLineAsync($"data: {{\"status\": \"complete\", \"requestId\": \"{request.RequestId}\", \"timestamp\": \"{DateTime.UtcNow:O}\"}}");
            await writer.WriteLineAsync();
            await writer.FlushAsync();

            _logger.LogInformation("Completed SSE stream for RequestId: {RequestId}", request.RequestId);
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SSE streaming for RequestId: {RequestId}", request.RequestId);
            
            try
            {
                var writer = new StreamWriter(Response.Body, Encoding.UTF8);
                await writer.WriteLineAsync($"event: error");
                await writer.WriteLineAsync($"data: {{\"error\": \"{ex.Message}\", \"requestId\": \"{request.RequestId}\", \"timestamp\": \"{DateTime.UtcNow:O}\"}}");
                await writer.WriteLineAsync();
                await writer.FlushAsync();
            }
            catch
            {
                // If we can't write the error, just return
            }

            return new EmptyResult();
        }
    }

    /// <summary>
    /// Chunked JSON streaming endpoint for tool calls
    /// </summary>
    [HttpPost("tools/call/chunked")]
    public async Task<IActionResult> StreamToolCallChunked([FromBody] StreamingToolCallRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting chunked stream for tool: {ToolName}, RequestId: {RequestId}", request.ToolName, request.RequestId);

            Response.Headers["Transfer-Encoding"] = "chunked";
            Response.ContentType = "application/json";

            var writer = new StreamWriter(Response.Body, Encoding.UTF8, leaveOpen: true);

            // Send opening bracket for JSON array
            await writer.WriteAsync("[");
            await writer.FlushAsync();

            bool isFirst = true;

            await foreach (var chunk in _streamingService.StreamToolCallAsync(request.ToolName, request.Arguments, request.RequestId, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (!isFirst)
                {
                    await writer.WriteAsync(",");
                }
                
                await writer.WriteAsync(chunk);
                await writer.FlushAsync();
                isFirst = false;
            }

            // Send closing bracket for JSON array
            await writer.WriteAsync("]");
            await writer.FlushAsync();

            _logger.LogInformation("Completed chunked stream for RequestId: {RequestId}", request.RequestId);
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chunked streaming for RequestId: {RequestId}", request.RequestId);
            var errorResponse = CreateErrorResponse(request.RequestId, -32603, "Streaming error", ex.Message);
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Stream restaurant data with Server-Sent Events
    /// </summary>
    [HttpGet("restaurants/sse")]
    public async Task<IActionResult> StreamRestaurantsSSE(CancellationToken cancellationToken = default)
    {
        try
        {
            var requestId = Guid.NewGuid().ToString();
            _logger.LogInformation("Starting restaurant SSE stream, RequestId: {RequestId}", requestId);

            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";
            Response.ContentType = "text/event-stream";

            var writer = new StreamWriter(Response.Body, Encoding.UTF8);

            // Get restaurants from service
            var restaurants = await _restaurantService.GetRestaurantsAsync();

            // Send header
            var headerData = new { 
                message = "??? Streaming Restaurant Data", 
                totalCount = restaurants.Count, 
                requestId = requestId,
                timestamp = DateTime.UtcNow
            };
            
            await writer.WriteLineAsync($"event: header");
            await writer.WriteLineAsync($"data: {JsonSerializer.Serialize(headerData)}");
            await writer.WriteLineAsync();
            await writer.FlushAsync();

            // Stream each restaurant
            for (int i = 0; i < restaurants.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var restaurant = restaurants[i];
                var restaurantData = new {
                    sequence = i + 1,
                    total = restaurants.Count,
                    restaurant = restaurant,
                    requestId = requestId,
                    timestamp = DateTime.UtcNow
                };

                await writer.WriteLineAsync($"event: restaurant");
                await writer.WriteLineAsync($"data: {JsonSerializer.Serialize(restaurantData)}");
                await writer.WriteLineAsync();
                await writer.FlushAsync();
                
                await Task.Delay(100, cancellationToken); // Simulate processing time
            }

            // Send completion
            var completionData = new { 
                status = "complete", 
                totalSent = restaurants.Count, 
                requestId = requestId,
                timestamp = DateTime.UtcNow
            };
            
            await writer.WriteLineAsync($"event: complete");
            await writer.WriteLineAsync($"data: {JsonSerializer.Serialize(completionData)}");
            await writer.WriteLineAsync();
            await writer.FlushAsync();

            _logger.LogInformation("Completed restaurant SSE stream, RequestId: {RequestId}", requestId);
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in restaurant SSE streaming");
            var errorResponse = CreateErrorResponse(null, -32603, "SSE streaming error", ex.Message);
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Get streaming capabilities and available streaming tools
    /// </summary>
    [HttpGet("capabilities")]
    public IActionResult GetStreamingCapabilities()
    {
        try
        {
            var capabilities = new
            {
                streaming = new
                {
                    supported = true,
                    protocols = new[] { "sse", "chunked-json" },
                    endpoints = new
                    {
                        toolCallSSE = "/mcp/stream/tools/call/sse",
                        toolCallChunked = "/mcp/stream/tools/call/chunked",
                        restaurantsSSE = "/mcp/stream/restaurants/sse"
                    },
                    supportedTools = new[]
                    {
                        new { name = "get_restaurants_stream", description = "Stream restaurant data progressively", streaming = true },
                        new { name = "analyze_restaurants_stream", description = "Stream restaurant analysis results", streaming = true },
                        new { name = "search_restaurants_stream", description = "Stream restaurant search results", streaming = true }
                    }
                },
                server = new
                {
                    name = "LunchTime MCP Streaming Server",
                    version = "1.0.0",
                    description = "HTTP-based streamable MCP server for restaurant management",
                    protocolVersion = "2024-11-05"
                },
                timestamp = DateTime.UtcNow
            };

            return Ok(capabilities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting streaming capabilities");
            var errorResponse = CreateErrorResponse(null, -32603, "Capabilities error", ex.Message);
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Health check for streaming endpoints
    /// </summary>
    [HttpGet("health")]
    public IActionResult StreamingHealthCheck()
    {
        return Ok(new 
        { 
            status = "healthy", 
            streaming = "available",
            capabilities = new[] { "sse", "chunked-json" },
            timestamp = DateTime.UtcNow 
        });
    }

    private JsonRpcErrorResponse CreateErrorResponse(object? id, int code, string message, object? data = null)
    {
        return new JsonRpcErrorResponse
        {
            Id = id,
            Error = new JsonRpcError
            {
                Code = code,
                Message = message,
                Data = data
            }
        };
    }
}