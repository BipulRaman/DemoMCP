using System.ComponentModel;
using System.Text.Json;
using MCP.Common.Services;
using MCP.Shared;
using ModelContextProtocol.Server;

namespace MCP.Common.Tools;

[McpServerToolType]
public sealed class SnippetTools
{
    private readonly Services.ISnippetService snippetService;

    public SnippetTools(Services.ISnippetService snippetService)
    {
        this.snippetService = snippetService;
    }

    [McpServerTool, Description("Get a code snippet by name")]
    public async Task<string> GetSnippet([Description("The name of the snippet to retrieve")] string snippetName)
    {
        try
        {
            var content = await snippetService.GetSnippetAsync(snippetName);
            var response = new SnippetResponse
            {
                Success = true,
                Snippet = new Snippet
                {
                    Name = snippetName,
                    Content = content
                },
                SnippetName = snippetName
            };
            return JsonSerializer.Serialize(response, SnippetContext.Default.SnippetResponse);
        }
        catch (Exception ex)
        {
            var response = new SnippetResponse
            {
                Success = false,
                Error = ex.Message,
                SnippetName = snippetName
            };
            return JsonSerializer.Serialize(response, SnippetContext.Default.SnippetResponse);
        }
    }

    [McpServerTool, Description("Save a code snippet with a name and content")]
    public async Task<string> SaveSnippet(
        [Description("The name of the snippet")] string snippetName,
        [Description("The code content of the snippet")] string snippet)
    {
        try
        {
            await snippetService.SaveSnippetAsync(snippetName, snippet);
            var response = new SnippetResponse
            {
                Success = true,
                Message = $"Snippet '{snippetName}' saved successfully",
                SnippetName = snippetName
            };
            return JsonSerializer.Serialize(response, SnippetContext.Default.SnippetResponse);
        }
        catch (Exception ex)
        {
            var response = new SnippetResponse
            {
                Success = false,
                Error = ex.Message,
                SnippetName = snippetName
            };
            return JsonSerializer.Serialize(response, SnippetContext.Default.SnippetResponse);
        }
    }

    [McpServerTool, Description("List all available code snippets")]
    public async Task<string> ListSnippets()
    {
        try
        {
            var snippets = await snippetService.ListSnippetsAsync();
            var snippetList = snippets.ToList();
            var response = new SnippetListResponse
            {
                Success = true,
                Snippets = snippetList,
                Count = snippetList.Count
            };
            return JsonSerializer.Serialize(response, SnippetContext.Default.SnippetListResponse);
        }
        catch (Exception ex)
        {
            var response = new SnippetListResponse
            {
                Success = false,
                Error = ex.Message
            };
            return JsonSerializer.Serialize(response, SnippetContext.Default.SnippetListResponse);
        }
    }

    [McpServerTool, Description("Delete a code snippet by name")]
    public async Task<string> DeleteSnippet([Description("The name of the snippet to delete")] string snippetName)
    {
        try
        {
            // Check if snippet exists before trying to delete
            var exists = await snippetService.SnippetExistsAsync(snippetName);
            if (!exists)
            {
                var notFoundResponse = new SnippetResponse
                {
                    Success = false,
                    Error = $"Snippet '{snippetName}' not found",
                    SnippetName = snippetName
                };
                return JsonSerializer.Serialize(notFoundResponse, SnippetContext.Default.SnippetResponse);
            }

            await snippetService.DeleteSnippetAsync(snippetName);
            var response = new SnippetResponse
            {
                Success = true,
                Message = $"Snippet '{snippetName}' deleted successfully",
                SnippetName = snippetName
            };
            return JsonSerializer.Serialize(response, SnippetContext.Default.SnippetResponse);
        }
        catch (Exception ex)
        {
            var response = new SnippetResponse
            {
                Success = false,
                Error = ex.Message,
                SnippetName = snippetName
            };
            return JsonSerializer.Serialize(response, SnippetContext.Default.SnippetResponse);
        }
    }

    [McpServerTool, Description("Get detailed information about a specific snippet including metadata")]
    public async Task<string> GetSnippetDetails([Description("The name of the snippet to get details for")] string snippetName)
    {
        try
        {
            var snippet = await snippetService.GetSnippetDetailsAsync(snippetName);
            if (snippet == null)
            {
                var notFoundResponse = new SnippetResponse
                {
                    Success = false,
                    Error = $"Snippet '{snippetName}' not found",
                    SnippetName = snippetName
                };
                return JsonSerializer.Serialize(notFoundResponse, SnippetContext.Default.SnippetResponse);
            }

            var response = new SnippetResponse
            {
                Success = true,
                Snippet = snippet,
                SnippetName = snippetName
            };
            return JsonSerializer.Serialize(response, SnippetContext.Default.SnippetResponse);
        }
        catch (Exception ex)
        {
            var response = new SnippetResponse
            {
                Success = false,
                Error = ex.Message,
                SnippetName = snippetName
            };
            return JsonSerializer.Serialize(response, SnippetContext.Default.SnippetResponse);
        }
    }

    [McpServerTool, Description("Check if a snippet exists")]
    public async Task<string> SnippetExists([Description("The name of the snippet to check")] string snippetName)
    {
        try
        {
            var exists = await snippetService.SnippetExistsAsync(snippetName);
            var response = new
            {
                success = true,
                exists = exists,
                snippetName = snippetName,
                message = exists ? $"Snippet '{snippetName}' exists" : $"Snippet '{snippetName}' does not exist"
            };
            return JsonSerializer.Serialize(response);
        }
        catch (Exception ex)
        {
            var response = new
            {
                success = false,
                error = ex.Message,
                snippetName = snippetName
            };
            return JsonSerializer.Serialize(response);
        }
    }
}
