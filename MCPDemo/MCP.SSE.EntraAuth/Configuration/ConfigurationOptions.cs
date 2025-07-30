namespace MCP.SSE.EntraAuth.Configuration;

public class AzureAdConfig
{
    public const string SectionName = "AzureAd";

    public string Instance { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
}

public class AuthenticationConfig
{
    public const string SectionName = "Authentication";

    public string ServerUrl { get; set; } = string.Empty;
    public List<string> RequiredScopes { get; set; } = new List<string>();
    public List<string> RequiredRoles { get; set; } = new List<string>();
}

public class McpServerConfig
{
    public const string SectionName = "McpServer";

    public string Name { get; set; } = "MCP.SSE.EntraAuth";
    public string Version { get; set; } = "1.0.0";
    public string Transport { get; set; } = "sse";
}