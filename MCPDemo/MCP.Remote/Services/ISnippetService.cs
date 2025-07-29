namespace MCP.Remote.Services;

/// <summary>
/// Interface for snippet operations
/// </summary>
public interface ISnippetService
{
    /// <summary>
    /// Retrieves a snippet by name
    /// </summary>
    /// <param name="snippetName">The name of the snippet</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The snippet content</returns>
    Task<string> GetSnippetAsync(string snippetName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a snippet with the given name and content
    /// </summary>
    /// <param name="snippetName">The name of the snippet</param>
    /// <param name="content">The snippet content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveSnippetAsync(string snippetName, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a snippet exists
    /// </summary>
    /// <param name="snippetName">The name of the snippet</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the snippet exists, false otherwise</returns>
    Task<bool> SnippetExistsAsync(string snippetName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a snippet
    /// </summary>
    /// <param name="snippetName">The name of the snippet</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteSnippetAsync(string snippetName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available snippets
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of snippet names</returns>
    Task<IEnumerable<string>> ListSnippetsAsync(CancellationToken cancellationToken = default);
}