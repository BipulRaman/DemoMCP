using MCP.HTTP.EntraAuth.Models;

namespace MCP.HTTP.EntraAuth.Services;

/// <summary>
/// Service for managing code snippets using blob storage
/// </summary>
public class SnippetService : ISnippetService
{
    private const string SnippetsContainerName = "snippets";
    private const string SnippetFileExtension = ".json";
    private readonly IAzBlobService _blobService;

    public SnippetService(IAzBlobService blobService)
    {
        _blobService = blobService ?? throw new ArgumentNullException(nameof(blobService));
    }

    public async Task<string> GetSnippetAsync(string snippetName, CancellationToken cancellationToken = default)
    {
        ValidateSnippetName(snippetName);

        var blobName = GetBlobName(snippetName);
        var content = await _blobService.GetBlobAsStringAsync(SnippetsContainerName, blobName, cancellationToken);

        if (string.IsNullOrEmpty(content))
        {
            throw new KeyNotFoundException($"Snippet '{snippetName}' not found");
        }

        return content;
    }

    public async Task SaveSnippetAsync(string snippetName, string content, CancellationToken cancellationToken = default)
    {
        ValidateSnippetName(snippetName);
        ValidateSnippetContent(content);

        var blobName = GetBlobName(snippetName);
        await _blobService.SaveBlobFromStringAsync(SnippetsContainerName, blobName, content, cancellationToken);
    }

    public async Task<bool> SnippetExistsAsync(string snippetName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(snippetName))
            return false;

        try
        {
            var blobName = GetBlobName(snippetName);
            return await _blobService.BlobExistsAsync(SnippetsContainerName, blobName, cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    public async Task DeleteSnippetAsync(string snippetName, CancellationToken cancellationToken = default)
    {
        ValidateSnippetName(snippetName);

        var blobName = GetBlobName(snippetName);

        // Check if snippet exists before trying to delete
        var exists = await _blobService.BlobExistsAsync(SnippetsContainerName, blobName, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException($"Snippet '{snippetName}' not found");
        }

        await _blobService.DeleteBlobAsync(SnippetsContainerName, blobName, cancellationToken);
    }

    public async Task<IEnumerable<string>> ListSnippetsAsync(CancellationToken cancellationToken = default)
    {
        var blobNames = await _blobService.ListBlobsAsync(SnippetsContainerName, cancellationToken: cancellationToken);

        // Extract snippet names from blob names (remove .json extension)
        var snippetNames = blobNames
            .Where(name => name.EndsWith(SnippetFileExtension, StringComparison.OrdinalIgnoreCase))
            .Select(name => name.Substring(0, name.Length - SnippetFileExtension.Length))
            .ToList();

        return snippetNames;
    }

    public async Task<Snippet?> GetSnippetDetailsAsync(string snippetName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(snippetName))
            return null;

        try
        {
            var content = await GetSnippetAsync(snippetName, cancellationToken);
            var snippet = new Snippet
            {
                Name = snippetName,
                Content = content,
                Language = DetectLanguage(content)
            };

            return snippet;
        }
        catch
        {
            return null;
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