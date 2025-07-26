namespace MCP.Common.Services;

/// <summary>
/// Interface for Azure Blob Storage operations
/// </summary>
public interface IAzBlobService
{
    /// <summary>
    /// Retrieves content from a blob as a string
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <param name="blobName">The blob name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The blob content as a string</returns>
    Task<string> GetBlobAsStringAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves content from a blob as a stream
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <param name="blobName">The blob name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The blob content as a stream</returns>
    Task<Stream> GetBlobAsStreamAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves content to a blob from a string
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <param name="blobName">The blob name</param>
    /// <param name="content">The content to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveBlobFromStringAsync(string containerName, string blobName, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves content to a blob from a stream
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <param name="blobName">The blob name</param>
    /// <param name="content">The content stream to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveBlobFromStreamAsync(string containerName, string blobName, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a blob exists
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <param name="blobName">The blob name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the blob exists, false otherwise</returns>
    Task<bool> BlobExistsAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a blob
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <param name="blobName">The blob name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all blobs in a container
    /// </summary>
    /// <param name="containerName">The container name</param>
    /// <param name="prefix">Optional prefix to filter blobs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A list of blob names</returns>
    Task<IEnumerable<string>> ListBlobsAsync(string containerName, string? prefix = null, CancellationToken cancellationToken = default);
}
