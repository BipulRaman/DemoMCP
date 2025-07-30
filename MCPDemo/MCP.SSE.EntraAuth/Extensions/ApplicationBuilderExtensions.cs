using MCP.SSE.EntraAuth.Configuration;
using MCP.SSE.EntraAuth.Middleware;
using MCP.SSE.EntraAuth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MCP.SSE.EntraAuth.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseMcpPipeline(this IApplicationBuilder app)
    {
        // Add CORS support for VS Code MCP extension
        app.UseCors(policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());

        // Enable authentication and authorization middleware
        app.UseAuthentication();
        app.UseAuthorization();

        // Use the custom MCP authentication middleware
        app.UseMcpAuthentication();

        return app;
    }

    public static IApplicationBuilder MapMcpEndpoints(this IApplicationBuilder app)
    {
        var appBuilder = (WebApplication)app;

        // Add health check endpoint for MCP clients
        appBuilder.MapGet("/health", () => new { status = "ok", timestamp = DateTime.UtcNow });

        // Add server capabilities endpoint (public, no auth required)
        appBuilder.MapGet("/capabilities", (HttpContext context, IOptions<McpServerOptions> mcpOptions, IOptions<AzureAdOptions> azureOptions, IOptions<AuthenticationOptions> authOptions) =>
        {
            var azure = azureOptions.Value;
            var auth = authOptions.Value;
            
            // Debug: Log configuration values
            var logger = context.RequestServices.GetService<ILogger<Program>>();
            logger?.LogInformation("Azure AD Config - ClientId: {ClientId}, TenantId: {TenantId}, Instance: {Instance}", 
                azure.ClientId, azure.TenantId, azure.Instance);
            
            var requiredScopes = auth.RequiredScopes.Select(s => $"api://{azure.ClientId}/{s}").ToArray();

            return new
            {
                name = mcpOptions.Value.Name,
                version = mcpOptions.Value.Version,
                authentication = new
                {
                    required = true,
                    type = "oauth2",
                    flow = "authorization_code",
                    authorization_endpoint = $"{azure.Instance.TrimEnd('/')}/{azure.TenantId}/oauth2/v2.0/authorize",
                    token_endpoint = $"{azure.Instance.TrimEnd('/')}/{azure.TenantId}/oauth2/v2.0/token",
                    scopes = requiredScopes,
                    instructions = new
                    {
                        step1 = "POST to /auth/authorize to get authorization URL",
                        step2 = "Open authorization URL in browser and authenticate",
                        step3 = "Copy authorization code from callback page",
                        step4 = "POST authorization code to /auth/token to get access token",
                        step5 = "POST access token to /auth/sse-url to get authenticated SSE URL",
                        step6 = "Use the returned SSE URL for MCP connections"
                    }
                },
                transport = mcpOptions.Value.Transport
            };
        });

        // Add authorization URL generation endpoint
        appBuilder.MapPost("/auth/authorize", async (HttpContext context, IOptions<AzureAdOptions> azureOptions, IOptions<AuthenticationOptions> authOptions) =>
        {
            var azure = azureOptions.Value;
            var auth = authOptions.Value;

            // Debug: Log configuration values
            var logger = context.RequestServices.GetService<ILogger<Program>>();
            logger?.LogInformation("Auth endpoint - ClientId: {ClientId}, TenantId: {TenantId}, ServerUrl: {ServerUrl}", 
                azure.ClientId, azure.TenantId, auth.ServerUrl);

            if (string.IsNullOrEmpty(azure.TenantId) || string.IsNullOrEmpty(azure.ClientId))
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "configuration_error",
                    error_description = "Azure AD configuration is missing",
                    debug_info = new
                    {
                        tenant_id = azure.TenantId ?? "null",
                        client_id = azure.ClientId ?? "null",
                        server_url = auth.ServerUrl ?? "null"
                    }
                }));
                return;
            }

            // Read the request body for optional parameters
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            var authRequest = string.IsNullOrEmpty(body) ? new Dictionary<string, object>() :
                JsonSerializer.Deserialize<Dictionary<string, object>>(body) ?? new Dictionary<string, object>();

            // Generate state parameter for CSRF protection
            var state = Guid.NewGuid().ToString("N");

            // Build redirect URI - use server URL + callback path
            var redirectUri = $"{auth.ServerUrl.TrimEnd('/')}/auth/callback";

            // Build required scopes
            var requiredScopes = auth.RequiredScopes.Select(s => $"api://{azure.ClientId}/{s}");
            var scope = string.Join(" ", requiredScopes);

            // Build authorization URL with proper URL formation
            var azureInstance = azure.Instance.TrimEnd('/');
            var authUrl = $"{azureInstance}/{azure.TenantId}/oauth2/v2.0/authorize" +
                         $"?client_id={Uri.EscapeDataString(azure.ClientId)}" +
                         $"&response_type=code" +
                         $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                         $"&scope={Uri.EscapeDataString(scope)}" +
                         $"&state={Uri.EscapeDataString(state)}" +
                         $"&response_mode=query";

            logger?.LogInformation("Generated auth URL: {AuthUrl}", authUrl);

            var response = new
            {
                authorization_url = authUrl,
                state = state,
                redirect_uri = redirectUri,
                expires_in = 600, // URL valid for 10 minutes
                debug_info = new
                {
                    client_id = azure.ClientId,
                    tenant_id = azure.TenantId,
                    instance = azure.Instance,
                    server_url = auth.ServerUrl
                }
            };

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        });

        // Add authorization callback endpoint
        appBuilder.MapGet("/auth/callback", async (HttpContext context, IOptions<AzureAdOptions> azureOptions, IOptions<AuthenticationOptions> authOptions) =>
        {
            var code = context.Request.Query["code"];
            var state = context.Request.Query["state"];
            var error = context.Request.Query["error"];

            if (!string.IsNullOrEmpty(error))
            {
                var errorDescription = context.Request.Query["error_description"];
                await context.Response.WriteAsync($@"
                    <html><body>
                        <h2>Authentication Error</h2>
                        <p>Error: {error}</p>
                        <p>Description: {errorDescription}</p>
                        <p>Please close this window and try again.</p>
                    </body></html>");
                return;
            }

            if (string.IsNullOrEmpty(code))
            {
                await context.Response.WriteAsync(@"
                    <html><body>
                        <h2>Authentication Error</h2>
                        <p>No authorization code received.</p>
                        <p>Please close this window and try again.</p>
                    </body></html>");
                return;
            }

            // Display success page with code for user to copy
            await context.Response.WriteAsync($@"
                <html><body>
                    <h2>Authentication Successful</h2>
                    <p>Copy the authorization code below and paste it into your application:</p>
                    <pre style='background: #f0f0f0; padding: 10px; border-radius: 4px;'>{code}</pre>
                    <p>State: {state}</p>
                    <p>You can close this window now.</p>
                </body></html>");
        });

        // Add authorization code exchange endpoint
        appBuilder.MapPost("/auth/token", async (HttpContext context, IOptions<AzureAdOptions> azureOptions, IOptions<AuthenticationOptions> authOptions) =>
        {
            var httpClient = context.RequestServices.GetRequiredService<HttpClient>();
            var azure = azureOptions.Value;
            var auth = authOptions.Value;

            if (string.IsNullOrEmpty(azure.TenantId) || string.IsNullOrEmpty(azure.ClientId))
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "configuration_error",
                    error_description = "Azure AD configuration is missing"
                }));
                return;
            }

            // Read the request body
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            var tokenRequest = JsonSerializer.Deserialize<Dictionary<string, object>>(body);

            if (tokenRequest == null || !tokenRequest.TryGetValue("code", out var codeObj))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "invalid_request",
                    error_description = "authorization code is required"
                }));
                return;
            }

            var code = codeObj?.ToString();
            if (string.IsNullOrEmpty(code))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "invalid_request",
                    error_description = "authorization code cannot be empty"
                }));
                return;
            }

            var tokenUrl = $"{azure.Instance.TrimEnd('/')}/{azure.TenantId}/oauth2/v2.0/token";
            var redirectUri = $"{auth.ServerUrl.TrimEnd('/')}/auth/callback";

            // Include the same scope that was used in the authorization request
            var requiredScopes = auth.RequiredScopes.Select(s => $"api://{azure.ClientId}/{s}");
            var scope = string.Join(" ", requiredScopes);

            var tokenBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("client_id", azure.ClientId),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("scope", scope)
            });

            try
            {
                var response = await httpClient.PostAsync(tokenUrl, tokenBody);
                var responseContent = await response.Content.ReadAsStringAsync();

                context.Response.StatusCode = (int)response.StatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(responseContent);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "token_request_failed",
                    error_description = ex.Message
                }));
            }
        });

        // Add OAuth protected resource metadata endpoint
        appBuilder.Map("/.well-known/oauth-protected-resource", (HttpContext context, IOptions<AzureAdOptions> azureOptions, IOptions<AuthenticationOptions> authOptions) =>
        {
            var azure = azureOptions.Value;
            var auth = authOptions.Value;
            var serverUrl = context.Request.Scheme + "://" + context.Request.Host;
            var authorizationServer = $"{azure.Instance}{azure.TenantId}/oauth2/v2.0";
            var requiredScopes = auth.RequiredScopes.Select(s => $"api://{azure.ClientId}/{s}").ToArray();

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
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(JsonSerializer.Serialize(metadata));
        });

        // Add OAuth authorization server metadata endpoint
        appBuilder.Map("/.well-known/oauth-authorization-server", (HttpContext context, IOptions<AzureAdOptions> azureOptions, IOptions<AuthenticationOptions> authOptions) =>
        {
            var azure = azureOptions.Value;
            var auth = authOptions.Value;
            var serverUrl = context.Request.Scheme + "://" + context.Request.Host;
            var server = $"{azure.Instance}{azure.TenantId}/oauth2/v2.0";
            var requiredScopes = auth.RequiredScopes.Select(s => $"api://{azure.ClientId}/{s}").ToArray();

            var metadata = new
            {
                issuer = server,
                authorization_endpoint = server + "/authorize",
                token_endpoint = server + "/token",
                scopes_supported = requiredScopes,
                response_types_supported = new[] { "code", "token" },
                grant_types_supported = new[] { "authorization_code", "implicit", "client_credentials" },
                token_endpoint_auth_methods_supported = new[] { "client_secret_post", "private_key_jwt", "client_secret_basic" },
                registration_endpoint = serverUrl + "/register",
            };
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(JsonSerializer.Serialize(metadata));
        });

        // Add dynamic client registration endpoint
        appBuilder.MapPost("/register", (HttpContext context, IOptions<AzureAdOptions> azureOptions, IOptions<AuthenticationOptions> authOptions) =>
        {
            var azure = azureOptions.Value;
            var auth = authOptions.Value;
            var serverUrl = context.Request.Scheme + "://" + context.Request.Host;
            var requiredScopes = auth.RequiredScopes.Select(s => $"api://{azure.ClientId}/{s}").ToArray();

            var registration = new
            {
                client_id = azure.ClientId,
                client_secret = "*** Contact administrator ***",
                registration_endpoint = serverUrl + "/register",
                authorization_endpoint = azure.Instance + azure.TenantId + "/oauth2/v2.0/authorize",
                token_endpoint = azure.Instance + azure.TenantId + "/oauth2/v2.0/token",
                scope = string.Join(" ", requiredScopes)
            };
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(JsonSerializer.Serialize(registration));
        });

        // Add SSE URL generation endpoint for authenticated connections
        appBuilder.MapPost("/auth/sse-url", async (HttpContext context, IOptions<AuthenticationOptions> authOptions) =>
        {
            var auth = authOptions.Value;

            // Read the request body for the access token
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<Dictionary<string, object>>(body);

            if (request == null || !request.TryGetValue("access_token", out var tokenObj))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "invalid_request",
                    error_description = "access_token is required"
                }));
                return;
            }

            var accessToken = tokenObj?.ToString();
            if (string.IsNullOrEmpty(accessToken))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "invalid_request",
                    error_description = "access_token cannot be empty"
                }));
                return;
            }

            // Generate SSE URL with embedded token
            var sseUrl = $"{auth.ServerUrl.TrimEnd('/')}/?access_token={Uri.EscapeDataString(accessToken)}";

            var response = new
            {
                sse_url = sseUrl,
                expires_in = 3600 // Token-dependent, but provide a reasonable default
            };

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        });

        // Map MCP JSON-RPC SSE endpoint at root (authentication handled by middleware)
        appBuilder.MapMcp("/");

        // Add custom MCP endpoint that bypasses library session validation
        appBuilder.MapPost("/mcp-connect", [Authorize] async (HttpContext context, CustomMcpService mcpService) =>
        {
            try
            {
                var body = await new StreamReader(context.Request.Body).ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(body))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        jsonrpc = "2.0",
                        error = new
                        {
                            code = -32600,
                            message = "Invalid Request"
                        },
                        id = (object?)null
                    }));
                    return;
                }

                using var jsonDoc = JsonDocument.Parse(body);
                var response = await mcpService.HandleMcpRequestAsync(jsonDoc, context);

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (JsonException)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    error = new
                    {
                        code = -32700,
                        message = "Parse error"
                    },
                    id = (object?)null
                }));
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    error = new
                    {
                        code = -32603,
                        message = "Internal error",
                        data = ex.Message
                    },
                    id = (object?)null
                }));
            }
        });

        return app;
    }
}
