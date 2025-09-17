using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace MCP.HTTP.EntraAuth.Services;

/// <summary>
/// Implementation of Azure Blob Storage operations
/// </summary>
public class AzBlobService : IAzBlobService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzBlobService> _logger;

    public AzBlobService(string connectionString, ILogger<AzBlobService> logger)
    {
        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public AzBlobService(BlobServiceClient blobServiceClient, ILogger<AzBlobService> logger)
    {
        _blobServiceClient = blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GetBlobAsStringAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning("{Class}_{Method} : Blob '{BlobName}' not found in container '{ContainerName}'",
                    nameof(AzBlobService), nameof(GetBlobAsStringAsync), blobName, containerName);
                return string.Empty;
            }

            var response = await blobClient.DownloadContentAsync(cancellationToken);
            return response.Value.Content.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Class}_{Method} : Failed to get blob '{BlobName}' from container '{ContainerName}': {ErrorMessage}",
                nameof(AzBlobService), nameof(GetBlobAsStringAsync), blobName, containerName, ex.Message);
            throw;
        }
    }

    public async Task<Stream> GetBlobAsStreamAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning("{Class}_{Method} : Blob '{BlobName}' not found in container '{ContainerName}'",
                    nameof(AzBlobService), nameof(GetBlobAsStreamAsync), blobName, containerName);
                return Stream.Null;
            }

            var response = await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Class}_{Method} : Failed to get blob stream '{BlobName}' from container '{ContainerName}': {ErrorMessage}",
                nameof(AzBlobService), nameof(GetBlobAsStreamAsync), blobName, containerName, ex.Message);
            throw;
        }
    }

    public async Task SaveBlobFromStringAsync(string containerName, string blobName, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(blobName);
            var contentBytes = Encoding.UTF8.GetBytes(content);
            using var stream = new MemoryStream(contentBytes);
            await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);

            _logger.LogInformation("{Class}_{Method} : Successfully saved blob '{BlobName}' to container '{ContainerName}'",
                nameof(AzBlobService), nameof(SaveBlobFromStringAsync), blobName, containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Class}_{Method} : Failed to save blob '{BlobName}' to container '{ContainerName}': {ErrorMessage}",
                nameof(AzBlobService), nameof(SaveBlobFromStringAsync), blobName, containerName, ex.Message);
            throw;
        }
    }

    public async Task SaveBlobFromStreamAsync(string containerName, string blobName, Stream content, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(content, overwrite: true, cancellationToken);

            _logger.LogInformation("{Class}_{Method} : Successfully saved blob '{BlobName}' to container '{ContainerName}'",
                nameof(AzBlobService), nameof(SaveBlobFromStreamAsync), blobName, containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Class}_{Method} : Failed to save blob '{BlobName}' to container '{ContainerName}': {ErrorMessage}",
                nameof(AzBlobService), nameof(SaveBlobFromStreamAsync), blobName, containerName, ex.Message);
            throw;
        }
    }

    public async Task<bool> BlobExistsAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            var response = await blobClient.ExistsAsync(cancellationToken);
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Class}_{Method} : Failed to check if blob '{BlobName}' exists in container '{ContainerName}': {ErrorMessage}",
                nameof(AzBlobService), nameof(BlobExistsAsync), blobName, containerName, ex.Message);
            throw;
        }
    }

    public async Task DeleteBlobAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("{Class}_{Method} : Successfully deleted blob '{BlobName}' from container '{ContainerName}'",
                nameof(AzBlobService), nameof(DeleteBlobAsync), blobName, containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Class}_{Method} : Failed to delete blob '{BlobName}' from container '{ContainerName}': {ErrorMessage}",
                nameof(AzBlobService), nameof(DeleteBlobAsync), blobName, containerName, ex.Message);
            throw;
        }
    }

    public async Task<IEnumerable<string>> ListBlobsAsync(string containerName, string? prefix = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            if (!await containerClient.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning("{Class}_{Method} : Container '{ContainerName}' does not exist",
                    nameof(AzBlobService), nameof(ListBlobsAsync), containerName);
                return Enumerable.Empty<string>();
            }

            var blobNames = new List<string>();
            await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken))
            {
                blobNames.Add(blobItem.Name);
            }

            return blobNames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Class}_{Method} : Failed to list blobs in container '{ContainerName}' with prefix '{Prefix}': {ErrorMessage}",
                nameof(AzBlobService), nameof(ListBlobsAsync), containerName, prefix, ex.Message);
            throw;
        }
    }
}