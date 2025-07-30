using MCP.SSE.EntraAuth.Configuration;
using MCP.SSE.EntraAuth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MCP.SSE.EntraAuth.Controllers;

[ApiController]
public class McpAuthController : ControllerBase
{
    private readonly AzureAdConfig _azureConfig;
    private readonly AuthenticationConfig _authConfig;
    private readonly McpServerConfig _mcpConfig;
    private readonly HttpClient _httpClient;
    private readonly ILogger<McpAuthController> _logger;
    private readonly string _authUrl;
    private readonly string _tokenUrl;
    private readonly string _redirectUrl;
    private readonly string _requiredScopes;

    public McpAuthController(
        IOptions<AzureAdConfig> azureConfig,
        IOptions<AuthenticationConfig> authConfig,
        IOptions<McpServerConfig> mcpConfig,
        HttpClient httpClient,
        ILogger<McpAuthController> logger)
    {
        _azureConfig = azureConfig.Value;
        _authConfig = authConfig.Value;
        _mcpConfig = mcpConfig.Value;
        _httpClient = httpClient;
        _logger = logger;

        _requiredScopes = string.Join(" ", _authConfig.RequiredScopes.Select(s => $"api://{_azureConfig.ClientId}/{s}"));
        _redirectUrl = $"{_authConfig.ServerUrl.TrimEnd('/')}/auth/callback";
        _tokenUrl = $"{_azureConfig.Instance.TrimEnd('/')}/{_azureConfig.TenantId}/oauth2/v2.0/token";
        _authUrl = $"{_azureConfig.Instance.TrimEnd('/')}/{_azureConfig.TenantId}/oauth2/v2.0/authorize" + 
            $"?client_id={_azureConfig.ClientId}" +
                   $"&response_type=code" +
                   $"&redirect_uri={_redirectUrl}" + // Placeholder, will be set in GetAuthorizationUrl
                   $"&scope={_requiredScopes}" + // Placeholder, will be set in GetAuthorizationUrl
                   $"&state={Guid.NewGuid().ToString("N")}";  // Placeholder, will be set in GetAuthorizationUrl
    }

    [HttpGet("/health")]
    public IActionResult Health()
    {
        return Ok(new { status = "ok", timestamp = DateTime.UtcNow });
    }

    [HttpGet("/capabilities")]
    public IActionResult GetCapabilities()
    {
        _logger.LogInformation("Capabilities endpoint called");
        return Ok(new
        {
            name = _mcpConfig.Name,
            version = _mcpConfig.Version,
            authentication = new
            {
                required = true,
                type = "oauth2",
                flow = "authorization_code",
                client_id = _azureConfig.ClientId,
                authorization_endpoint = _authUrl,
                token_endpoint = _tokenUrl,
                scopes = _requiredScopes,
                instructions = new
                {
                    step1 = $"POST to /auth/authorize to get authorization URL (e.g. {_authUrl})",
                    step2 = $"Open authorization URL in browser and authenticate, client_id to be used is {_azureConfig.ClientId}",
                    step3 = "Copy authorization code from callback page",
                    step4 = "POST authorization code to /auth/token to get access token",
                    step5 = "POST access token to /auth/sse-url to get authenticated SSE URL",
                    step6 = "Use the returned SSE URL for MCP connections"
                }
            },
            transport = _mcpConfig.Transport
        });
    }

    [HttpPost("/auth/authorize")]
    public async Task<IActionResult> GetAuthorizationUrl()
    {
        _logger.LogInformation("Authorization URL requested");

        if (string.IsNullOrEmpty(_azureConfig.TenantId) || string.IsNullOrEmpty(_azureConfig.ClientId))
        {
            return base.StatusCode(500, new
            {
                error = "configuration_error",
                error_description = "Azure AD configuration is missing",
                debug_info = new
                {
                    tenant_id = _azureConfig.TenantId ?? "null",
                    client_id = _azureConfig.ClientId ?? "null",
                    server_url = _authConfig.ServerUrl ?? "null"
                }
            });
        }

        // Read the request body for optional parameters
        var body = await new StreamReader(Request.Body).ReadToEndAsync();
        var authRequest = string.IsNullOrEmpty(body) ? new Dictionary<string, object>() :
            JsonSerializer.Deserialize<Dictionary<string, object>>(body) ?? new Dictionary<string, object>();

        _logger.LogInformation("Generated auth URL: {AuthUrl}", _authUrl);

        var response = new
        {
            authorization_url = _authUrl,
            redirect_uri = _redirectUrl,
            expires_in = 600, // URL valid for 10 minutes
            debug_info = new
            {
                client_id = _azureConfig.ClientId,
                tenant_id = _azureConfig.TenantId,
                instance = _azureConfig.Instance,
                server_url = _authConfig.ServerUrl
            }
        };

        return Ok(response);
    }

    [HttpGet("/auth/callback")]
    public IActionResult AuthCallback(string? code, string? state, string? error, string? error_description)
    {
        if (!string.IsNullOrEmpty(error))
        {
            var html = $@"
                <html><body>
                    <h2>Authentication Error</h2>
                    <p>Error: {error}</p>
                    <p>Description: {error_description}</p>
                    <p>Please close this window and try again.</p>
                </body></html>";
            return Content(html, "text/html");
        }

        if (string.IsNullOrEmpty(code))
        {
            var html = @"
                <html><body>
                    <h2>Authentication Error</h2>
                    <p>No authorization code received.</p>
                    <p>Please close this window and try again.</p>
                </body></html>";
            return Content(html, "text/html");
        }

        // Display success page with code for user to copy
        var successHtml = $@"
            <html><body>
                <h2>Authentication Successful</h2>
                <p>Copy the authorization code below and paste it into your application:</p>
                <pre style='background: #f0f0f0; padding: 10px; border-radius: 4px;'>{code}</pre>
                <p>State: {state}</p>
                <p>You can close this window now.</p>
            </body></html>";
        return Content(successHtml, "text/html");
    }

    [HttpPost("/auth/token")]
    public async Task<IActionResult> ExchangeToken([FromBody] Dictionary<string, object> tokenRequest)
    {
        if (string.IsNullOrEmpty(_azureConfig.TenantId) || string.IsNullOrEmpty(_azureConfig.ClientId))
        {
            return StatusCode(500, new
            {
                error = "configuration_error",
                error_description = "Azure AD configuration is missing"
            });
        }

        if (tokenRequest == null || !tokenRequest.TryGetValue("code", out var codeObj))
        {
            return BadRequest(new
            {
                error = "invalid_request",
                error_description = "authorization code is required"
            });
        }

        var code = codeObj?.ToString();
        if (string.IsNullOrEmpty(code))
        {
            return BadRequest(new
            {
                error = "invalid_request",
                error_description = "authorization code cannot be empty"
            });
        }

        var tokenUrl = $"{_azureConfig.Instance.TrimEnd('/')}/{_azureConfig.TenantId}/oauth2/v2.0/token";
        var redirectUri = $"{_authConfig.ServerUrl.TrimEnd('/')}/auth/callback";

        // Include the same scope that was used in the authorization request
        var requiredScopes = _authConfig.RequiredScopes.Select(s => $"api://{_azureConfig.ClientId}/{s}");
        var scope = string.Join(" ", requiredScopes);

        var tokenBody = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", _azureConfig.ClientId),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", redirectUri),
            new KeyValuePair<string, string>("scope", scope)
        });

        try
        {
            var response = await _httpClient.PostAsync(tokenUrl, tokenBody);
            var responseContent = await response.Content.ReadAsStringAsync();

            Response.StatusCode = (int)response.StatusCode;
            Response.ContentType = "application/json";
            return Content(responseContent, "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "token_request_failed",
                error_description = ex.Message
            });
        }
    }

    [HttpPost("/auth/sse-url")]
    public IActionResult GetSseUrl([FromBody] Dictionary<string, object> request)
    {
        if (request == null || !request.TryGetValue("access_token", out var tokenObj))
        {
            return BadRequest(new
            {
                error = "invalid_request",
                error_description = "access_token is required"
            });
        }

        var accessToken = tokenObj?.ToString();
        if (string.IsNullOrEmpty(accessToken))
        {
            return BadRequest(new
            {
                error = "invalid_request",
                error_description = "access_token cannot be empty"
            });
        }

        // Generate SSE URL with embedded token
        var sseUrl = $"{_authConfig.ServerUrl.TrimEnd('/')}/?access_token={Uri.EscapeDataString(accessToken)}";

        var response = new
        {
            sse_url = sseUrl,
            expires_in = 3600 // Token-dependent, but provide a reasonable default
        };

        return Ok(response);
    }

    [HttpPost("/register")]
    public IActionResult Register()
    {
        var serverUrl = $"{Request.Scheme}://{Request.Host}";
        var requiredScopes = _authConfig.RequiredScopes.Select(s => $"api://{_azureConfig.ClientId}/{s}").ToArray();

        var registration = new
        {
            client_id = _azureConfig.ClientId,
            client_secret = "*** Contact administrator ***",
            registration_endpoint = $"{serverUrl}/register",
            authorization_endpoint = _authUrl,
            token_endpoint = _tokenUrl,
            scope = _requiredScopes
        };

        return Ok(registration);
    }

    [HttpGet("/.well-known/oauth-protected-resource")]
    public IActionResult GetOAuthProtectedResourceMetadata()
    {
        var serverUrl = $"{Request.Scheme}://{Request.Host}";
        var authorizationServer = $"{_azureConfig.Instance}{_azureConfig.TenantId}/oauth2/v2.0";
        var requiredScopes = _authConfig.RequiredScopes.Select(s => $"api://{_azureConfig.ClientId}/{s}").ToArray();

        var metadata = new
        {
            resource = serverUrl,
            authorization_servers = new[] { authorizationServer },
            scopes_supported = requiredScopes,
            resource_documentation = "https://github.com/modelcontextprotocol/csharp-sdk",
            token_endpoint_auth_methods_supported = new[] { "client_secret_post", "private_key_jwt", "client_secret_basic" },
            token_endpoint_auth_signing_alg_values_supported = new[] { "RS256", "ES256" },
            bearer_methods_supported = new[] { "header", "body" }
        };

        return Ok(metadata);
    }

    [HttpGet("/.well-known/oauth-authorization-server")]
    public IActionResult GetOAuthAuthorizationServerMetadata()
    {
        var serverUrl = $"{Request.Scheme}://{Request.Host}";
        var server = $"{_azureConfig.Instance}{_azureConfig.TenantId}/oauth2/v2.0";
        var requiredScopes = _authConfig.RequiredScopes.Select(s => $"api://{_azureConfig.ClientId}/{s}").ToArray();

        var metadata = new
        {
            issuer = server,
            authorization_endpoint = $"{server}/authorize",
            token_endpoint = $"{server}/token",
            scopes_supported = requiredScopes,
            response_types_supported = new[] { "code", "token" },
            grant_types_supported = new[] { "authorization_code", "implicit", "client_credentials" },
            token_endpoint_auth_methods_supported = new[] { "client_secret_post", "private_key_jwt", "client_secret_basic" },
            registration_endpoint = $"{serverUrl}/register",
        };

        return Ok(metadata);
    }

    [HttpPost("/mcp-connect")]
    [Authorize]
    public async Task<IActionResult> McpConnect([FromServices] CustomMcpService mcpService)
    {
        try
        {
            var body = await new StreamReader(Request.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
            {
                return BadRequest(new
                {
                    jsonrpc = "2.0",
                    error = new
                    {
                        code = -32600,
                        message = "Invalid Request"
                    },
                    id = (object?)null
                });
            }

            using var jsonDoc = JsonDocument.Parse(body);
            var response = await mcpService.HandleMcpRequestAsync(jsonDoc, HttpContext);

            return Ok(response);
        }
        catch (JsonException)
        {
            return BadRequest(new
            {
                jsonrpc = "2.0",
                error = new
                {
                    code = -32700,
                    message = "Parse error"
                },
                id = (object?)null
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                jsonrpc = "2.0",
                error = new
                {
                    code = -32603,
                    message = "Internal error",
                    data = ex.Message
                },
                id = (object?)null
            });
        }
    }
}
