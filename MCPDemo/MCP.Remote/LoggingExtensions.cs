using Microsoft.Extensions.Logging;

namespace MCP.Remote;

/// <summary>
/// Utility class for standardized logging throughout the MCP.Remote project
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Logs the start of a tool operation
    /// </summary>
    public static void LogToolOperationStart(this ILogger logger, string toolName, string operation, object? parameters = null)
    {
        logger.LogInformation("Starting {ToolName}.{Operation} with parameters: {@Parameters}", 
            toolName, operation, parameters);
    }

    /// <summary>
    /// Logs the completion of a tool operation
    /// </summary>
    public static void LogToolOperationComplete(this ILogger logger, string toolName, string operation, long elapsedMs = 0)
    {
        logger.LogInformation("Completed {ToolName}.{Operation} in {ElapsedMs}ms", 
            toolName, operation, elapsedMs);
    }

    /// <summary>
    /// Logs a tool operation error
    /// </summary>
    public static void LogToolOperationError(this ILogger logger, string toolName, string operation, Exception ex)
    {
        logger.LogError(ex, "Error in {ToolName}.{Operation}: {ErrorMessage}", 
            toolName, operation, ex.Message);
    }

    /// <summary>
    /// Logs blob storage operations
    /// </summary>
    public static void LogBlobOperation(this ILogger logger, string operation, string blobPath, long? contentLength = null)
    {
        if (contentLength.HasValue)
        {
            logger.LogInformation("Blob {Operation}: {BlobPath}, Size: {ContentLength} bytes", 
                operation, blobPath, contentLength.Value);
        }
        else
        {
            logger.LogInformation("Blob {Operation}: {BlobPath}", operation, blobPath);
        }
    }
}