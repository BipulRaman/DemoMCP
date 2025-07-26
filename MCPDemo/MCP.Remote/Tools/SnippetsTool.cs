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
        try
        {
            return snippetContent;
        }
        catch (Exception ex)
        {
            var snippetName = context.Arguments?.GetValueOrDefault(SnippetNamePropertyName)?.ToString() ?? "unknown";
            logger.LogError(ex, "{Class}_{Method} : Failed to retrieve snippet '{SnippetName}': {ErrorMessage}", 
                nameof(SnippetsTool), nameof(GetSnippet), snippetName, ex.Message);
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
        try
        {
            return snippet;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Class}_{Method} : Failed to save snippet '{SnippetName}': {ErrorMessage}", 
                nameof(SnippetsTool), nameof(SaveSnippet), name, ex.Message);
            throw;
        }
    }
}
