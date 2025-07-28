# Simple token decoder to check the JWT contents
param(
    [string]$Token
)

if (-not $Token) {
    Write-Host "Please provide a token as parameter: .\decode-token.ps1 -Token 'your_token_here'" -ForegroundColor Red
    exit 1
}

# Decode JWT without verification (just for inspection)
$tokenParts = $Token.Split('.')
if ($tokenParts.Length -ne 3) {
    Write-Host "Invalid JWT format" -ForegroundColor Red
    exit 1
}

# Decode header
$headerBytes = [System.Convert]::FromBase64String(($tokenParts[0] + "==").Substring(0, ($tokenParts[0].Length + 3) -band -4))
$header = [System.Text.Encoding]::UTF8.GetString($headerBytes) | ConvertFrom-Json

# Decode payload
$payloadBytes = [System.Convert]::FromBase64String(($tokenParts[1] + "==").Substring(0, ($tokenParts[1].Length + 3) -band -4))
$payload = [System.Text.Encoding]::UTF8.GetString($payloadBytes) | ConvertFrom-Json

Write-Host "JWT Header:" -ForegroundColor Yellow
$header | ConvertTo-Json -Depth 5

Write-Host "`nJWT Payload:" -ForegroundColor Yellow
$payload | ConvertTo-Json -Depth 5

Write-Host "`nKey Claims:" -ForegroundColor Green
Write-Host "Issuer (iss): $($payload.iss)" -ForegroundColor Cyan
Write-Host "Audience (aud): $($payload.aud)" -ForegroundColor Cyan
Write-Host "Subject (sub): $($payload.sub)" -ForegroundColor Cyan
Write-Host "Scopes (scp): $($payload.scp)" -ForegroundColor Cyan
Write-Host "Roles: $($payload.roles -join ', ')" -ForegroundColor Cyan
Write-Host "UPN: $($payload.upn)" -ForegroundColor Cyan
Write-Host "App ID (appid): $($payload.appid)" -ForegroundColor Cyan
Write-Host "Expiry: $([DateTimeOffset]::FromUnixTimeSeconds($payload.exp).ToString())" -ForegroundColor Cyan
