namespace MCP.SSE.KeyAuth.Tools;

internal sealed class ToolsInformation
{
    // Snippet Tools
    public const string SaveSnippetToolName = "save_snippet";
    public const string SaveSnippetToolDescription = "Saves a code snippet into your snippet collection.";
    public const string GetSnippetToolName = "get_snippets";
    public const string GetSnippetToolDescription = "Gets code snippets from your snippet collection.";
    public const string ListSnippetsToolName = "list_snippets";
    public const string ListSnippetsToolDescription = "List all available code snippets.";
    public const string DeleteSnippetToolName = "delete_snippet";
    public const string DeleteSnippetToolDescription = "Delete a code snippet by name.";

    // Snippet Properties
    public const string SnippetNamePropertyName = "snippetname";
    public const string SnippetPropertyName = "snippet";
    public const string SnippetNamePropertyDescription = "The name of the snippet.";
    public const string SnippetPropertyDescription = "The code snippet.";
    public const string PropertyType = "string";

    // Hello Tool
    public const string HelloToolName = "hello";
    public const string HelloToolDescription = "Simple hello world MCP Tool that responses with a hello message.";
}
