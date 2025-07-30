using MCP.SSE.EntraAuth.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;

namespace MCP.SSE.EntraAuth.Middleware;

public class McpAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<McpAuthenticationMiddleware> _logger;
    private readonly AuthenticationConfig _authOptions;
    private readonly AzureAdConfig _azureAdOptions;

    // Constants for better performance and maintainability
    private static readonly string[] AllowedMethods = { "initialize", "initialized", "ping", "notifications/initialized" };
    private static readonly string[] ProtectedMethodPrefixes = { "tools/", "resources/", "prompts/" };
    private static readonly PathString McpRootPath = new("/");
    private static readonly PathString McpProtectedPath = new("/mcp");
    private static readonly PathString ApiProtectedPath = new("/api");
    private const string PostMethod = "POST";
    private const string JsonContentType = "application/json";
    private const int AuthenticationRequiredCode = -32001;
    private const string JsonRpcVersion = "2.0";

    public McpAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<McpAuthenticationMiddleware> logger,
        IOptions<AuthenticationConfig> authOptions,
        IOptions<AzureAdConfig> azureAdOptions)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
        _azureAdOptions = azureAdOptions?.Value ?? throw new ArgumentNullException(nameof(azureAdOptions));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Early exit conditions
        if (context.RequestAborted.IsCancellationRequested || context.Response.HasStarted)
            return;

        // Check if this is an MCP request (POST to root path)
        if (IsRootMcpRequest(context))
        {
            await HandleMcpRequestAsync(context);
            return;
        }

        // For non-MCP requests, apply standard logic
        await HandleNonMcpRequestAsync(context);
    }

    private static bool IsRootMcpRequest(HttpContext context) =>
        context.Request.Path == McpRootPath &&
        string.Equals(context.Request.Method, PostMethod, StringComparison.OrdinalIgnoreCase);

    private async Task HandleMcpRequestAsync(HttpContext context)
    {
        // Enable buffering only when necessary
        if (!context.Request.Body.CanSeek)
        {
            context.Request.EnableBuffering();
        }

        try
        {
            var method = await ExtractMethodFromJsonAsync(context.Request.Body);

            if (method == null)
            {
                _logger.LogDebug("Could not extract method from MCP request, continuing...");
                await ContinueToNextMiddleware(context);
                return;
            }

            // Check if method is allowed without authentication
            if (IsMethodAllowedWithoutAuth(method))
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Allowing MCP protocol method without auth: {Method}", method);
                }
                await ContinueToNextMiddleware(context);
                return;
            }

            // Check if method requires authentication
            if (IsProtectedMethod(method))
            {
                if (!IsUserAuthenticated(context))
                {
                    await SendAuthenticationRequiredResponseAsync(context);
                    return;
                }

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Authenticated request from {Name} for method: {Method}",
                        GetUserName(context), method);
                }
            }

            await ContinueToNextMiddleware(context);
        }
        catch (JsonException)
        {
            // If we can't parse the JSON, let the MCP server handle it
            _logger.LogDebug("Could not parse MCP request JSON, continuing...");
            await ContinueToNextMiddleware(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MCP request");
            await ContinueToNextMiddleware(context);
        }
        finally
        {
            // Reset stream position for downstream middleware
            if (context.Request.Body.CanSeek)
            {
                context.Request.Body.Position = 0;
            }
        }
    }

    private static async Task<string?> ExtractMethodFromJsonAsync(Stream requestBody)
    {
        var originalPosition = requestBody.Position;

        try
        {
            using var jsonDocument = await JsonDocument.ParseAsync(requestBody);
            return jsonDocument.RootElement.TryGetProperty("method", out var methodElement)
                ? methodElement.GetString()
                : null;
        }
        finally
        {
            // Always reset position
            if (requestBody.CanSeek)
            {
                requestBody.Position = originalPosition;
            }
        }
    }

    private static bool IsMethodAllowedWithoutAuth(string method) =>
        Array.IndexOf(AllowedMethods, method) >= 0;

    private static bool IsProtectedMethod(string method) =>
        ProtectedMethodPrefixes.Any(prefix => method.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private static bool IsUserAuthenticated(HttpContext context) =>
        context.User.Identity?.IsAuthenticated == true;

    private static string GetUserName(HttpContext context) =>
        context.User.Identity?.Name ?? "unknown";

    private async Task HandleNonMcpRequestAsync(HttpContext context)
    {
        if (IsUserAuthenticated(context))
        {
            LogAuthenticatedUser(context);
        }
        else if (IsProtectedPath(context.Request.Path))
        {
            _logger.LogWarning("Unauthenticated request to protected path: {Path}", context.Request.Path);
        }

        await ContinueToNextMiddleware(context);
    }

    private void LogAuthenticatedUser(HttpContext context)
    {
        if (!_logger.IsEnabled(LogLevel.Debug)) return;

        var name = GetUserName(context);
        var upn = GetClaimValue(context, "upn", "preferred_username");
        var scopes = GetClaimValue(context, "scp", "scope");
        var roles = string.Join(", ", context.User.FindAll(ClaimTypes.Role).Select(c => c.Value));

        _logger.LogDebug("Authenticated request from {Name} ({UPN}) with scopes: {Scopes}, roles: {Roles}",
            name, upn, scopes, roles);
    }

    private static string GetClaimValue(HttpContext context, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = context.User.FindFirstValue(claimType);
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }
        return "unknown";
    }

    private static bool IsProtectedPath(PathString path) =>
        path.StartsWithSegments(McpProtectedPath) || path.StartsWithSegments(ApiProtectedPath);

    private async Task ContinueToNextMiddleware(HttpContext context)
    {
        await _next(context);
    }

    private async Task SendAuthenticationRequiredResponseAsync(HttpContext context)
    {
        _logger.LogWarning("Unauthenticated request to protected MCP method");

        context.Response.StatusCode = 401;
        context.Response.ContentType = JsonContentType;

        var response = CreateAuthenticationRequiredResponse();
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private object CreateAuthenticationRequiredResponse()
    {
        return new
        {
            error = new
            {
                code = AuthenticationRequiredCode,
                message = "Authentication required for this method",
                data = new
                {
                    auth_flow = "authorization_code",
                    authorize_endpoint = $"{_authOptions.ServerUrl.TrimEnd('/')}/auth/authorize",
                    token_endpoint = $"{_authOptions.ServerUrl.TrimEnd('/')}/auth/token",
                    sse_url_endpoint = $"{_authOptions.ServerUrl.TrimEnd('/')}/auth/sse-url",
                    instructions = new
                    {
                        step1 = "POST to /auth/authorize to get authorization URL",
                        step2 = "Open authorization URL in browser and authenticate",
                        step3 = "Copy authorization code from callback page",
                        step4 = "POST authorization code to /auth/token to get access token",
                        step5 = "POST access token to /auth/sse-url to get authenticated SSE URL",
                        step6 = "Use the returned SSE URL for MCP connections"
                    }
                }
            },
            id = "null", // We don't have the original request ID in this context
            jsonrpc = JsonRpcVersion
        };
    }
}

public static class McpAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseMcpAuthentication(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseMiddleware<McpAuthenticationMiddleware>();
    }
}