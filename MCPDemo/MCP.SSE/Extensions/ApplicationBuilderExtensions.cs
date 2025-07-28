using MCP.SSE.Configuration;
using MCP.SSE.Middleware;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MCP.SSE.Extensions;

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
        appBuilder.MapGet("/capabilities", (IOptions<McpServerOptions> mcpOptions, IOptions<AzureAdOptions> azureOptions, IOptions<AuthenticationOptions> authOptions) => 
        {
            var requiredScopes = authOptions.Value.RequiredScopes.Select(s => $"api://{azureOptions.Value.ClientId}/{s}").ToArray();
            
            return new 
            {
                name = mcpOptions.Value.Name,
                version = mcpOptions.Value.Version,
                authentication = new
                {
                    required = true,
                    type = "oauth2",
                    flow = "device_code",
                    authorization_endpoint = $"{azureOptions.Value.Instance}{azureOptions.Value.TenantId}/oauth2/v2.0/authorize",
                    token_endpoint = $"{azureOptions.Value.Instance}{azureOptions.Value.TenantId}/oauth2/v2.0/token",
                    device_authorization_endpoint = $"{azureOptions.Value.Instance}{azureOptions.Value.TenantId}/oauth2/v2.0/devicecode",
                    scopes = requiredScopes
                },
                transport = mcpOptions.Value.Transport
            };
        });

        // Add device code flow initiation endpoint
        appBuilder.MapPost("/auth/device", async (HttpContext context, IOptions<AzureAdOptions> azureOptions, IOptions<AuthenticationOptions> authOptions) =>
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
            
            var requiredScopes = auth.RequiredScopes.Select(s => $"api://{azure.ClientId}/{s}");
            var scope = string.Join(" ", requiredScopes);
            var deviceCodeUrl = $"{azure.Instance}{azure.TenantId}/oauth2/v2.0/devicecode";
            
            var deviceCodeBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", azure.ClientId),
                new KeyValuePair<string, string>("scope", scope)
            });
            
            try
            {
                var response = await httpClient.PostAsync(deviceCodeUrl, deviceCodeBody);
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
                    error = "device_code_failed",
                    error_description = ex.Message
                }));
            }
        });

        // Add device code token polling endpoint
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
            
            if (tokenRequest == null || !tokenRequest.TryGetValue("device_code", out var deviceCodeObj))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "invalid_request",
                    error_description = "device_code is required"
                }));
                return;
            }
            
            var deviceCode = deviceCodeObj?.ToString();
            if (string.IsNullOrEmpty(deviceCode))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = "invalid_request",
                    error_description = "device_code cannot be empty"
                }));
                return;
            }
            
            var tokenUrl = $"{azure.Instance}{azure.TenantId}/oauth2/v2.0/token";
            
            // Include the same scope that was used in the device authorization request
            var requiredScopes = auth.RequiredScopes.Select(s => $"api://{azure.ClientId}/{s}");
            var scope = string.Join(" ", requiredScopes);
            
            var tokenBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
                new KeyValuePair<string, string>("client_id", azure.ClientId),
                new KeyValuePair<string, string>("device_code", deviceCode),
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
                grant_types_supported = new[] { "authorization_code", "implicit", "client_credentials", "urn:ietf:params:oauth:grant-type:device_code" },
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

        // Map MCP JSON-RPC SSE endpoint at root (authentication handled by middleware)
        appBuilder.MapMcp("/");
        
        return app;
    }
}