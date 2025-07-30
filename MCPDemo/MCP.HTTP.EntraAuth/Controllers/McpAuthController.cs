using MCP.HTTP.EntraAuth.Configuration;
using MCP.HTTP.EntraAuth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MCP.HTTP.EntraAuth.Controllers;

/// <summary>
/// The McpAuthController handles authentication and authorization for the Model Context Protocol (MCP).
/// </summary>
[ApiController]
public class McpAuthController : ControllerBase
{
    private readonly AzureAdConfig _azureConfig;
    private readonly AuthenticationConfig _authConfig;
    private readonly McpServerConfig _mcpConfig;
    private readonly HttpClient _httpClient;
    private readonly ILogger<McpAuthController> _logger;

    /// <summary>
    /// The constructor initializes the controller with necessary configurations and services.
    /// </summary>
    /// <param name="azureConfig"></param>
    /// <param name="authConfig"></param>
    /// <param name="mcpConfig"></param>
    /// <param name="httpClient"></param>
    /// <param name="logger"></param>
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
    }

    /// <summary>
    /// Gets the health status of the MCP authentication service.
    /// </summary>
    /// <returns></returns>
    [HttpGet("/health")]
    public IActionResult Health()
    {
        return Ok(new { status = "ok", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Gets the capabilities of the MCP authentication service, including OAuth configuration.
    /// </summary>
    /// <returns></returns>
    [HttpGet("/capabilities")]
    public IActionResult GetCapabilities()
    {
        _logger.LogInformation("{Class}_{Method} : Capabilities endpoint called",
            nameof(McpAuthController), nameof(GetCapabilities));
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
                authorization_endpoint = _azureConfig.GetAuthorizationEndpoint(),
                token_endpoint = _azureConfig.GetTokenUrl(),
                scopes = _authConfig.GetFormattedScopes(_azureConfig.ClientId),
                instructions = new
                {
                    step1 = $"POST to /auth/authorize to get authorization URL (e.g. {_azureConfig.GetAuthorizationEndpoint()})",
                    step2 = $"Open authorization URL in browser and authenticate, client_id to be used is {_azureConfig.ClientId}",
                    step3 = "The callback will return JSON with the authorization code",
                    step4 = "POST authorization code to /auth/token to get access token",
                    step5 = "POST access token to /auth/sse-url to get authenticated SSE URL",
                    step6 = "Use the returned SSE URL for MCP connections"
                }
            },
            transport = _mcpConfig.Transport
        });
    }

    /// <summary>
    /// Authorizes the client by generating an authorization URL.
    /// </summary>
    /// <returns></returns>
    [HttpPost("/auth/authorize")]
    public async Task<IActionResult> GetAuthorizationUrl()
    {
        _logger.LogInformation("{Class}_{Method} : Authorization URL requested",
            nameof(McpAuthController), nameof(GetAuthorizationUrl));

        if (!_azureConfig.IsValid())
        {
            return base.StatusCode(500, new
            {
                error = "configuration_error",
                error_description = "Azure AD configuration has some issue",
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

        var authUrl = _azureConfig.GetCompleteAuthorizationUrl(
            _authConfig.GetRedirectUri(),
            _authConfig.GetFormattedScopes(_azureConfig.ClientId)
        );

        _logger.LogInformation("{Class}_{Method} : Generated auth URL: {AuthUrl}",
            nameof(McpAuthController), nameof(GetAuthorizationUrl), authUrl);

        var response = new
        {
            authorization_url = authUrl,
            redirect_uri = _authConfig.GetRedirectUri(),
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

    /// <summary>
    /// Authentication callback endpoint that handles the response from the authorization server.
    /// </summary>
    /// <param name="code"></param>
    /// <param name="state"></param>
    /// <param name="error"></param>
    /// <param name="error_description"></param>
    /// <returns></returns>
    [HttpGet("/auth/callback")]
    public IActionResult AuthCallback(string? code, string? state, string? error, string? error_description)
    {
        _logger.LogInformation("{Class}_{Method} : Auth callback received with code: {Code}, state: {State}, error: {Error}",
            nameof(McpAuthController), nameof(AuthCallback), code, state, error);
        if (!string.IsNullOrEmpty(error))
        {
            return BadRequest(new
            {
                success = false,
                error = error,
                error_description = error_description,
                message = "Authentication failed. Please try again."
            });
        }

        if (string.IsNullOrEmpty(code))
        {
            return BadRequest(new
            {
                success = false,
                error = "missing_code",
                error_description = "No authorization code received",
                message = "Authentication failed. Please try again."
            });
        }

        return Ok(new
        {
            success = true,
            code = code,
            state = state,
            message = "Authentication successful. Use the authorization code to exchange for an access token.",
            next_step = "POST the authorization code to /auth/token to get an access token"
        });
    }

    /// <summary>
    /// Authorizes the client by exchanging the authorization code for an access token.
    /// </summary>
    /// <param name="tokenRequest"></param>
    /// <returns></returns>
    [HttpPost("/auth/token")]
    public async Task<IActionResult> ExchangeToken([FromBody] Dictionary<string, object> tokenRequest)
    {
        _logger.LogInformation("{Class}_{Method} : Token exchange requested with body: {TokenRequest}",
            nameof(McpAuthController), nameof(ExchangeToken), JsonSerializer.Serialize(tokenRequest));

        if (!_azureConfig.IsValid())
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

        var tokenUrl = _azureConfig.GetTokenUrl();
        var redirectUri = _authConfig.GetRedirectUri();

        // Include the same scope that was used in the authorization request
        var scope = _authConfig.GetFormattedScopes(_azureConfig.ClientId);

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

    /// <summary>
    /// Allows clients to get a Server-Sent Events (SSE) URL for real-time updates.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("/auth/sse-url")]
    public IActionResult GetSseUrl([FromBody] Dictionary<string, object> request)
    {
        _logger.LogInformation("{Class}_{Method} : SSE URL requested with body: {Request}",
            nameof(McpAuthController), nameof(GetSseUrl), JsonSerializer.Serialize(request));

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
        var sseUrl = _authConfig.GetSseUrl(accessToken);

        var response = new
        {
            sse_url = sseUrl,
            expires_in = 3600 // Token-dependent, but provide a reasonable default
        };

        return Ok(response);
    }

    /// <summary>
    /// Registers the MCP server with Azure AD, providing necessary OAuth endpoints and client information.
    /// </summary>
    /// <returns></returns>
    [HttpPost("/register")]
    public IActionResult Register()
    {
        _logger.LogInformation("{Class}_{Method} : Registration endpoint called",
            nameof(McpAuthController), nameof(Register));

        var serverUrl = $"{Request.Scheme}://{Request.Host}";
        var requiredScopes = _authConfig.GetFormattedScopes(_azureConfig.ClientId).Split(' ');

        var registration = new
        {
            client_id = _azureConfig.ClientId,
            client_secret = "*** Contact administrator ***",
            registration_endpoint = $"{serverUrl}/register",
            authorization_endpoint = _azureConfig.GetAuthorizationEndpoint(),
            token_endpoint = _azureConfig.GetTokenUrl(),
            scope = _authConfig.GetFormattedScopes(_azureConfig.ClientId)
        };

        return Ok(registration);
    }

    /// <summary>
    /// Gets metadata for the OAuth protected resource, including supported scopes and endpoints.
    /// </summary>
    /// <returns></returns>
    [HttpGet("/.well-known/oauth-protected-resource")]
    public IActionResult GetOAuthProtectedResourceMetadata()
    {
        _logger.LogInformation("{Class}_{Method} : OAuth protected resource metadata requested",
            nameof(McpAuthController), nameof(GetOAuthProtectedResourceMetadata));

        var serverUrl = $"{Request.Scheme}://{Request.Host}";
        var authorizationServer = _azureConfig.GetOAuthServerUrl();
        var requiredScopes = _authConfig.GetFormattedScopes(_azureConfig.ClientId).Split(' ');

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

    /// <summary>
    /// Gets metadata for the OAuth authorization server, including endpoints and supported scopes.
    /// </summary>
    /// <returns></returns>
    [HttpGet("/.well-known/oauth-authorization-server")]
    public IActionResult GetOAuthAuthorizationServerMetadata()
    {
        _logger.LogInformation("{Class}_{Method} : OAuth authorization server metadata requested",
            nameof(McpAuthController), nameof(GetOAuthAuthorizationServerMetadata));

        var serverUrl = $"{Request.Scheme}://{Request.Host}";
        var server = _azureConfig.GetOAuthServerUrl();
        var requiredScopes = _authConfig.GetFormattedScopes(_azureConfig.ClientId).Split(' ');

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

    /// <summary>
    /// Connects to the Model Context Protocol (MCP) service, handling incoming requests and returning appropriate responses.
    /// </summary>
    /// <param name="mcpService"></param>
    /// <returns></returns>
    [HttpPost("/mcp-connect")]
    [Authorize]
    public async Task<IActionResult> McpConnect([FromServices] IMcpConnectService mcpService)
    {
        _logger.LogInformation("{Class}_{Method} : MCP connect endpoint called",
            nameof(McpAuthController), nameof(McpConnect));

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
