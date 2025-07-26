using MCP.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MCP.Common;

/// <summary>
/// Extension methods for registering MCP Shared services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers blob service with connection string
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">The blob storage connection string</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSharedServices(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IAzBlobService>(provider =>
            new AzBlobService(connectionString, provider.GetRequiredService<ILogger<AzBlobService>>()));
        services.AddSingleton<ISnippetService, SnippetService>();

        return services;
    }
}
