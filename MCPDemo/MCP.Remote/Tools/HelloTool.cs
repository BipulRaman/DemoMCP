using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using static MCP.Remote.Tools.ToolsInformation;

namespace MCP.Remote.Tools;

public class HelloTool(ILogger<HelloTool> logger)
{
    [Function(nameof(SayHello))]
    public string SayHello(
        [McpToolTrigger(HelloToolName, HelloToolDescription)] ToolInvocationContext context
    )
    {
        logger.LogInformation("{Class}_{Method} : Starting operation", nameof(HelloTool), nameof(SayHello));
        
        try
        {
            const string message = "Hello I am MCP Tool!";
            logger.LogInformation("{Class}_{Method} : Generated hello message: {Message}", nameof(HelloTool), nameof(SayHello), message);
            
            return message;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Class}_{Method} : Failed to generate hello message: {ErrorMessage}", nameof(HelloTool), nameof(SayHello), ex.Message);
            throw;
        }
    }
}
