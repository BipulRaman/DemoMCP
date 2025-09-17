using MCP.HTTP.EntraAuth.Configuration;
using MCP.HTTP.EntraAuth.Models;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace MCP.HTTP.EntraAuth.Services;

/// <summary>
/// Implementation of device code authentication state management - returns Entra ID tokens directly
/// </summary>
public class AuthenticationStateService : IAuthenticationStateService
{
    private readonly AzureAdConfig _azureConfig;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthenticationStateService> _logger;
    private readonly ConcurrentDictionary<string, AuthenticationSession> _sessions = new();

    public AuthenticationStateService(
        IOptions<AzureAdConfig> azureConfig,
        HttpClient httpClient,
        ILogger<AuthenticationStateService> logger)
    {
        _azureConfig = azureConfig.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<DeviceCodeResult> StartDeviceCodeFlowAsync(string sessionId)
    {
        try
        {
            _logger.LogInformation("Starting device code flow for session {SessionId}", sessionId);

            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _azureConfig.ClientId),
                new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default")
            });

            var response = await _httpClient.PostAsync(
                $"https://login.microsoftonline.com/{_azureConfig.TenantId}/oauth2/v2.0/devicecode",
                requestBody);

            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogDebug("Device code response: {Response}", responseContent);

            var result = ParseDeviceCodeResponse(responseContent);
            
            // Store session
            _sessions[sessionId] = new AuthenticationSession
            {
                DeviceCode = result.DeviceCode,
                ExpiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresIn),
                Interval = result.Interval,
                IsComplete = false
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting device code flow for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<bool> PollForCompletionAsync(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            _logger.LogWarning("Session {SessionId} not found", sessionId);
            return false;
        }

        if (session.IsComplete)
        {
            return true;
        }

        if (DateTime.UtcNow > session.ExpiresAt)
        {
            _logger.LogInformation("Session {SessionId} expired", sessionId);
            _sessions.TryRemove(sessionId, out _);
            return false;
        }

        try
        {
            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
                new KeyValuePair<string, string>("client_id", _azureConfig.ClientId),
                new KeyValuePair<string, string>("device_code", session.DeviceCode)
            });

            var response = await _httpClient.PostAsync(
                $"https://login.microsoftonline.com/{_azureConfig.TenantId}/oauth2/v2.0/token",
                requestBody);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var tokenResult = ParseTokenResponse(responseContent);
                session.AccessToken = tokenResult.AccessToken;
                session.IsComplete = true;
                
                _logger.LogInformation("Authentication completed for session {SessionId}", sessionId);
                return true;
            }
            else
            {
                var error = ParseErrorResponse(responseContent);
                if (error == "authorization_pending")
                {
                    // Still waiting for user to complete authentication
                    return false;
                }
                else
                {
                    _logger.LogWarning("Authentication error for session {SessionId}: {Error}", sessionId, error);
                    _sessions.TryRemove(sessionId, out _);
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error polling authentication status for session {SessionId}", sessionId);
            return false;
        }
    }

    public Task<string?> GetAccessTokenAsync(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session) && session.IsComplete)
        {
            return Task.FromResult<string?>(session.AccessToken);
        }
        return Task.FromResult<string?>(null);
    }

    public Task<bool> IsSessionAuthenticatedAsync(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult(session.IsComplete && !string.IsNullOrEmpty(session.AccessToken));
        }
        return Task.FromResult(false);
    }

    public Task<string?> GetJwtTokenAsync(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session) && session.IsComplete && !string.IsNullOrEmpty(session.AccessToken))
        {
            // Return the Entra ID access token directly (it's already a JWT)
            _logger.LogInformation("Entra ID access token retrieved for session {SessionId}", sessionId);
            return Task.FromResult<string?>(session.AccessToken);
        }
        return Task.FromResult<string?>(null);
    }

    private DeviceCodeResult ParseDeviceCodeResponse(string response)
    {
        // Simple JSON parsing to avoid AOT issues
        var lines = response.Replace("{", "").Replace("}", "").Replace("\"", "").Split(',');
        var result = new DeviceCodeResult();

        foreach (var line in lines)
        {
            var parts = line.Split(':');
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();

                switch (key)
                {
                    case "device_code":
                        result.DeviceCode = value;
                        break;
                    case "user_code":
                        result.UserCode = value;
                        break;
                    case "verification_uri":
                        result.VerificationUri = value;
                        break;
                    case "verification_uri_complete":
                        result.VerificationUriComplete = value;
                        break;
                    case "expires_in":
                        if (int.TryParse(value, out var expiresIn))
                            result.ExpiresIn = expiresIn;
                        break;
                    case "interval":
                        if (int.TryParse(value, out var interval))
                            result.Interval = interval;
                        break;
                }
            }
        }

        return result;
    }

    private TokenResult ParseTokenResponse(string response)
    {
        var lines = response.Replace("{", "").Replace("}", "").Replace("\"", "").Split(',');
        var result = new TokenResult();

        foreach (var line in lines)
        {
            var parts = line.Split(':');
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();

                if (key == "access_token")
                {
                    result.AccessToken = value;
                    break;
                }
            }
        }

        return result;
    }

    private string ParseErrorResponse(string response)
    {
        var lines = response.Replace("{", "").Replace("}", "").Replace("\"", "").Split(',');
        
        foreach (var line in lines)
        {
            var parts = line.Split(':');
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();

                if (key == "error")
                {
                    return value;
                }
            }
        }

        return "unknown_error";
    }

    private class AuthenticationSession
    {
        public string DeviceCode { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int Interval { get; set; }
        public bool IsComplete { get; set; }
        public string? AccessToken { get; set; }
    }

    private class TokenResult
    {
        public string AccessToken { get; set; } = string.Empty;
    }
}