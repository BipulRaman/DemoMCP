# Get OAuth Token for VS Code MCP Extension
param(
    [string]$ServerUrl = "http://localhost:5116"
)

Write-Host "Getting OAuth token for VS Code MCP Extension..." -ForegroundColor Green
Write-Host "This token can be used in VS Code's MCP extension configuration." -ForegroundColor Yellow

# Step 1: Initiate device code flow
Write-Host "`n1. Starting device code authentication..." -ForegroundColor Yellow
try {
    $deviceResponse = Invoke-RestMethod -Uri "$ServerUrl/auth/device" -Method POST -ContentType "application/json"
    
    Write-Host "Device code authentication initiated!" -ForegroundColor Green
    Write-Host "User code: $($deviceResponse.user_code)" -ForegroundColor Cyan
    Write-Host "`nOpening browser for authentication..." -ForegroundColor Yellow
    Write-Host "Please enter this code when prompted: $($deviceResponse.user_code)" -ForegroundColor Cyan
    
    # Open browser
    Start-Process $deviceResponse.verification_uri
    
    # Wait for browser to open
    Start-Sleep -Seconds 2
}
catch {
    Write-Host "Failed to initiate device code flow: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Poll for token
Write-Host "`n2. Waiting for authentication completion..." -ForegroundColor Yellow
$maxAttempts = [math]::Floor($deviceResponse.expires_in / $deviceResponse.interval)
$attempts = 0
$token = $null

do {
    $attempts++
    Write-Host "Checking authentication status... (attempt $attempts)" -ForegroundColor Gray
    
    try {
        $tokenBody = @{
            device_code = $deviceResponse.device_code
        } | ConvertTo-Json
        
        $tokenResponse = Invoke-RestMethod -Uri "$ServerUrl/auth/token" -Method POST -Body $tokenBody -ContentType "application/json"
        
        if ($tokenResponse.access_token) {
            $token = $tokenResponse.access_token
            Write-Host "`n✅ Authentication successful!" -ForegroundColor Green
            break
        }
    }
    catch {
        $errorResponse = $_.Exception.Response
        if ($errorResponse.StatusCode -eq 400) {
            # Check error type
            try {
                $errorContent = $errorResponse.Content.ReadAsStringAsync().Result
                $errorData = $errorContent | ConvertFrom-Json
                
                if ($errorData.error -eq "authorization_pending") {
                    Write-Host "⏳ Waiting for browser authentication..." -ForegroundColor Yellow
                }
                elseif ($errorData.error -eq "expired_token") {
                    Write-Host "❌ Authentication expired! Please restart." -ForegroundColor Red
                    exit 1
                }
                elseif ($errorData.error -eq "access_denied") {
                    Write-Host "❌ Authentication denied." -ForegroundColor Red
                    exit 1
                }
            }
            catch {
                Write-Host "⏳ Still waiting for authentication..." -ForegroundColor Yellow
            }
        }
        else {
            Write-Host "❌ Unexpected error: $($_.Exception.Message)" -ForegroundColor Red
            exit 1
        }
    }
    
    if ($null -eq $token) {
        Start-Sleep -Seconds $deviceResponse.interval
    }
    
} while ($attempts -lt $maxAttempts -and $null -eq $token)

if ($null -eq $token) {
    Write-Host "❌ Authentication timed out!" -ForegroundColor Red
    exit 1
}

# Step 3: Display token and instructions
Write-Host "`n🎉 Token acquired successfully!" -ForegroundColor Green
Write-Host "Expires in: $($tokenResponse.expires_in) seconds ($([math]::Round($tokenResponse.expires_in / 3600, 1)) hours)" -ForegroundColor Cyan

Write-Host "`n📋 Your OAuth Token:" -ForegroundColor Yellow
Write-Host $token -ForegroundColor White
Write-Host "`n📝 Instructions for VS Code:" -ForegroundColor Yellow
Write-Host "1. In VS Code, when prompted for 'MCP Authentication Token', paste the token above" -ForegroundColor Gray
Write-Host "2. The token will be used as: Authorization: Bearer <token>" -ForegroundColor Gray
Write-Host "3. Token expires in $([math]::Round($tokenResponse.expires_in / 3600, 1)) hours - you'll need to get a new one after that" -ForegroundColor Gray

# Copy to clipboard if possible
try {
    $token | Set-Clipboard
    Write-Host "`n📋 Token copied to clipboard!" -ForegroundColor Green
}
catch {
    Write-Host "`nNote: Could not copy to clipboard automatically." -ForegroundColor Yellow
}

Write-Host "`n🔄 To refresh the connection in VS Code:" -ForegroundColor Yellow
Write-Host "- Open Command Palette (Ctrl+Shift+P)" -ForegroundColor Gray
Write-Host "- Run: 'MCP: Restart All Servers'" -ForegroundColor Gray
Write-Host "- Enter the new token when prompted" -ForegroundColor Gray
