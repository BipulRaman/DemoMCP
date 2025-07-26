using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using static MCP.Remote.ToolsInformation;

namespace MCP.Remote;

public class SnippetsTool(ILogger<SnippetsTool> logger)
{
    private const string BlobPath = "snippets/{mcptoolargs." + SnippetNamePropertyName + "}.json";

    [Function(nameof(GetSnippet))]
    public object GetSnippet(
        [McpToolTrigger(GetSnippetToolName, GetSnippetToolDescription)]
            ToolInvocationContext context,
        [BlobInput(BlobPath)] string snippetContent
    )
    {
        var stopwatch = Stopwatch.StartNew();
        var snippetName = context.Arguments[SnippetNamePropertyName]?.ToString();
        
        logger.LogToolOperationStart(nameof(SnippetsTool), nameof(GetSnippet), new { snippetName });
        
        try
        {
            if (string.IsNullOrEmpty(snippetContent))
            {
                logger.LogWarning("Snippet not found: {SnippetName}", snippetName);
                var notFoundMessage = $"Snippet '{snippetName}' not found.";
                logger.LogToolOperationComplete(nameof(SnippetsTool), nameof(GetSnippet), stopwatch.ElapsedMilliseconds);
                return notFoundMessage;
            }
            
            logger.LogBlobOperation("Read", BlobPath.Replace("{mcptoolargs." + SnippetNamePropertyName + "}", snippetName ?? "unknown"), snippetContent.Length);
            logger.LogToolOperationComplete(nameof(SnippetsTool), nameof(GetSnippet), stopwatch.ElapsedMilliseconds);
            
            return snippetContent;
        }
        catch (Exception ex)
        {
            logger.LogToolOperationError(nameof(SnippetsTool), nameof(GetSnippet), ex);
            throw;
        }
    }

    [Function(nameof(SaveSnippet))]
    [BlobOutput(BlobPath)]
    public string SaveSnippet(
        [McpToolTrigger(SaveSnippetToolName, SaveSnippetToolDescription)]
            ToolInvocationContext context,
        [McpToolProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription)]
            string name,
        [McpToolProperty(SnippetPropertyName, PropertyType, SnippetPropertyDescription)]
            string snippet
    )
    {
        var stopwatch = Stopwatch.StartNew();
        
        logger.LogToolOperationStart(nameof(SnippetsTool), nameof(SaveSnippet), 
            new { name, contentLength = snippet?.Length ?? 0 });
        
        try
        {
            if (string.IsNullOrEmpty(name))
            {
                logger.LogError("Attempted to save snippet with empty name");
                throw new ArgumentException("Snippet name cannot be empty", nameof(name));
            }
            
            if (string.IsNullOrEmpty(snippet))
            {
                logger.LogWarning("Saving empty snippet: {SnippetName}", name);
            }
            
            logger.LogBlobOperation("Write", BlobPath.Replace("{mcptoolargs." + SnippetNamePropertyName + "}", name), snippet?.Length ?? 0);
            logger.LogToolOperationComplete(nameof(SnippetsTool), nameof(SaveSnippet), stopwatch.ElapsedMilliseconds);
            
            return snippet;
        }
        catch (Exception ex)
        {
            logger.LogToolOperationError(nameof(SnippetsTool), nameof(SaveSnippet), ex);
            throw;
        }
    }
}
