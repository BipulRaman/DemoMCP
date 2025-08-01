using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;

namespace MCP.HTTP.OAuth.Middleware;

public class McpAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<McpAuthenticationMiddleware> _logger;

    // Only allow basic MCP protocol methods without any authentication
    private static readonly string[] AllowedWithoutAuth = {
        "initialize",
        "initialized",
        "ping",
        "notifications/initialized",
        "capabilities/list",
        "notifications/cancelled",
        "notifications/progress"
    };

    private static readonly PathString McpRootPath = new("/");
    private const string PostMethod = "POST";
    private const string JsonContentType = "application/json";

    public McpAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<McpAuthenticationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Early exit conditions
        if (context.RequestAborted.IsCancellationRequested || context.Response.HasStarted)
            return;

        // For MCP requests (POST to root path), check if authentication is needed
        if (IsRootMcpRequest(context))
        {
            await HandleMcpRequestAsync(context);
            return;
        }

        // For all other requests, just continue without any authentication checks
        await _next(context);
    }

    private static bool IsRootMcpRequest(HttpContext context) =>
        context.Request.Path == McpRootPath &&
        string.Equals(context.Request.Method, PostMethod, StringComparison.OrdinalIgnoreCase);

    private async Task HandleMcpRequestAsync(HttpContext context)
    {
        // Enable buffering for reading the request body
        if (!context.Request.Body.CanSeek)
        {
            context.Request.EnableBuffering();
        }

        try
        {
            var method = await ExtractMethodFromJsonAsync(context.Request.Body);

            if (method == null)
            {
                _logger.LogDebug("Could not extract method from MCP request, allowing...");
                await _next(context);
                return;
            }

            // Check if this is a basic protocol method that should always be allowed
            if (IsBasicProtocolMethod(method))
            {
                _logger.LogDebug("Allowing basic MCP protocol method: {Method}", method);
                await _next(context);
                return;
            }

            // For all other methods, check if there's a bearer token
            var hasToken = HasBearerToken(context);
            if (!hasToken)
            {
                _logger.LogDebug("No bearer token for method: {Method}, rejecting", method);
                await SendSimpleErrorResponseAsync(context);
                return;
            }

            _logger.LogDebug("Bearer token found for method: {Method}, allowing", method);
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error processing MCP request: {Error}, allowing anyway", ex.Message);
            await _next(context);
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
            if (requestBody.CanSeek)
            {
                requestBody.Position = originalPosition;
            }
        }
    }

    private static bool IsBasicProtocolMethod(string method) =>
        AllowedWithoutAuth.Contains(method, StringComparer.OrdinalIgnoreCase);

    private static bool HasBearerToken(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return false;

        var token = authHeader.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrEmpty(token))
            return false;

        // Actually validate the JWT token structure
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (tokenHandler.CanReadToken(token))
            {
                var jsonToken = tokenHandler.ReadJwtToken(token);
                // Check if it has at least some claims (basic validation)
                return jsonToken.Claims.Any();
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private async Task SendSimpleErrorResponseAsync(HttpContext context)
    {
        // Use 403 Forbidden instead of 401 Unauthorized to avoid triggering auth flows
        context.Response.StatusCode = 403;
        context.Response.ContentType = JsonContentType;

        // Make sure no authentication headers are sent that could trigger auth flows
        context.Response.Headers.Remove("WWW-Authenticate");

        var response = new
        {
            error = new
            {
                code = -32001,
                message = "Bearer token required for this operation"
            },
            id = "null",
            jsonrpc = "2.0"
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
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