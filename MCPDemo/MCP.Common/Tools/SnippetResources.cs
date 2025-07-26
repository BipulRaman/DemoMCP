using System.ComponentModel;
using System.Text.Json;
using MCP.Common.Services;
using MCP.Shared;
using ModelContextProtocol.Server;

namespace MCP.Common.Tools;

[McpServerResourceType]
public class SnippetResources
{
    private readonly SnippetService snippetService;

    public SnippetResources(SnippetService snippetService)
    {
        this.snippetService = snippetService;
    }

    [McpServerResource(UriTemplate = "snippets://code/{name}", Name = "Code Snippet")]
    [Description("Get code snippet by name")]
    public async Task<string> GetSnippet(string name)
    {
        try
        {
            var content = await snippetService.GetSnippetAsync(name);
            var snippet = new Snippet
            {
                Name = name,
                Content = content,
                Language = DetectLanguage(content)
            };
            return JsonSerializer.Serialize(snippet, SnippetContext.Default.Snippet);
        }
        catch (KeyNotFoundException)
        {
            throw new Exception($"Snippet '{name}' not found");
        }
    }

    [McpServerResource(UriTemplate = "snippets://list", Name = "Snippet List", MimeType = "application/json")]
    [Description("Get list of all available code snippets")]
    public async Task<string> ListSnippets()
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

    [McpServerResource(UriTemplate = "snippets://details/{name}", Name = "Snippet Details")]
    [Description("Get detailed information about a specific snippet")]
    public async Task<string> GetSnippetDetails(string name)
    {
        try
        {
            var snippet = await snippetService.GetSnippetDetailsAsync(name);
            if (snippet == null)
            {
                throw new Exception($"Snippet '{name}' not found");
            }
            return JsonSerializer.Serialize(snippet, SnippetContext.Default.Snippet);
        }
        catch (KeyNotFoundException)
        {
            throw new Exception($"Snippet '{name}' not found");
        }
    }

    [McpServerResource(UriTemplate = "snippets://hello-world", Name = "Hello World Example", MimeType = "text/plain")]
    [Description("Get the hello world example snippet")]
    public async Task<string> HelloWorld()
    {
        try
        {
            return await snippetService.GetSnippetAsync("hello-world");
        }
        catch (KeyNotFoundException)
        {
            // Return a default hello world if not found
            return """
            public static void Main(string[] args)
            {
                Console.WriteLine("Hello, World!");
            }
            """;
        }
    }

    private static string DetectLanguage(string content)
    {
        // Simple language detection based on content patterns
        if (content.Contains("public class") || content.Contains("namespace") || content.Contains("using System"))
            return "csharp";
        if (content.Contains("function") || content.Contains("const") || content.Contains("let"))
            return "javascript";
        if (content.Contains("def ") || content.Contains("import "))
            return "python";
        if (content.Contains("public static void main") || content.Contains("System.out.println"))
            return "java";

        return "text";
    }
}
