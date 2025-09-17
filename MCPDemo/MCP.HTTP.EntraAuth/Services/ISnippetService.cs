using MCP.HTTP.EntraAuth.Models;

namespace MCP.HTTP.EntraAuth.Services;

/// <summary>
/// Interface for managing code snippets using blob storage
/// </summary>
public interface ISnippetService
{
    /// <summary>
    /// Retrieves a snippet by name
    /// </summary>
    /// <param name="snippetName">The name of the snippet to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The snippet content as a string</returns>
    /// <exception cref="ArgumentException">Thrown when snippet name is invalid</exception>
    /// <exception cref="KeyNotFoundException">Thrown when snippet is not found</exception>
    Task<string> GetSnippetAsync(string snippetName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a snippet with the specified name and content
    /// </summary>
    /// <param name="snippetName">The name of the snippet</param>
    /// <param name="content">The content of the snippet</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ArgumentException">Thrown when snippet name is invalid</exception>
    /// <exception cref="ArgumentNullException">Thrown when content is null</exception>
    Task SaveSnippetAsync(string snippetName, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a snippet exists
    /// </summary>
    /// <param name="snippetName">The name of the snippet to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the snippet exists, false otherwise</returns>
    Task<bool> SnippetExistsAsync(string snippetName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a snippet by name
    /// </summary>
    /// <param name="snippetName">The name of the snippet to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="ArgumentException">Thrown when snippet name is invalid</exception>
    /// <exception cref="KeyNotFoundException">Thrown when snippet is not found</exception>
    Task DeleteSnippetAsync(string snippetName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available snippet names
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A collection of snippet names</returns>
    Task<IEnumerable<string>> ListSnippetsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a snippet including its content and detected language
    /// </summary>
    /// <param name="snippetName">The name of the snippet</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A Snippet object with details, or null if not found</returns>
    Task<Snippet?> GetSnippetDetailsAsync(string snippetName, CancellationToken cancellationToken = default);
}