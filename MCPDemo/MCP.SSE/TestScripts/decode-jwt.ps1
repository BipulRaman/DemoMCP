# Decode JWT token
param(
    [Parameter(Mandatory=$true)]
    [string]$Token
)

try {
    $parts = $Token.Split('.')
    $payload = $parts[1]
    
    # Add padding if needed
    while ($payload.Length % 4 -ne 0) { 
        $payload += '=' 
    }
    
    $bytes = [System.Convert]::FromBase64String($payload)
    $json = [System.Text.Encoding]::UTF8.GetString($bytes)
    
    Write-Host "JWT Payload:" -ForegroundColor Green
    $decodedPayload = $json | ConvertFrom-Json
    $decodedPayload | ConvertTo-Json -Depth 10
    
    Write-Host "`nKey Claims:" -ForegroundColor Yellow
    Write-Host "  aud (audience): $($decodedPayload.aud)" -ForegroundColor Cyan
    Write-Host "  iss (issuer): $($decodedPayload.iss)" -ForegroundColor Cyan
    Write-Host "  exp (expires): $([DateTimeOffset]::FromUnixTimeSeconds($decodedPayload.exp).ToString())" -ForegroundColor Cyan
    Write-Host "  scp (scopes): $($decodedPayload.scp)" -ForegroundColor Cyan
    Write-Host "  roles: $($decodedPayload.roles -join ', ')" -ForegroundColor Cyan
    Write-Host "  email: $($decodedPayload.email)" -ForegroundColor Cyan
    
} catch {
    Write-Host "Failed to decode token: $($_.Exception.Message)" -ForegroundColor Red
}
