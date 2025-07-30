using System.Text.Json;

namespace MCP.HTTP.EntraAuth.Services;

/// <summary>
/// Interface for MCP (Model Context Protocol) connection service that handles MCP requests and responses.
/// </summary>
public interface IMcpConnectService
{
    /// <summary>
    /// Handles an incoming MCP request and returns an appropriate response.
    /// </summary>
    /// <param name="request">The JSON document containing the MCP request</param>
    /// <param name="context">The HTTP context for the request</param>
    /// <returns>A response object that will be serialized and sent back to the client</returns>
    Task<object> HandleMcpRequestAsync(JsonDocument request, HttpContext context);
}
