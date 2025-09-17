using MCP.HTTP.EntraAuth.Services;
using MCP.HTTP.EntraAuth.Models;
using System.ComponentModel;

namespace MCP.HTTP.EntraAuth.MCP;

/// <summary>
/// MCP Prompts for snippet-related prompts with authentication
/// </summary>
public class McpPrompts
{
    private readonly ISnippetService _snippetService;
    private readonly IAuthenticationStateService _authService;
    private readonly ILogger<McpPrompts> _logger;

    public McpPrompts(
        ISnippetService snippetService,
        IAuthenticationStateService authService,
        ILogger<McpPrompts> logger)
    {
        _snippetService = snippetService;
        _authService = authService;
        _logger = logger;
    }

    [Description("Generate code using snippet templates")]
    public async Task<object> GenerateCodePrompt(string language, string description, string sessionId)
    {
        try
        {
            var isAuthenticated = await _authService.IsSessionAuthenticatedAsync(sessionId);
            if (!isAuthenticated)
            {
                return new
                {
                    messages = new[]
                    {
                        new { role = "system", content = "Authentication required to access snippet templates. Please authenticate first." }
                    }
                };
            }

            var snippetNames = await _snippetService.ListSnippetsAsync();
            var relevantSnippets = new List<Snippet>();
            
            foreach (var name in snippetNames.Take(5))
            {
                var snippet = await _snippetService.GetSnippetDetailsAsync(name);
                if (snippet != null && snippet.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
                {
                    relevantSnippets.Add(snippet);
                }
            }

            var messages = new List<object>
            {
                new { role = "system", content = $"You are a code generation assistant. Generate {language} code for: {description}" }
            };

            if (relevantSnippets.Any())
            {
                var examplesContent = string.Join("\n\n", relevantSnippets.Select(s => 
                    $"Example snippet '{s.Name}':\n```{s.Language}\n{s.Content}\n```"));
                
                messages.Add(new { role = "system", content = $"Here are some relevant code examples:\n\n{examplesContent}" });
            }

            messages.Add(new { role = "user", content = $"Generate {language} code for: {description}" });

            return new { messages };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate code prompt for session {SessionId}", sessionId);
            return new
            {
                messages = new[]
                {
                    new { role = "system", content = $"Error generating prompt: {ex.Message}" }
                }
            };
        }
    }

    [Description("Authentication guidance prompt")]
    public Task<object> AuthenticationPrompt()
    {
        var messages = new[]
        {
            new { role = "system", content = "This MCP server uses Azure AD device code authentication. To access snippet tools, you need to authenticate first." },
            new { role = "assistant", content = "To get started, I'll help you authenticate:\n\n1. I'll call the StartAuthentication tool to get a device code\n2. You'll visit the provided URL and enter the code\n3. Once authenticated, you can access all snippet management features\n\nShall I start the authentication process for you?" }
        };

        return Task.FromResult<object>(new { messages });
    }
}