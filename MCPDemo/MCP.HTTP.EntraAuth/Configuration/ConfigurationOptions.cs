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