namespace MCP.HTTP.EntraAuth.Models;

/// <summary>
/// Device code authentication result
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