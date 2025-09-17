# MCP OAuth Token Fetcher - Entra ID Browser Authentication Flow
# This script uses MSAL to authenticate with Microsoft Entra ID via browser flow
# and retrieves an access token for the MCP server

param(
    [Parameter(Mandatory=$false)]
    [string]$TenantId = "79b3c9ee-76f4-414c-8636-8f36c3318796",
    
    [Parameter(Mandatory=$false)]
    [string]$ClientId = "77b032b7-d8b0-4310-b18d-a8f49d52e861",
    
    [Parameter(Mandatory=$false)]
    [string[]]$Scopes = @("api://77b032b7-d8b0-4310-b18d-a8f49d52e861/mcp:tools", "api://77b032b7-d8b0-4310-b18d-a8f49d52e861/mcp:resources"),
    
    [Parameter(Mandatory=$false)]
    [string]$RedirectUri = "http://localhost:8080",
    
    [Parameter(Mandatory=$false)]
    [switch]$Silent = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$ClearCache = $false
)

# Check if MSAL.PS module is installed
if (-not (Get-Module -ListAvailable -Name MSAL.PS)) {
    Write-Host "MSAL.PS module not found. Installing..." -ForegroundColor Yellow
    try {
        Install-Module -Name MSAL.PS -Scope CurrentUser -Force -AllowClobber
        Write-Host "MSAL.PS module installed successfully." -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to install MSAL.PS module: $($_.Exception.Message)"
        exit 1
    }
}

# Import the MSAL module
Import-Module MSAL.PS

# Clear token cache if requested
if ($ClearCache) {
    Write-Host "Clearing token cache..." -ForegroundColor Yellow
    try {
        Clear-MsalTokenCache
        Write-Host "Token cache cleared successfully." -ForegroundColor Green
    }
    catch {
        Write-Warning "Failed to clear token cache: $($_.Exception.Message)"
    }
}

Write-Host "Starting Entra ID authentication flow..." -ForegroundColor Cyan
Write-Host "Tenant ID: $TenantId" -ForegroundColor Gray
Write-Host "Client ID: $ClientId" -ForegroundColor Gray
Write-Host "Scopes: $($Scopes -join ', ')" -ForegroundColor Gray
Write-Host "Redirect URI: $RedirectUri" -ForegroundColor Gray

try {
    # Attempt silent authentication first (if tokens are cached)
    if ($Silent -or (Get-MsalToken -ClientId $ClientId -TenantId $TenantId -Scopes $Scopes -Silent -ErrorAction SilentlyContinue)) {
        Write-Host "Attempting silent authentication..." -ForegroundColor Yellow
        
        $token = Get-MsalToken -ClientId $ClientId -TenantId $TenantId -Scopes $Scopes -Silent -ErrorAction SilentlyContinue
        
        if ($token) {
            Write-Host "Silent authentication successful!" -ForegroundColor Green
        }
    }
    
    # If silent authentication failed or wasn't attempted, use interactive flow
    if (-not $token) {
        Write-Host "Starting interactive browser authentication..." -ForegroundColor Yellow
        Write-Host "A browser window will open for authentication. Please complete the sign-in process." -ForegroundColor Cyan
        
        $token = Get-MsalToken -ClientId $ClientId -TenantId $TenantId -Scopes $Scopes -RedirectUri $RedirectUri -Interactive
    }
    
    if ($token) {
        Write-Host "`nAuthentication successful!" -ForegroundColor Green
        Write-Host "Token Details:" -ForegroundColor Cyan
        Write-Host "  Account: $($token.Account.Username)" -ForegroundColor White
        Write-Host "  Token Type: $($token.TokenType)" -ForegroundColor White
        Write-Host "  Expires On: $($token.ExpiresOn.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor White
        Write-Host "  Scopes: $($token.Scopes -join ', ')" -ForegroundColor White
        
        # Display the access token (first and last 10 characters for security)
        $tokenPreview = $token.AccessToken.Substring(0, 10) + "..." + $token.AccessToken.Substring($token.AccessToken.Length - 10)
        Write-Host "  Access Token: $tokenPreview" -ForegroundColor White
        
        # Output the full token to a secure location or clipboard
        Write-Host "`nAccess Token (copy to use):" -ForegroundColor Yellow
        Write-Host $token.AccessToken -ForegroundColor DarkGray
        
        # Optionally copy to clipboard
        try {
            $token.AccessToken | Set-Clipboard
            Write-Host "`nToken copied to clipboard!" -ForegroundColor Green
        }
        catch {
            Write-Warning "Could not copy token to clipboard: $($_.Exception.Message)"
        }
        
        # Save token info to a JSON file for other scripts to use
        $tokenInfo = @{
            AccessToken = $token.AccessToken
            TokenType = $token.TokenType
            ExpiresOn = $token.ExpiresOn.ToString('yyyy-MM-ddTHH:mm:ssZ')
            Account = $token.Account.Username
            Scopes = $token.Scopes
            TenantId = $TenantId
            ClientId = $ClientId
            FetchedAt = (Get-Date).ToString('yyyy-MM-ddTHH:mm:ssZ')
        }
        
        $tokenFilePath = Join-Path $PSScriptRoot "token.json"
        $tokenInfo | ConvertTo-Json -Depth 10 | Out-File -FilePath $tokenFilePath -Encoding UTF8
        Write-Host "Token information saved to: $tokenFilePath" -ForegroundColor Green
        
        return $token
    }
    else {
        Write-Error "Failed to obtain access token"
        exit 1
    }
}
catch {
    Write-Error "Authentication failed: $($_.Exception.Message)"
    Write-Host "Stack Trace:" -ForegroundColor Red
    Write-Host $_.Exception.StackTrace -ForegroundColor DarkRed
    exit 1
}

# Example usage:
# .\FetchToken.ps1
# .\FetchToken.ps1 -Silent
# .\FetchToken.ps1 -ClearCache
# .\FetchToken.ps1 -TenantId "your-tenant-id" -ClientId "your-client-id" -Scopes @("api://77b032b7-d8b0-4310-b18d-a8f49d52e861/mcp:tools", "api://77b032b7-d8b0-4310-b18d-a8f49d52e861/mcp:resources")