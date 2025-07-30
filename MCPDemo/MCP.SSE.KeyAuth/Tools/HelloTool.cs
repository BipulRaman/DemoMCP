using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using static MCP.SSE.KeyAuth.Tools.ToolsInformation;

namespace MCP.SSE.KeyAuth.Tools;

public class HelloTool(ILogger<HelloTool> logger)
{
    [Function(nameof(SayHello))]
    public string SayHello(
        [McpToolTrigger(HelloToolName, HelloToolDescription)] ToolInvocationContext context
    )
    {
        logger.LogInformation("{Class}_{Method} : Saying hello", nameof(HelloTool), nameof(SayHello));
        return "Hello I am MCP Tool!";
    }
}
