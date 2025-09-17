namespace MCP.HTTP.EntraAuth.Models;

/// <summary>
/// Represents a code snippet with metadata
/// </summary>
public class Snippet
{
    /// <summary>
    /// The name/identifier of the snippet
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The content/code of the snippet
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The detected or specified programming language
    /// </summary>
    public string Language { get; set; } = string.Empty;
}