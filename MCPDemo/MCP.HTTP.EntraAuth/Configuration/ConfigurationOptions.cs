namespace MCP.HTTP.EntraAuth.Configuration;

public class AzureAdConfig
{
    public const string SectionName = "AzureAd";

    public string Instance { get; set; } = string.Empty;
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
    /// Gets the OAuth 2.0 token endpoint URL
    /// </summary>
    /// <returns>Token endpoint URL</returns>
    public string GetTokenUrl()
    {
        return $"{Instance.TrimEnd('/')}/{TenantId}/oauth2/v2.0/token";
    }

    /// <summary>
    /// Gets the OAuth 2.0 authorization endpoint URL
    /// </summary>
    /// <returns>Authorization endpoint URL</returns>
    public string GetAuthorizationEndpoint()
    {
        return $"{Instance.TrimEnd('/')}/{TenantId}/oauth2/v2.0/authorize";
    }

    /// <summary>
    /// Gets the OAuth 2.0 server base URL
    /// </summary>
    /// <returns>OAuth server base URL</returns>
    public string GetOAuthServerUrl()
    {
        return $"{Instance.TrimEnd('/')}/{TenantId}/oauth2/v2.0";
    }

    /// <summary>
    /// Gets the complete authorization URL with query parameters
    /// </summary>
    /// <param name="redirectUri">The redirect URI</param>
    /// <param name="scopes">The formatted scopes string</param>
    /// <param name="state">The state parameter (optional)</param>
    /// <returns>Complete authorization URL</returns>
    public string GetCompleteAuthorizationUrl(string redirectUri, string scopes, string? state = null)
    {
        var stateParam = !string.IsNullOrEmpty(state) ? state : Guid.NewGuid().ToString("N");

        return $"{GetAuthorizationEndpoint()}" +
               $"?client_id={ClientId}" +
               $"&response_type=code" +
               $"&redirect_uri={redirectUri}" +
               $"&scope={scopes}" +
               $"&state={stateParam}";
    }
}

public class AuthenticationConfig
{
    public const string SectionName = "Authentication";

    public string ServerUrl { get; set; } = string.Empty;
    public List<string> RequiredScopes { get; set; } = new List<string>();
    public List<string> RequiredRoles { get; set; } = new List<string>();

    /// <summary>
    /// Validates that all required authentication configuration properties are provided
    /// </summary>
    /// <returns>True if configuration is valid, false otherwise</returns>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(ServerUrl) &&
               RequiredScopes != null &&
               RequiredRoles != null;
    }

    /// <summary>
    /// Gets the OAuth 2.0 redirect URI for the callback
    /// </summary>
    /// <returns>Redirect URI</returns>
    public string GetRedirectUri()
    {
        return $"{ServerUrl.TrimEnd('/')}/auth/callback";
    }

    /// <summary>
    /// Gets the formatted scopes string for OAuth requests
    /// </summary>
    /// <param name="clientId">The Azure AD client ID to format scopes with</param>
    /// <returns>Space-separated scopes string</returns>
    public string GetFormattedScopes(string clientId)
    {
        if (RequiredScopes == null || RequiredScopes.Count == 0)
            return string.Empty;

        return string.Join(" ", RequiredScopes.Select(s => $"api://{clientId}/{s}"));
    }

    /// <summary>
    /// Gets the SSE URL with embedded access token
    /// </summary>
    /// <param name="accessToken">The access token to embed</param>
    /// <returns>SSE URL with token</returns>
    public string GetSseUrl(string accessToken)
    {
        return $"{ServerUrl.TrimEnd('/')}/?access_token={Uri.EscapeDataString(accessToken)}";
    }
}

public class McpServerConfig
{
    public const string SectionName = "McpServer";

    public string Name { get; set; } = "MCP.SSE.EntraAuth";
    public string Version { get; set; } = "1.0.0";
    public string Transport { get; set; } = "sse";

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