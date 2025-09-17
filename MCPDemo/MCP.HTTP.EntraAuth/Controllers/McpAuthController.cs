using MCP.HTTP.EntraAuth.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MCP.HTTP.EntraAuth.Controllers;

/// <summary>
/// The McpAuthController handles MCP connections with device code authentication through chat.
/// </summary>
[ApiController]
public class McpAuthController : ControllerBase
{
    private readonly ILogger<McpAuthController> _logger;

    /// <summary>
    /// The constructor initializes the controller with necessary services.
    /// </summary>
    /// <param name="logger"></param>
    public McpAuthController(ILogger<McpAuthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the health status of the MCP authentication service.
    /// </summary>
    /// <returns></returns>
    [HttpGet("/health")]
    public IActionResult Health()
    {
        return Ok(new { status = "ok", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Gets the capabilities of the MCP service with device code authentication support.
    /// </summary>
    /// <returns></returns>
    [HttpGet("/capabilities")]
    public IActionResult GetCapabilities()
    {
        _logger.LogInformation("{Class}_{Method} : Capabilities endpoint called",
            nameof(McpAuthController), nameof(GetCapabilities));
        
        return Ok(new
        {
            name = "MCP.HTTP.EntraAuth",
            version = "1.0.0",
            description = "MCP server with device code authentication through chat interface",
            authentication = new
            {
                required = false, // No auth required to connect
                type = "device_code",
                flow = "chat_based",
                instructions = new
                {
                    step1 = "Connect to MCP server without authentication",
                    step2 = "Use 'start_authentication' tool through chat to begin device code flow",
                    step3 = "Follow chat instructions to complete authentication",
                    step4 = "Access protected features after authentication"
                }
            },
            features = new[]
            {
                "Device code authentication through chat",
                "No authentication barriers on connection",
                "Protected snippet management tools",
                "Real-time authentication status"
            },
            transport = "http"
        });
    }

    /// <summary>
    /// Connects to the Model Context Protocol (MCP) service with chat-based device code authentication.
    /// Users can authenticate through chat using device code flow when needed.
    /// </summary>
    /// <param name="mcpService"></param>
    /// <returns></returns>
    [HttpPost("/mcp")]
    public async Task<IActionResult> McpConnect([FromServices] IMcpConnectService mcpService)
    {
        _logger.LogInformation("{Class}_{Method} : MCP connect endpoint called",
            nameof(McpAuthController), nameof(McpConnect));

        try
        {
            var body = await new StreamReader(Request.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
            {
                return BadRequest(new
                {
                    jsonrpc = "2.0",
                    error = new
                    {
                        code = -32600,
                        message = "Invalid Request"
                    },
                    id = (object?)null
                });
            }

            using var jsonDoc = JsonDocument.Parse(body);
            var response = await mcpService.HandleMcpRequestAsync(jsonDoc, HttpContext);

            return Ok(response);
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                jsonrpc = "2.0",
                error = new
                {
                    code = -32700,
                    message = "Parse error"
                },
                id = (object?)null
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                jsonrpc = "2.0",
                error = new
                {
                    code = -32603,
                    message = "Internal error",
                    data = ex.Message
                },
                id = (object?)null
            });
        }
    }
}