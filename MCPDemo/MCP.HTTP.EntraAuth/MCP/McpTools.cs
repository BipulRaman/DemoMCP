using MCP.HTTP.EntraAuth.Services;
using MCP.HTTP.EntraAuth.Models;
using System.ComponentModel;

namespace MCP.HTTP.EntraAuth.MCP;

/// <summary>
/// MCP Tools with integrated device code authentication - preserving existing functionality
/// </summary>
public class McpTools
{
    private readonly ISnippetService _snippetService;
    private readonly IAuthenticationStateService _authService;
    private readonly ILogger<McpTools> _logger;

    public McpTools(
        ISnippetService snippetService,
        IAuthenticationStateService authService,
        ILogger<McpTools> logger)
    {
        _snippetService = snippetService;
        _authService = authService;
        _logger = logger;
    }

    [Description("Start device code authentication flow for MCP session")]
    public async Task<object> StartAuthentication(string sessionId)
    {
        try
        {
            var result = await _authService.StartDeviceCodeFlowAsync(sessionId);
            
            return new
            {
                success = true,
                userCode = result.UserCode,
                verificationUri = result.VerificationUri,
                verificationUriComplete = result.VerificationUriComplete,
                expiresIn = result.ExpiresIn,
                message = $"Please visit {result.VerificationUri} and enter code: {result.UserCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start authentication for session {SessionId}", sessionId);
            return new { success = false, error = ex.Message };
        }
    }

    [Description("Check authentication status for current session")]
    public async Task<object> CheckAuthenticationStatus(string sessionId)
    {
        try
        {
            var isAuthenticated = await _authService.IsSessionAuthenticatedAsync(sessionId);
            
            return new 
            { 
                success = true,
                authenticated = isAuthenticated,
                message = isAuthenticated ? "Session is authenticated" : "Session is not authenticated"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check authentication for session {SessionId}", sessionId);
            return new { success = false, error = ex.Message };
        }
    }

    [Description("Get a code snippet by name (requires authentication)")]
    public async Task<object> GetSnippet(string sessionId, string name)
    {
        try
        {
            var isAuthenticated = await _authService.IsSessionAuthenticatedAsync(sessionId);
            if (!isAuthenticated)
            {
                return new { success = false, error = "Authentication required. Use StartAuthentication first." };
            }

            var snippetDetails = await _snippetService.GetSnippetDetailsAsync(name);
            if (snippetDetails == null)
            {
                return new { success = false, error = $"Snippet '{name}' not found" };
            }

            return new 
            { 
                success = true, 
                snippet = new 
                { 
                    name = snippetDetails.Name,
                    content = snippetDetails.Content,
                    language = snippetDetails.Language
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get snippet {Name} for session {SessionId}", name, sessionId);
            return new { success = false, error = ex.Message };
        }
    }

    [Description("Save a new code snippet (requires authentication)")]
    public async Task<object> SaveSnippet(string sessionId, string name, string content, string language = "")
    {
        try
        {
            var isAuthenticated = await _authService.IsSessionAuthenticatedAsync(sessionId);
            if (!isAuthenticated)
            {
                return new { success = false, error = "Authentication required. Use StartAuthentication first." };
            }

            await _snippetService.SaveSnippetAsync(name, content);

            return new 
            { 
                success = true, 
                message = $"Snippet '{name}' saved successfully",
                snippet = new 
                { 
                    name = name,
                    language = language
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save snippet {Name} for session {SessionId}", name, sessionId);
            return new { success = false, error = ex.Message };
        }
    }

    [Description("List all available code snippets (requires authentication)")]
    public async Task<object> ListSnippets(string sessionId)
    {
        try
        {
            var isAuthenticated = await _authService.IsSessionAuthenticatedAsync(sessionId);
            if (!isAuthenticated)
            {
                return new { success = false, error = "Authentication required. Use StartAuthentication first." };
            }

            var snippetNames = await _snippetService.ListSnippetsAsync();

            var snippetDetails = new List<object>();
            foreach (var name in snippetNames)
            {
                var details = await _snippetService.GetSnippetDetailsAsync(name);
                if (details != null)
                {
                    snippetDetails.Add(new
                    {
                        name = details.Name,
                        language = details.Language,
                        preview = details.Content.Length > 100 ? details.Content.Substring(0, 100) + "..." : details.Content
                    });
                }
            }

            return new 
            { 
                success = true, 
                count = snippetDetails.Count,
                snippets = snippetDetails.ToArray()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list snippets for session {SessionId}", sessionId);
            return new { success = false, error = ex.Message };
        }
    }
}