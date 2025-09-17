namespace MCP.HTTP.EntraAuth.Services;

/// <summary>
/// Service for managing authentication state using device code flow
/// </summary>
public interface IAuthenticationStateService
{
    /// <summary>
    /// Starts a device code authentication flow
    /// </summary>
    /// <param name="sessionId">Session ID for this authentication flow</param>
    /// <returns>Device code authentication result</returns>
    Task<DeviceCodeResult> StartDeviceCodeFlowAsync(string sessionId);

    /// <summary>
    /// Polls to check if authentication is complete
    /// </summary>
    /// <param name="sessionId">Session ID to check</param>
    /// <returns>True if authentication is complete, false otherwise</returns>
    Task<bool> PollForCompletionAsync(string sessionId);

    /// <summary>
    /// Gets the access token for a completed authentication session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Access token if available, null otherwise</returns>
    Task<string?> GetAccessTokenAsync(string sessionId);

    /// <summary>
    /// Checks if a session is authenticated and has a valid token
    /// </summary>
    /// <param name="sessionId">Session ID to check</param>
    /// <returns>True if authenticated, false otherwise</returns>
    Task<bool> IsSessionAuthenticatedAsync(string sessionId);
}

/// <summary>
/// Result from device code authentication initiation
/// </summary>
public class DeviceCodeResult
{
    public string DeviceCode { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public string VerificationUri { get; set; } = string.Empty;
    public string VerificationUriComplete { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public int Interval { get; set; }
}