using MCP.sse.Models;
using MCP.sse.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace MCP.sse.Controllers;

/// <summary>
/// Server-Sent Events controller for real-time MCP streaming
/// </summary>
[ApiController]
[Route("sse")]
[Produces("text/event-stream")]
public class SseController : ControllerBase
{
    private readonly SseStreamingService _sseStreamingService;
    private readonly ILogger<SseController> _logger;

    public SseController(SseStreamingService sseStreamingService, ILogger<SseController> logger)
    {
        _sseStreamingService = sseStreamingService;
        _logger = logger;
    }

    /// <summary>
    /// Main SSE stream endpoint for real-time communication
    /// </summary>
    [HttpGet("stream")]
    public async Task<IActionResult> Stream(CancellationToken cancellationToken = default)
    {
        var connectionId = Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation("Starting SSE stream for connection: {ConnectionId}", connectionId);

            // Set SSE headers
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";
            Response.ContentType = "text/event-stream";

            var writer = new StreamWriter(Response.Body, Encoding.UTF8);
            _sseStreamingService.AddConnection(connectionId, writer);

            // Send initial connection event
            var welcomeEvent = new SseEvent
            {
                Event = "connected",
                Id = connectionId,
                Data = new 
                { 
                    status = "connected", 
                    connectionId, 
                    timestamp = DateTime.UtcNow,
                    server = "LunchTime MCP SSE Server",
                    version = "1.0.0"
                }
            };

            await writer.WriteAsync(SseStreamingService.FormatSseEvent(welcomeEvent));
            await writer.FlushAsync();

            // Keep connection alive until cancelled
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(30000, cancellationToken); // 30 second intervals
                
                // Heartbeat is sent automatically by the service
                if (!Response.HttpContext.RequestAborted.IsCancellationRequested)
                {
                    continue;
                }
                else
                {
                    break;
                }
            }

            _logger.LogInformation("SSE stream ended for connection: {ConnectionId}", connectionId);
            return new EmptyResult();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE stream cancelled for connection: {ConnectionId}", connectionId);
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SSE stream for connection: {ConnectionId}", connectionId);
            return new EmptyResult();
        }
        finally
        {
            if (Response.Body != null)
            {
                var writer = new StreamWriter(Response.Body, Encoding.UTF8);
                _sseStreamingService.RemoveConnection(connectionId, writer);
            }
        }
    }

    /// <summary>
    /// Execute a specific tool via SSE streaming
    /// </summary>
    [HttpGet("tools/{toolName}")]
    public async Task<IActionResult> StreamTool(
        string toolName, 
        [FromQuery] string? arguments = null,
        CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString();

        try
        {
            _logger.LogInformation("Starting SSE tool stream for {ToolName}, RequestId: {RequestId}", toolName, requestId);

            // Set SSE headers
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";
            Response.ContentType = "text/event-stream";

            var writer = new StreamWriter(Response.Body, Encoding.UTF8);

            // Parse arguments if provided
            Dictionary<string, object>? toolArguments = null;
            if (!string.IsNullOrEmpty(arguments))
            {
                try
                {
                    toolArguments = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(arguments);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse tool arguments: {Arguments}", arguments);
                    
                    var errorEvent = new SseEvent
                    {
                        Event = "error",
                        Id = requestId,
                        Data = new { error = "Invalid arguments format", message = ex.Message }
                    };
                    
                    await writer.WriteAsync(SseStreamingService.FormatSseEvent(errorEvent));
                    await writer.FlushAsync();
                    return new EmptyResult();
                }
            }

            // Stream tool execution
            await foreach (var sseEvent in _sseStreamingService.StreamToolExecutionAsync(toolName, toolArguments, requestId, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await writer.WriteAsync(SseStreamingService.FormatSseEvent(sseEvent));
                await writer.FlushAsync();
            }

            _logger.LogInformation("Completed SSE tool stream for {ToolName}, RequestId: {RequestId}", toolName, requestId);
            return new EmptyResult();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE tool stream cancelled for {ToolName}, RequestId: {RequestId}", toolName, requestId);
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SSE tool stream for {ToolName}, RequestId: {RequestId}", toolName, requestId);
            
            try
            {
                var writer = new StreamWriter(Response.Body, Encoding.UTF8);
                var errorEvent = new SseEvent
                {
                    Event = "error",
                    Id = requestId,
                    Data = new { error = "Tool execution failed", message = ex.Message }
                };
                
                await writer.WriteAsync(SseStreamingService.FormatSseEvent(errorEvent));
                await writer.FlushAsync();
            }
            catch
            {
                // Ignore if we can't send error
            }
            
            return new EmptyResult();
        }
    }

    /// <summary>
    /// Execute a tool via SSE streaming with POST body
    /// </summary>
    [HttpPost("tools/{toolName}")]
    public async Task<IActionResult> StreamToolPost(
        string toolName,
        [FromBody] SseToolRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var requestId = request?.RequestId ?? Guid.NewGuid().ToString();

        try
        {
            _logger.LogInformation("Starting SSE tool stream (POST) for {ToolName}, RequestId: {RequestId}", toolName, requestId);

            // Set SSE headers
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";
            Response.ContentType = "text/event-stream";

            var writer = new StreamWriter(Response.Body, Encoding.UTF8);

            // Stream tool execution
            await foreach (var sseEvent in _sseStreamingService.StreamToolExecutionAsync(toolName, request?.Arguments, requestId, cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await writer.WriteAsync(SseStreamingService.FormatSseEvent(sseEvent));
                await writer.FlushAsync();
            }

            _logger.LogInformation("Completed SSE tool stream (POST) for {ToolName}, RequestId: {RequestId}", toolName, requestId);
            return new EmptyResult();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE tool stream (POST) cancelled for {ToolName}, RequestId: {RequestId}", toolName, requestId);
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SSE tool stream (POST) for {ToolName}, RequestId: {RequestId}", toolName, requestId);
            
            try
            {
                var writer = new StreamWriter(Response.Body, Encoding.UTF8);
                var errorEvent = new SseEvent
                {
                    Event = "error",
                    Id = requestId,
                    Data = new { error = "Tool execution failed", message = ex.Message }
                };
                
                await writer.WriteAsync(SseStreamingService.FormatSseEvent(errorEvent));
                await writer.FlushAsync();
            }
            catch
            {
                // Ignore if we can't send error
            }
            
            return new EmptyResult();
        }
    }

    /// <summary>
    /// Get SSE capabilities and status
    /// </summary>
    [HttpGet("capabilities")]
    [Produces("application/json")]
    public IActionResult GetCapabilities()
    {
        return Ok(new
        {
            name = "LunchTime MCP SSE Server",
            version = "1.0.0",
            protocol = "server-sent-events",
            streaming = new
            {
                supported = true,
                eventTypes = new[] { "tool-result", "restaurant-update", "error", "heartbeat", "connected" },
                features = new[] { "real-time-streaming", "auto-reconnection", "progressive-loading" }
            },
            endpoints = new
            {
                stream = "/sse/stream (GET) - Main SSE connection",
                toolStream = "/sse/tools/{toolName} (GET) - Stream specific tool execution",
                toolStreamPost = "/sse/tools/{toolName} (POST) - Stream tool with request body",
                capabilities = "/sse/capabilities (GET) - This endpoint"
            },
            tools = new
            {
                streaming = new[] { "get_restaurants_stream", "analyze_restaurants_stream", "search_restaurants_stream" },
                standard = new[] { "get_restaurants", "add_restaurant", "pick_random_restaurant", "get_visit_stats" }
            }
        });
    }
}