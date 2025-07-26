using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using MCP.Remote.Services;
using static MCP.Remote.Tools.ToolsInformation;

namespace MCP.Remote.Tools;

public class SnippetsTool
{
    private readonly ISnippetService _snippetService;
    private readonly ILogger<SnippetsTool> _logger;

    public SnippetsTool(ISnippetService snippetService, ILogger<SnippetsTool> logger)
    {
        _snippetService = snippetService ?? throw new ArgumentNullException(nameof(snippetService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Function(nameof(GetSnippet))]
    public async Task<object> GetSnippet(
        [McpToolTrigger(GetSnippetToolName, GetSnippetToolDescription)]
            ToolInvocationContext context
    )
    {
        try
        {
            var snippetName = context.Arguments?.GetValueOrDefault(SnippetNamePropertyName)?.ToString();
            
            if (string.IsNullOrWhiteSpace(snippetName))
            {
                throw new ArgumentException("Snippet name is required", nameof(snippetName));
            }

            var content = await _snippetService.GetSnippetAsync(snippetName);
            return new { success = true, content, snippetName };
        }
        catch (Exception ex)
        {
            var snippetName = context.Arguments?.GetValueOrDefault(SnippetNamePropertyName)?.ToString() ?? "unknown";
            _logger.LogError(ex, "{Class}_{Method} : Failed to retrieve snippet '{SnippetName}': {ErrorMessage}", 
                nameof(SnippetsTool), nameof(GetSnippet), snippetName, ex.Message);
            
            return new { success = false, error = ex.Message, snippetName };
        }
    }

    [Function(nameof(SaveSnippet))]
    public async Task<object> SaveSnippet(
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
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Snippet name is required", nameof(name));
            }

            if (snippet == null)
            {
                throw new ArgumentNullException(nameof(snippet), "Snippet content cannot be null");
            }

            await _snippetService.SaveSnippetAsync(name, snippet);
            return new { success = true, message = $"Snippet '{name}' saved successfully", snippetName = name };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Class}_{Method} : Failed to save snippet '{SnippetName}': {ErrorMessage}", 
                nameof(SnippetsTool), nameof(SaveSnippet), name, ex.Message);
            
            return new { success = false, error = ex.Message, snippetName = name };
        }
    }

    [Function(nameof(ListSnippets))]
    public async Task<object> ListSnippets(
        [McpToolTrigger("list_snippets", "List all available code snippets")]
            ToolInvocationContext context
    )
    {
        try
        {
            var snippets = await _snippetService.ListSnippetsAsync();
            return new { success = true, snippets = snippets.ToArray(), count = snippets.Count() };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Class}_{Method} : Failed to list snippets: {ErrorMessage}", 
                nameof(SnippetsTool), nameof(ListSnippets), ex.Message);
            
            return new { success = false, error = ex.Message };
        }
    }

    [Function(nameof(DeleteSnippet))]
    public async Task<object> DeleteSnippet(
        [McpToolTrigger("delete_snippet", "Delete a code snippet by name")]
            ToolInvocationContext context,
        [McpToolProperty(SnippetNamePropertyName, PropertyType, SnippetNamePropertyDescription)]
            string name
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Snippet name is required", nameof(name));
            }

            // Check if snippet exists before trying to delete
            var exists = await _snippetService.SnippetExistsAsync(name);
            if (!exists)
            {
                return new { success = false, error = $"Snippet '{name}' not found", snippetName = name };
            }

            await _snippetService.DeleteSnippetAsync(name);
            return new { success = true, message = $"Snippet '{name}' deleted successfully", snippetName = name };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Class}_{Method} : Failed to delete snippet '{SnippetName}': {ErrorMessage}", 
                nameof(SnippetsTool), nameof(DeleteSnippet), name, ex.Message);
            
            return new { success = false, error = ex.Message, snippetName = name };
        }
    }
}
