using MCP.HTTP.EntraAuth.Services;
using System.ComponentModel;

namespace MCP.HTTP.EntraAuth.MCP;

/// <summary>
/// MCP Resources for snippet management with authentication
/// </summary>
public class SnippetResources
{
    private readonly ISnippetService _snippetService;
    private readonly IAuthenticationStateService _authService;
    private readonly ILogger<SnippetResources> _logger;

    public SnippetResources(
        ISnippetService snippetService,
        IAuthenticationStateService authService,
        ILogger<SnippetResources> logger)
    {
        _snippetService = snippetService;
        _authService = authService;
        _logger = logger;
    }

    [Description("Get snippet content as a resource")]
    public async Task<object> GetSnippetResource(string uri, string sessionId)
    {
        try
        {
            // Parse snippet name from URI (e.g., "snippet://example-snippet")
            if (!uri.StartsWith("snippet://"))
            {
                return new { error = "Invalid snippet URI format" };
            }

            var snippetName = uri.Substring("snippet://".Length);
            
            var isAuthenticated = await _authService.IsSessionAuthenticatedAsync(sessionId);
            if (!isAuthenticated)
            {
                return new { error = "Authentication required" };
            }

            var content = await _snippetService.GetSnippetAsync(snippetName);
            if (string.IsNullOrEmpty(content))
            {
                return new { error = $"Snippet '{snippetName}' not found" };
            }

            return new { content = content, mimeType = "text/plain" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get snippet resource {Uri} for session {SessionId}", uri, sessionId);
            return new { error = ex.Message };
        }
    }

    [Description("List available snippet resources")]
    public async Task<object> ListSnippetResources(string sessionId)
    {
        try
        {
            var isAuthenticated = await _authService.IsSessionAuthenticatedAsync(sessionId);
            if (!isAuthenticated)
            {
                return new { resources = new object[0] };
            }

            var snippetNames = await _snippetService.ListSnippetsAsync();
            
            var resources = new List<object>();
            foreach (var name in snippetNames)
            {
                var snippet = await _snippetService.GetSnippetDetailsAsync(name);
                if (snippet != null)
                {
                    resources.Add(new
                    {
                        uri = $"snippet://{snippet.Name}",
                        name = snippet.Name,
                        description = $"{snippet.Language} snippet: {snippet.Name}",
                        mimeType = "text/plain"
                    });
                }
            }

            return new { resources = resources.ToArray() };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list snippet resources for session {SessionId}", sessionId);
            return new { resources = new object[0] };
        }
    }
}