namespace MCP.SSE.EntraAuth.Configuration;

public class AzureAdOptions
{
    public const string SectionName = "AzureAd";

    public string Instance { get; set; } = "https://login.microsoftonline.com/";
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
}

public class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public string ServerUrl { get; set; } = "http://localhost:5116/";
    public string[] RequiredScopes { get; set; } = { "mcp:tools", "mcp:resources" };
    public string[] RequiredRoles { get; set; } = { "MCP.User" };
}

public class McpServerOptions
{
    public const string SectionName = "McpServer";

    public string Name { get; set; } = "MCP.SSE";
    public string Version { get; set; } = "1.0.0";
    public string Transport { get; set; } = "sse";
}