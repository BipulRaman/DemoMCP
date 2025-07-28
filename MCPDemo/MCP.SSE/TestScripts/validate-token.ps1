# Test token validation manually
param(
    [string]$Token = "",
    [string]$TenantId = "79b3c9ee-76f4-414c-8636-8f36c3318796",
    [string]$ClientId = "77b032b7-d8b0-4310-b18d-a8f49d52e861"
)

if (-not $Token) {
    Write-Host "Please provide a token as parameter" -ForegroundColor Red
    exit 1
}

Write-Host "Validating token manually..." -ForegroundColor Green

# Decode the token parts
$tokenParts = $Token.Split('.')
if ($tokenParts.Length -ne 3) {
    Write-Host "Invalid JWT format" -ForegroundColor Red
    exit 1
}

# Decode payload
$payloadBytes = [System.Convert]::FromBase64String(($tokenParts[1] + "==").Substring(0, ($tokenParts[1].Length + 3) -band -4))
$payload = [System.Text.Encoding]::UTF8.GetString($payloadBytes) | ConvertFrom-Json

# Check key validation points
Write-Host "`nToken Validation Checks:" -ForegroundColor Yellow

# 1. Audience
$expectedAudiences = @("api://$ClientId", $ClientId)
$tokenAudience = $payload.aud
Write-Host "Expected audiences: $($expectedAudiences -join ', ')" -ForegroundColor Cyan
Write-Host "Token audience: $tokenAudience" -ForegroundColor Cyan
$audienceMatch = $expectedAudiences -contains $tokenAudience
Write-Host "Audience match: $audienceMatch" -ForegroundColor $(if($audienceMatch) {"Green"} else {"Red"})

# 2. Issuer
$expectedIssuer = "https://sts.windows.net/$TenantId/"
$tokenIssuer = $payload.iss
Write-Host "`nExpected issuer: $expectedIssuer" -ForegroundColor Cyan
Write-Host "Token issuer: $tokenIssuer" -ForegroundColor Cyan
$issuerMatch = $expectedIssuer -eq $tokenIssuer
Write-Host "Issuer match: $issuerMatch" -ForegroundColor $(if($issuerMatch) {"Green"} else {"Red"})

# 3. Expiry
$now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$expiry = $payload.exp
Write-Host "`nCurrent time: $now" -ForegroundColor Cyan
Write-Host "Token expiry: $expiry" -ForegroundColor Cyan
$notExpired = $now -lt $expiry
Write-Host "Not expired: $notExpired" -ForegroundColor $(if($notExpired) {"Green"} else {"Red"})

# 4. Not before
$notBefore = $payload.nbf
Write-Host "`nNot before: $notBefore" -ForegroundColor Cyan
$afterNotBefore = $now -ge $notBefore
Write-Host "After not-before: $afterNotBefore" -ForegroundColor $(if($afterNotBefore) {"Green"} else {"Red"})

# 5. Algorithm
$headerBytes = [System.Convert]::FromBase64String(($tokenParts[0] + "==").Substring(0, ($tokenParts[0].Length + 3) -band -4))
$header = [System.Text.Encoding]::UTF8.GetString($headerBytes) | ConvertFrom-Json
Write-Host "`nToken algorithm: $($header.alg)" -ForegroundColor Cyan
Write-Host "Token type: $($header.typ)" -ForegroundColor Cyan

# Summary
Write-Host "`nValidation Summary:" -ForegroundColor Yellow
$allValid = $audienceMatch -and $issuerMatch -and $notExpired -and $afterNotBefore
Write-Host "All validations pass: $allValid" -ForegroundColor $(if($allValid) {"Green"} else {"Red"})

if (-not $allValid) {
    Write-Host "`nPotential issues to check:" -ForegroundColor Red
    if (-not $audienceMatch) { Write-Host "- Audience mismatch" -ForegroundColor Red }
    if (-not $issuerMatch) { Write-Host "- Issuer mismatch" -ForegroundColor Red }
    if (-not $notExpired) { Write-Host "- Token expired" -ForegroundColor Red }
    if (-not $afterNotBefore) { Write-Host "- Token not yet valid" -ForegroundColor Red }
}
