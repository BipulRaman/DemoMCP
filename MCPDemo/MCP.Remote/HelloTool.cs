using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using static MCP.Remote.ToolsInformation;

namespace MCP.Remote;

public class HelloTool(ILogger<HelloTool> logger)
{
    [Function(nameof(SayHello))]
    public string SayHello(
        [McpToolTrigger(HelloToolName, HelloToolDescription)] ToolInvocationContext context
    )
    {
        var stopwatch = Stopwatch.StartNew();
        
        logger.LogToolOperationStart(nameof(HelloTool), nameof(SayHello));
        
        try
        {
            const string message = "Hello I am MCP Tool!";
            logger.LogInformation("Generated hello message: {Message}", message);
            
            logger.LogToolOperationComplete(nameof(HelloTool), nameof(SayHello), stopwatch.ElapsedMilliseconds);
            
            return message;
        }
        catch (Exception ex)
        {
            logger.LogToolOperationError(nameof(HelloTool), nameof(SayHello), ex);
            throw;
        }
    }
}
