using MCP.HTTP.EntraAuth.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;

namespace MCP.HTTP.EntraAuth.Middleware;

/// <summary>
/// The McpAuthenticationMiddleware class is responsible for handling authentication
/// </summary>
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

    /// <summary>
    /// Middleware constructor that initializes the middleware with necessary dependencies.
    /// </summary>
    /// <param name="next"></param>
    /// <param name="logger"></param>
    /// <param name="authOptions"></param>
    /// <param name="azureAdOptions"></param>
    /// <exception cref="ArgumentNullException"></exception>
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

    /// <summary>
    /// Middleware entry point that processes incoming HTTP requests.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
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
                _logger.LogDebug("{Class}_{Method} : Could not extract method from MCP request, continuing...",
                    nameof(McpAuthenticationMiddleware), nameof(HandleMcpRequestAsync));
                await ContinueToNextMiddleware(context);
                return;
            }

            // Check if method is allowed without authentication
            if (IsMethodAllowedWithoutAuth(method))
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("{Class}_{Method} : Allowing MCP protocol method without auth: {Method}",
                        nameof(McpAuthenticationMiddleware), nameof(HandleMcpRequestAsync), method);
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
                    _logger.LogDebug("{Class}_{Method} : Authenticated request from {Name} for method: {Method}",
                        nameof(McpAuthenticationMiddleware), nameof(HandleMcpRequestAsync),
                        GetUserName(context), method);
                }
            }

            await ContinueToNextMiddleware(context);
        }
        catch (JsonException)
        {
            // If we can't parse the JSON, let the MCP server handle it
            _logger.LogDebug("{Class}_{Method} : Could not parse MCP request JSON, continuing...",
                nameof(McpAuthenticationMiddleware), nameof(HandleMcpRequestAsync));
            await ContinueToNextMiddleware(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Class}_{Method} : Error processing MCP request: {ErrorMessage}",
                nameof(McpAuthenticationMiddleware), nameof(HandleMcpRequestAsync), ex.Message);
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

    private static string GetUserName(HttpContext context)
    {
        // Try multiple claim types to get the user name
        var name = GetClaimValue(context, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
        return string.IsNullOrEmpty(name) ? "unknown" : name;
    }

    private async Task HandleNonMcpRequestAsync(HttpContext context)
    {
        if (IsUserAuthenticated(context))
        {
            LogAuthenticatedUser(context);
        }
        else if (IsProtectedPath(context.Request.Path))
        {
            _logger.LogWarning("{Class}_{Method} : Unauthenticated request to protected path: {Path}",
                nameof(McpAuthenticationMiddleware), nameof(HandleNonMcpRequestAsync), context.Request.Path);
        }

        await ContinueToNextMiddleware(context);
    }

    private void LogAuthenticatedUser(HttpContext context)
    {
        var name = GetClaimValue(context, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
        var upn = GetClaimValue(context, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
        var scopes = GetClaimValue(context, "http://schemas.microsoft.com/identity/claims/scope");
        var roles = string.Join(", ", context.User.FindAll(ClaimTypes.Role).Select(c => c.Value));

        _logger.LogDebug("{Class}_{Method} : Authenticated request from {Name} ({UPN}) with scopes: {Scopes}, roles: {Roles}",
            nameof(McpAuthenticationMiddleware), nameof(LogAuthenticatedUser), name, upn, scopes, roles);
    }

    private static string GetClaimValue(HttpContext context, params string[] claimTypes)
    {
        if (context?.User?.Claims == null)
            return "unknown";

        foreach (var claimType in claimTypes)
        {
            if (string.IsNullOrEmpty(claimType))
                continue;

            var claim = context.User.Claims.FirstOrDefault(c =>
                string.Equals(c.Type, claimType, StringComparison.OrdinalIgnoreCase));

            if (claim != null && !string.IsNullOrEmpty(claim.Value))
            {
                return claim.Value;
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
        _logger.LogWarning("{Class}_{Method} : Unauthenticated request to protected MCP method",
            nameof(McpAuthenticationMiddleware), nameof(SendAuthenticationRequiredResponseAsync));

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

/// <summary>
/// Middleware extension methods for adding MCP authentication to the ASP.NET Core pipeline.
/// </summary>
public static class McpAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseMcpAuthentication(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseMiddleware<McpAuthenticationMiddleware>();
    }
}