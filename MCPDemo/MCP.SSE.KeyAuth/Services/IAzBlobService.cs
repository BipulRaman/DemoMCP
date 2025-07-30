namespace MCP.SSE.KeyAuth.Services;

/// <summary>
/// Interface for Azure Blob Storage operations
/// </summary>
public interface IAzBlobService
{
    /// <summary>
    /// Retrieves blob content as a string
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="blobName">Blob name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Blob content as string</returns>
    Task<string> GetBlobAsStringAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves blob content as a stream
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="blobName">Blob name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Blob content as stream</returns>
    Task<Stream> GetBlobAsStreamAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves string content to blob
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="blobName">Blob name</param>
    /// <param name="content">Content to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveBlobFromStringAsync(string containerName, string blobName, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves stream content to blob
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="blobName">Blob name</param>
    /// <param name="content">Content to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveBlobFromStreamAsync(string containerName, string blobName, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if blob exists
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="blobName">Blob name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if blob exists, false otherwise</returns>
    Task<bool> BlobExistsAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a blob
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="blobName">Blob name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all blobs in a container
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="prefix">Optional prefix filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of blob names</returns>
    Task<IEnumerable<string>> ListBlobsAsync(string containerName, string? prefix = null, CancellationToken cancellationToken = default);
}