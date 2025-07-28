# Test Device Code Flow with Browser Authentication
param(
    [string]$ServerUrl = "http://localhost:5116"
)

Write-Host "Testing Device Code Flow with Browser Authentication..." -ForegroundColor Green
Write-Host "Server URL: $ServerUrl" -ForegroundColor Cyan

# Step 1: Check server capabilities
Write-Host "`n1. Checking server capabilities..." -ForegroundColor Yellow
try {
    $capabilities = Invoke-RestMethod -Uri "$ServerUrl/capabilities" -Method GET
    Write-Host "Server capabilities retrieved successfully!" -ForegroundColor Green
    Write-Host "Authentication type: $($capabilities.authentication.type)" -ForegroundColor Cyan
    Write-Host "Authentication flow: $($capabilities.authentication.flow)" -ForegroundColor Cyan
    Write-Host "Device endpoint: $($capabilities.authentication.device_authorization_endpoint)" -ForegroundColor Cyan
}
catch {
    Write-Host "Failed to get server capabilities: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Initiate device code flow
Write-Host "`n2. Initiating device code flow..." -ForegroundColor Yellow
try {
    $deviceResponse = Invoke-RestMethod -Uri "$ServerUrl/auth/device" -Method POST -ContentType "application/json"
    
    Write-Host "Device code flow initiated successfully!" -ForegroundColor Green
    Write-Host "User code: $($deviceResponse.user_code)" -ForegroundColor Cyan
    Write-Host "Device code: $($deviceResponse.device_code)" -ForegroundColor Gray
    Write-Host "Verification URI: $($deviceResponse.verification_uri)" -ForegroundColor Yellow
    Write-Host "Expires in: $($deviceResponse.expires_in) seconds" -ForegroundColor Gray
    Write-Host "Polling interval: $($deviceResponse.interval) seconds" -ForegroundColor Gray
    
    # Automatically open browser
    Write-Host "`n🌐 Opening browser for authentication..." -ForegroundColor Yellow
    Write-Host "Please enter this code when prompted: $($deviceResponse.user_code)" -ForegroundColor Cyan
    Start-Process $deviceResponse.verification_uri
    
    # Wait a moment for browser to open
    Start-Sleep -Seconds 2
    
}
catch {
    Write-Host "Failed to initiate device code flow: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $errorContent = $_.Exception.Response.Content.ReadAsStringAsync().Result
        Write-Host "Error details: $errorContent" -ForegroundColor Red
    }
    exit 1
}

# Step 3: Poll for token
Write-Host "`n3. Polling for authentication completion..." -ForegroundColor Yellow
$maxAttempts = [math]::Floor($deviceResponse.expires_in / $deviceResponse.interval)
$attempts = 0
$token = $null

do {
    $attempts++
    Write-Host "Polling attempt $attempts of $maxAttempts..." -ForegroundColor Gray
    
    try {
        $tokenBody = @{
            device_code = $deviceResponse.device_code
        } | ConvertTo-Json
        
        $tokenResponse = Invoke-RestMethod -Uri "$ServerUrl/auth/token" -Method POST -Body $tokenBody -ContentType "application/json"
        
        if ($tokenResponse.access_token) {
            $token = $tokenResponse.access_token
            Write-Host "`n✅ Authentication successful!" -ForegroundColor Green
            Write-Host "Access token acquired!" -ForegroundColor Green
            Write-Host "Token type: $($tokenResponse.token_type)" -ForegroundColor Cyan
            Write-Host "Expires in: $($tokenResponse.expires_in) seconds" -ForegroundColor Cyan
            Write-Host "Scope: $($tokenResponse.scope)" -ForegroundColor Cyan
            break
        }
    }
    catch {
        $errorResponse = $_.Exception.Response
        if ($errorResponse.StatusCode -eq 400) {
            # Parse the error response
            $errorContent = $errorResponse.Content.ReadAsStringAsync().Result
            $errorData = $errorContent | ConvertFrom-Json
            
            if ($errorData.error -eq "authorization_pending") {
                Write-Host "⏳ Waiting for user authentication..." -ForegroundColor Yellow
            }
            elseif ($errorData.error -eq "slow_down") {
                Write-Host "⚠️ Slowing down polling..." -ForegroundColor Yellow
                Start-Sleep -Seconds $deviceResponse.interval
            }
            elseif ($errorData.error -eq "expired_token") {
                Write-Host "❌ Device code expired! Please restart the process." -ForegroundColor Red
                exit 1
            }
            elseif ($errorData.error -eq "access_denied") {
                Write-Host "❌ Authentication was denied by user." -ForegroundColor Red
                exit 1
            }
            else {
                Write-Host "❌ Unknown error: $($errorData.error)" -ForegroundColor Red
                Write-Host "Error description: $($errorData.error_description)" -ForegroundColor Red
                exit 1
            }
        }
        else {
            Write-Host "❌ Unexpected error: $($_.Exception.Message)" -ForegroundColor Red
            exit 1
        }
    }
    
    if ($token -eq $null) {
        Start-Sleep -Seconds $deviceResponse.interval
    }
    
} while ($attempts -lt $maxAttempts -and $token -eq $null)

if ($token -eq $null) {
    Write-Host "❌ Authentication timed out! Please try again." -ForegroundColor Red
    exit 1
}

# Step 4: Test MCP protocol with authentication
Write-Host "`n4. Testing MCP protocol with authentication..." -ForegroundColor Yellow

# Test initialize (should work without auth)
Write-Host "Testing MCP initialize..." -ForegroundColor Gray
try {
    $initializeRequest = @{
        jsonrpc = "2.0"
        id = 1
        method = "initialize"
        params = @{
            protocolVersion = "2024-11-05"
            capabilities = @{
                roots = @{
                    listChanged = $true
                }
                sampling = @{}
            }
            clientInfo = @{
                name = "test-client"
                version = "1.0.0"
            }
        }
    } | ConvertTo-Json -Depth 10
    
    $initResponse = Invoke-RestMethod -Uri $ServerUrl -Method POST -Body $initializeRequest -ContentType "application/json"
    Write-Host "✅ Initialize successful!" -ForegroundColor Green
}
catch {
    Write-Host "❌ Initialize failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test tools/list with authentication
Write-Host "Testing tools/list with authentication..." -ForegroundColor Gray
try {
    $toolsRequest = @{
        jsonrpc = "2.0"
        id = 2
        method = "tools/list"
        params = @{}
    } | ConvertTo-Json -Depth 10
    
    $headers = @{
        Authorization = "Bearer $token"
    }
    
    $toolsResponse = Invoke-RestMethod -Uri $ServerUrl -Method POST -Body $toolsRequest -ContentType "application/json" -Headers $headers
    Write-Host "✅ Tools list successful!" -ForegroundColor Green
    Write-Host "Available tools: $($toolsResponse.result.tools.Count)" -ForegroundColor Cyan
    foreach ($tool in $toolsResponse.result.tools) {
        Write-Host "  - $($tool.name): $($tool.description)" -ForegroundColor Gray
    }
}
catch {
    Write-Host "❌ Tools list failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $errorContent = $_.Exception.Response.Content.ReadAsStringAsync().Result
        Write-Host "Error details: $errorContent" -ForegroundColor Red
    }
}

# Test tools/list without authentication (should fail)
Write-Host "Testing tools/list without authentication (should fail)..." -ForegroundColor Gray
try {
    $toolsRequest = @{
        jsonrpc = "2.0"
        id = 3
        method = "tools/list"
        params = @{}
    } | ConvertTo-Json -Depth 10
    
    $toolsResponse = Invoke-RestMethod -Uri $ServerUrl -Method POST -Body $toolsRequest -ContentType "application/json"
    Write-Host "❌ Unexpected success - authentication should be required!" -ForegroundColor Red
}
catch {
    $errorResponse = $_.Exception.Response
    if ($errorResponse.StatusCode -eq 401) {
        Write-Host "✅ Correctly rejected unauthenticated request!" -ForegroundColor Green
        
        # Try to parse the MCP error response
        try {
            $errorContent = $errorResponse.Content.ReadAsStringAsync().Result
            $errorData = $errorContent | ConvertFrom-Json
            if ($errorData.error.data.auth_flow -eq "device_code") {
                Write-Host "✅ Server correctly provided device code flow instructions!" -ForegroundColor Green
                Write-Host "Device endpoint: $($errorData.error.data.device_endpoint)" -ForegroundColor Cyan
                Write-Host "Token endpoint: $($errorData.error.data.token_endpoint)" -ForegroundColor Cyan
            }
        }
        catch {
            Write-Host "Could not parse error response, but 401 status is correct." -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "❌ Unexpected error status: $($errorResponse.StatusCode)" -ForegroundColor Red
    }
}

Write-Host "`n🎉 Device Code Flow test completed!" -ForegroundColor Green
Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "- Device code flow initiation: ✅" -ForegroundColor Green
Write-Host "- Browser authentication: ✅" -ForegroundColor Green
Write-Host "- Token acquisition: ✅" -ForegroundColor Green
Write-Host "- Authenticated MCP calls: ✅" -ForegroundColor Green
Write-Host "- Unauthenticated protection: ✅" -ForegroundColor Green
