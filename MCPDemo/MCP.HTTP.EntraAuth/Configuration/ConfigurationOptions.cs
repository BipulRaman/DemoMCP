namespace MCP.HTTP.EntraAuth.Configuration;

/// <summary>
/// Configuration for Azure AD device code authentication
/// </summary>
public class AzureAdConfig
{
    public const string SectionName = "AzureAd";

    public string Instance { get; set; } = "https://login.microsoftonline.com/";
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Validates that all required Azure AD configuration properties are provided
    /// </summary>
    /// <returns>True if configuration is valid, false otherwise</returns>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(TenantId) &&
               !string.IsNullOrEmpty(ClientId) &&
               !string.IsNullOrEmpty(Instance);
    }

    /// <summary>
    /// Gets the OAuth 2.0 device code endpoint URL
    /// </summary>
    /// <returns>Device code endpoint URL</returns>
    public string GetDeviceCodeEndpoint()
    {
        return $"{Instance.TrimEnd('/')}/{TenantId}/oauth2/v2.0/devicecode";
    }

    /// <summary>
    /// Gets the OAuth 2.0 token endpoint URL
    /// </summary>
    /// <returns>Token endpoint URL</returns>
    public string GetTokenEndpoint()
    {
        return $"{Instance.TrimEnd('/')}/{TenantId}/oauth2/v2.0/token";
    }
}

/// <summary>
/// Configuration for MCP server
/// </summary>
public class McpServerConfig
{
    public const string SectionName = "McpServer";

    public string Name { get; set; } = "MCP.HTTP.EntraAuth";
    public string Version { get; set; } = "1.0.0";
    public string Transport { get; set; } = "http";

    /// <summary>
    /// Validates that all required MCP server configuration properties are provided
    /// </summary>
    /// <returns>True if configuration is valid, false otherwise</returns>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Name) &&
               !string.IsNullOrEmpty(Version) &&
               !string.IsNullOrEmpty(Transport);
    }
}