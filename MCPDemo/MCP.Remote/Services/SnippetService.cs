using Microsoft.Extensions.Logging;

namespace MCP.Remote.Services;

/// <summary>
/// Service for managing code snippets using Azure Blob Storage
/// </summary>
public class SnippetService : ISnippetService
{
    private const string SnippetsContainerName = "snippets";
    private const string SnippetFileExtension = ".json";

    private readonly IAzBlobService _azBlobService;
    private readonly ILogger<SnippetService> _logger;

    public SnippetService(IAzBlobService azBlobService, ILogger<SnippetService> logger)
    {
        _azBlobService = azBlobService ?? throw new ArgumentNullException(nameof(azBlobService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GetSnippetAsync(string snippetName, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateSnippetName(snippetName);

            var blobName = GetBlobName(snippetName);
            var content = await _azBlobService.GetBlobAsStringAsync(SnippetsContainerName, blobName, cancellationToken);

            _logger.LogInformation("{Class}_{Method} : Successfully retrieved snippet '{SnippetName}'",
                nameof(SnippetService), nameof(GetSnippetAsync), snippetName);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Class}_{Method} : Failed to retrieve snippet '{SnippetName}': {ErrorMessage}",
                nameof(SnippetService), nameof(GetSnippetAsync), snippetName, ex.Message);
            throw;
        }
    }

    public async Task SaveSnippetAsync(string snippetName, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateSnippetName(snippetName);
            ValidateSnippetContent(content);

            var blobName = GetBlobName(snippetName);
            await _azBlobService.SaveBlobFromStringAsync(SnippetsContainerName, blobName, content, cancellationToken);

            _logger.LogInformation("{Class}_{Method} : Successfully saved snippet '{SnippetName}'",
                nameof(SnippetService), nameof(SaveSnippetAsync), snippetName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Class}_{Method} : Failed to save snippet '{SnippetName}': {ErrorMessage}",
                nameof(SnippetService), nameof(SaveSnippetAsync), snippetName, ex.Message);
            throw;
        }
    }

    public async Task<bool> SnippetExistsAsync(string snippetName, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateSnippetName(snippetName);

            var blobName = GetBlobName(snippetName);
            var exists = await _azBlobService.BlobExistsAsync(SnippetsContainerName, blobName, cancellationToken);

            _logger.LogDebug("{Class}_{Method} : Snippet '{SnippetName}' exists: {Exists}",
                nameof(SnippetService), nameof(SnippetExistsAsync), snippetName, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Class}_{Method} : Failed to check if snippet '{SnippetName}' exists: {ErrorMessage}",
                nameof(SnippetService), nameof(SnippetExistsAsync), snippetName, ex.Message);
            throw;
        }
    }

    public async Task DeleteSnippetAsync(string snippetName, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateSnippetName(snippetName);

            var blobName = GetBlobName(snippetName);
            await _azBlobService.DeleteBlobAsync(SnippetsContainerName, blobName, cancellationToken);

            _logger.LogInformation("{Class}_{Method} : Successfully deleted snippet '{SnippetName}'",
                nameof(SnippetService), nameof(DeleteSnippetAsync), snippetName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Class}_{Method} : Failed to delete snippet '{SnippetName}': {ErrorMessage}",
                nameof(SnippetService), nameof(DeleteSnippetAsync), snippetName, ex.Message);
            throw;
        }
    }

    public async Task<IEnumerable<string>> ListSnippetsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var blobNames = await _azBlobService.ListBlobsAsync(SnippetsContainerName, cancellationToken: cancellationToken);

            // Extract snippet names from blob names (remove .json extension)
            var snippetNames = blobNames
                .Where(name => name.EndsWith(SnippetFileExtension, StringComparison.OrdinalIgnoreCase))
                .Select(name => name.Substring(0, name.Length - SnippetFileExtension.Length))
                .ToList();

            _logger.LogInformation("{Class}_{Method} : Successfully listed {Count} snippets",
                nameof(SnippetService), nameof(ListSnippetsAsync), snippetNames.Count);
            return snippetNames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Class}_{Method} : Failed to list snippets: {ErrorMessage}",
                nameof(SnippetService), nameof(ListSnippetsAsync), ex.Message);
            throw;
        }
    }

    private static string GetBlobName(string snippetName)
    {
        return $"{snippetName}{SnippetFileExtension}";
    }

    private static void ValidateSnippetName(string snippetName)
    {
        if (string.IsNullOrWhiteSpace(snippetName))
        {
            throw new ArgumentException("Snippet name cannot be null or whitespace", nameof(snippetName));
        }

        // Additional validation for blob name requirements
        if (snippetName.Contains("/") || snippetName.Contains("\\"))
        {
            throw new ArgumentException("Snippet name cannot contain path separators", nameof(snippetName));
        }
    }

    private static void ValidateSnippetContent(string content)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content), "Snippet content cannot be null");
        }
    }
}
