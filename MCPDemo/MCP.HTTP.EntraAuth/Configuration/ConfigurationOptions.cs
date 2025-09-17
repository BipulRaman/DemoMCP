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
}