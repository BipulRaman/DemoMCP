# Test without signature validation to isolate the issue
param(
    [string]$TenantId = "79b3c9ee-76f4-414c-8636-8f36c3318796",
    [string]$ClientId = "77b032b7-d8b0-4310-b18d-a8f49d52e861",
    [string]$ServerUrl = "http://localhost:5116"
)

Write-Host "Testing OAuth with manual token handling..." -ForegroundColor Green

# Get a fresh token
Write-Host "`n1. Getting OAuth token using device code flow..." -ForegroundColor Yellow

$scope = "api://$ClientId/mcp:tools api://$ClientId/mcp:resources"
$deviceCodeUrl = "https://login.microsoftonline.com/$TenantId/oauth2/v2.0/devicecode"
$tokenUrl = "https://login.microsoftonline.com/$TenantId/oauth2/v2.0/token"

$deviceCodeBody = @{
    client_id = $ClientId
    scope = $scope
}

try {
    $deviceCodeResponse = Invoke-RestMethod -Uri $deviceCodeUrl -Method POST -Body $deviceCodeBody -ContentType "application/x-www-form-urlencoded"
    
    Write-Host "Device code acquired successfully!" -ForegroundColor Green
    Write-Host "User code: $($deviceCodeResponse.user_code)" -ForegroundColor Cyan
    Write-Host "`nPlease authenticate with code: $($deviceCodeResponse.user_code)" -ForegroundColor Yellow
    
    Start-Process $deviceCodeResponse.verification_uri
    
    # Poll for token
    $interval = $deviceCodeResponse.interval
    $expiresIn = $deviceCodeResponse.expires_in
    $startTime = Get-Date
    
    do {
        Start-Sleep -Seconds $interval
        
        $tokenBody = @{
            grant_type = "urn:ietf:params:oauth:grant-type:device_code"
            client_id = $ClientId
            device_code = $deviceCodeResponse.device_code
        }
        
        try {
            $tokenResponse = Invoke-RestMethod -Uri $tokenUrl -Method POST -Body $tokenBody -ContentType "application/x-www-form-urlencoded"
            $accessToken = $tokenResponse.access_token
            Write-Host "Token acquired successfully!" -ForegroundColor Green
            break
        }
        catch {
            $errorResponse = $_.Exception.Response
            if ($errorResponse.StatusCode -eq 400) {
                Write-Host "." -NoNewline -ForegroundColor Gray
            }
            else {
                Write-Host "Error getting token: $($_.Exception.Message)" -ForegroundColor Red
                exit 1
            }
        }
        
        $elapsed = (Get-Date) - $startTime
        if ($elapsed.TotalSeconds -gt $expiresIn) {
            Write-Host "`nDevice code expired. Please try again." -ForegroundColor Red
            exit 1
        }
    } while ($true)
}
catch {
    Write-Host "Error getting device code: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test basic endpoints first
Write-Host "`n2. Testing basic endpoints..." -ForegroundColor Yellow

# Health check
try {
    $healthResponse = Invoke-RestMethod -Uri "$ServerUrl/health" -Method GET
    Write-Host "Health check: $($healthResponse.status)" -ForegroundColor Green
}
catch {
    Write-Host "Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Validate the token manually
Write-Host "`n3. Validating token manually..." -ForegroundColor Yellow
try {
    & ".\validate-token.ps1" -Token $accessToken
}
catch {
    Write-Host "Token validation failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test a simple authenticated request with detailed error handling
Write-Host "`n4. Testing authenticated request with detailed error capture..." -ForegroundColor Yellow

$testRequest = @{
    jsonrpc = "2.0"
    id = 1
    method = "tools/call"
    params = @{
        name = "ListSnippets"
        arguments = @{}
    }
} | ConvertTo-Json -Depth 5

$headers = @{
    "Authorization" = "Bearer $accessToken"
    "Content-Type" = "application/json"
    "Accept" = "application/json"
    "User-Agent" = "PowerShell-Test/1.0"
}

Write-Host "Request headers:" -ForegroundColor Gray
$headers | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri $ServerUrl -Method POST -Body $testRequest -Headers $headers -UseBasicParsing
    Write-Host "Success! Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response content: $($response.Content)" -ForegroundColor Gray
}
catch {
    Write-Host "Request failed: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "HTTP Status: $statusCode" -ForegroundColor Red
        
        try {
            $errorStream = $_.Exception.Response.GetResponseStream()
            if ($errorStream) {
                $reader = New-Object System.IO.StreamReader($errorStream)
                $errorContent = $reader.ReadToEnd()
                $reader.Close()
                Write-Host "Error response body: $errorContent" -ForegroundColor Red
            }
        }
        catch {
            Write-Host "Could not read error response body" -ForegroundColor Red
        }
        
        $headers = $_.Exception.Response.Headers
        if ($headers) {
            Write-Host "Response headers:" -ForegroundColor Red
            foreach ($header in $headers) {
                Write-Host "  $($header.Key): $($header.Value)" -ForegroundColor Red
            }
        }
    }
}

Write-Host "`nTest completed!" -ForegroundColor Green
