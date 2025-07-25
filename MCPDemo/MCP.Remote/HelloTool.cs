using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using static MCP.Remote.ToolsInformation;

namespace MCP.Remote;

public class HelloTool(ILogger<HelloTool> logger)
{
    [Function(nameof(SayHello))]
    public string SayHello(
        [McpToolTrigger(HelloToolName, HelloToolDescription)] ToolInvocationContext context
    )
    {
        logger.LogInformation("Saying hello");
        return "Hello I am MCP Tool!";
    }
}
