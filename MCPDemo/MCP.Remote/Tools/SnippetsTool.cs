using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using static MCP.Remote.Tools.ToolsInformation;

namespace MCP.Remote.Tools;

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
        var snippetName = context.Arguments?[SnippetNamePropertyName]?.ToString();
        
        logger.LogInformation("{Class}_{Method} : Starting operation for snippet '{SnippetName}'", nameof(SnippetsTool), nameof(GetSnippet), snippetName);
        
        try
        {
            if (string.IsNullOrEmpty(snippetContent))
            {
                logger.LogWarning("{Class}_{Method} : Snippet not found: '{SnippetName}'", nameof(SnippetsTool), nameof(GetSnippet), snippetName);
                var notFoundMessage = $"Snippet '{snippetName}' not found.";
                return notFoundMessage;
            }
            
            logger.LogInformation("{Class}_{Method} : Successfully retrieved snippet '{SnippetName}' with {ContentLength} characters", nameof(SnippetsTool), nameof(GetSnippet), snippetName, snippetContent.Length);
            
            return snippetContent;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Class}_{Method} : Failed to retrieve snippet '{SnippetName}': {ErrorMessage}", nameof(SnippetsTool), nameof(GetSnippet), snippetName, ex.Message);
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
        logger.LogInformation("{Class}_{Method} : Starting operation to save snippet '{SnippetName}' with {ContentLength} characters", nameof(SnippetsTool), nameof(SaveSnippet), name, snippet?.Length ?? 0);
        
        try
        {
            if (string.IsNullOrEmpty(name))
            {
                logger.LogError("{Class}_{Method} : Attempted to save snippet with empty name", nameof(SnippetsTool), nameof(SaveSnippet));
                throw new ArgumentException("Snippet name cannot be empty", nameof(name));
            }
            
            if (string.IsNullOrEmpty(snippet))
            {
                logger.LogWarning("{Class}_{Method} : Saving empty snippet '{SnippetName}'", nameof(SnippetsTool), nameof(SaveSnippet), name);
            }
            
            logger.LogInformation("{Class}_{Method} : Successfully saved snippet '{SnippetName}'", nameof(SnippetsTool), nameof(SaveSnippet), name);
            
            return snippet ?? string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Class}_{Method} : Failed to save snippet '{SnippetName}': {ErrorMessage}", nameof(SnippetsTool), nameof(SaveSnippet), name, ex.Message);
            throw;
        }
    }
}
