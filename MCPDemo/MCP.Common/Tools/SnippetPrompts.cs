using System.ComponentModel;
using ModelContextProtocol.Server;

namespace MCP.Common.Tools;

[McpServerPromptType]
public class SnippetPrompts
{
    [McpServerPrompt, Description("Get a list of all code snippets.")]
    public static string GetSnippetsPrompt()
    {
        return "Please provide a list of all available code snippets and organize them by name in a table format with their programming language.";
    }

    [McpServerPrompt, Description("Get a specific code snippet by name.")]
    public static string GetSnippetPrompt([Description("The name of the snippet to retrieve")] string name)
    {
        return $"Please provide the code snippet named '{name}' with proper formatting and syntax highlighting.";
    }

    [McpServerPrompt, Description("Create a new code snippet.")]
    public static string CreateSnippetPrompt([Description("The name for the new snippet")] string name, [Description("The programming language (e.g., csharp, javascript, python)")] string language)
    {
        return $"Please create a new {language} code snippet named '{name}'. Provide a useful example with proper comments and best practices.";
    }

    [McpServerPrompt, Description("Explain and document a code snippet.")]
    public static string ExplainSnippetPrompt([Description("The name of the snippet to explain")] string name, [Description("The level of detail (e.g., beginner, intermediate, advanced)")] string level = "intermediate")
    {
        return $"Please explain the code snippet '{name}' in detail at a {level} level. Include what it does, how it works, and provide usage examples.";
    }

    [McpServerPrompt, Description("Generate variations of a code snippet.")]
    public static string VariateSnippetPrompt([Description("The name of the snippet to create variations for")] string name, [Description("The type of variation (e.g., async, generic, optimized, simplified)")] string variationType)
    {
        return $"Based on the code snippet '{name}', please create a {variationType} variation. Explain the differences and when to use each version.";
    }

    [McpServerPrompt, Description("Convert a code snippet to a different programming language.")]
    public static string ConvertSnippetPrompt([Description("The name of the snippet to convert")] string name, [Description("The target programming language")] string targetLanguage)
    {
        return $"Please convert the code snippet '{name}' to {targetLanguage}. Maintain the same functionality while following {targetLanguage} best practices and idioms.";
    }

    [McpServerPrompt, Description("Generate unit tests for a code snippet.")]
    public static string TestSnippetPrompt([Description("The name of the snippet to test")] string name, [Description("The testing framework to use (e.g., xunit, nunit, jest, pytest)")] string framework = "xunit")
    {
        return $"Please generate comprehensive unit tests for the code snippet '{name}' using the {framework} testing framework. Include positive, negative, and edge case scenarios.";
    }

    [McpServerPrompt, Description("Review and suggest improvements for a code snippet.")]
    public static string ReviewSnippetPrompt([Description("The name of the snippet to review")] string name)
    {
        return $"Please review the code snippet '{name}' and suggest improvements. Focus on performance, readability, security, and best practices.";
    }
}
