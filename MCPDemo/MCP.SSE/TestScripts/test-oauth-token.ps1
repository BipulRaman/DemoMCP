# Test OAuth token acquisition and MCP API calls
param(
    [string]$TenantId = "79b3c9ee-76f4-414c-8636-8f36c3318796",
    [string]$ClientId = "77b032b7-d8b0-4310-b18d-a8f49d52e861",
    [string]$ServerUrl = "http://localhost:5116"
)

Write-Host "Testing OAuth token acquisition and MCP API calls..." -ForegroundColor Green

# Step 1: Get OAuth token using device code flow
Write-Host "`n1. Getting OAuth token using device code flow..." -ForegroundColor Yellow

$scope = "api://$ClientId/mcp:tools api://$ClientId/mcp:resources"
$deviceCodeUrl = "https://login.microsoftonline.com/$TenantId/oauth2/v2.0/devicecode"
$tokenUrl = "https://login.microsoftonline.com/$TenantId/oauth2/v2.0/token"

# Request device code
$deviceCodeBody = @{
    client_id = $ClientId
    scope = $scope
}

try {
    $deviceCodeResponse = Invoke-RestMethod -Uri $deviceCodeUrl -Method POST -Body $deviceCodeBody -ContentType "application/x-www-form-urlencoded"
    
    Write-Host "Device code acquired successfully!" -ForegroundColor Green
    Write-Host "User code: $($deviceCodeResponse.user_code)" -ForegroundColor Cyan
    Write-Host "Device code: $($deviceCodeResponse.device_code)" -ForegroundColor Gray
    Write-Host "`nPlease open a browser and go to: $($deviceCodeResponse.verification_uri)" -ForegroundColor Yellow
    Write-Host "Enter this code when prompted: $($deviceCodeResponse.user_code)" -ForegroundColor Cyan
    Write-Host "`nWaiting for authentication..." -ForegroundColor Yellow
    
    # Start browser automatically
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
                # Still waiting for user to complete authentication
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

Write-Host "`n2. Testing MCP server endpoints with OAuth token..." -ForegroundColor Yellow

# Test the health endpoint (no auth required)
Write-Host "`nTesting health endpoint..." -ForegroundColor Gray
try {
    $healthResponse = Invoke-RestMethod -Uri "$ServerUrl/health" -Method GET
    Write-Host "Health check: $($healthResponse.status)" -ForegroundColor Green
}
catch {
    Write-Host "Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test the capabilities endpoint (no auth required)
Write-Host "`nTesting capabilities endpoint..." -ForegroundColor Gray
try {
    $capabilitiesResponse = Invoke-RestMethod -Uri "$ServerUrl/capabilities" -Method GET
    Write-Host "Server name: $($capabilitiesResponse.name)" -ForegroundColor Green
    Write-Host "Authentication required: $($capabilitiesResponse.authentication.required)" -ForegroundColor Green
}
catch {
    Write-Host "Capabilities check failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test MCP protocol initialization (no auth required)
Write-Host "`nTesting MCP initialization..." -ForegroundColor Gray
$initializeRequest = @{
    jsonrpc = "2.0"
    id = 1
    method = "initialize"
    params = @{
        protocolVersion = "2024-11-05"
        capabilities = @{
            tools = @{}
            resources = @{}
            prompts = @{}
        }
        clientInfo = @{
            name = "test-client"
            version = "1.0.0"
        }
    }
} | ConvertTo-Json -Depth 10

try {
    $initResponse = Invoke-RestMethod -Uri $ServerUrl -Method POST -Body $initializeRequest -ContentType "application/json" -Headers @{"Accept" = "application/json"}
    Write-Host "MCP initialization successful!" -ForegroundColor Green
    Write-Host "Server capabilities: $($initResponse.result.capabilities | ConvertTo-Json -Compress)" -ForegroundColor Gray
}
catch {
    Write-Host "MCP initialization failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "Status code: $statusCode" -ForegroundColor Red
    }
}

# Test authenticated MCP tool call
Write-Host "`nTesting authenticated MCP tool call (ListSnippets)..." -ForegroundColor Gray
$listSnippetsRequest = @{
    jsonrpc = "2.0"
    id = 2
    method = "tools/call"
    params = @{
        name = "ListSnippets"
        arguments = @{}
    }
} | ConvertTo-Json -Depth 10

$headers = @{
    "Authorization" = "Bearer $accessToken"
    "Content-Type" = "application/json"
}

try {
    $listResponse = Invoke-RestMethod -Uri $ServerUrl -Method POST -Body $listSnippetsRequest -Headers $headers
    Write-Host "ListSnippets call successful!" -ForegroundColor Green
    
    if ($listResponse.result -and $listResponse.result.content) {
        $snippets = $listResponse.result.content | ConvertFrom-Json
        Write-Host "Found $($snippets.Count) snippets:" -ForegroundColor Green
        foreach ($snippet in $snippets) {
            Write-Host "  - $($snippet.name)" -ForegroundColor Cyan
        }
    }
    else {
        Write-Host "No snippets found or unexpected response format" -ForegroundColor Yellow
        Write-Host "Response: $($listResponse | ConvertTo-Json -Depth 5)" -ForegroundColor Gray
    }
}
catch {
    Write-Host "ListSnippets call failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "Status code: $statusCode" -ForegroundColor Red
        
        if ($statusCode -eq 401) {
            Write-Host "Authentication failed - token may be invalid" -ForegroundColor Red
        }
        elseif ($statusCode -eq 403) {
            Write-Host "Authorization failed - insufficient permissions" -ForegroundColor Red
        }
    }
}

# Test creating a snippet
Write-Host "`nTesting SaveSnippet with authentication..." -ForegroundColor Gray
$saveSnippetRequest = @{
    jsonrpc = "2.0"
    id = 3
    method = "tools/call"
    params = @{
        name = "SaveSnippet"
        arguments = @{
            snippetName = "test-oauth-snippet"
            snippet = "console.log('Hello from authenticated MCP!');"
        }
    }
} | ConvertTo-Json -Depth 10

try {
    $saveResponse = Invoke-RestMethod -Uri $ServerUrl -Method POST -Body $saveSnippetRequest -Headers $headers
    Write-Host "SaveSnippet call successful!" -ForegroundColor Green
    Write-Host "Response: $($saveResponse.result.content)" -ForegroundColor Gray
}
catch {
    Write-Host "SaveSnippet call failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test retrieving the snippet we just created
Write-Host "`nTesting GetSnippet with authentication..." -ForegroundColor Gray
$getSnippetRequest = @{
    jsonrpc = "2.0"
    id = 4
    method = "tools/call"
    params = @{
        name = "GetSnippet"
        arguments = @{
            snippetName = "test-oauth-snippet"
        }
    }
} | ConvertTo-Json -Depth 10

try {
    $getResponse = Invoke-RestMethod -Uri $ServerUrl -Method POST -Body $getSnippetRequest -Headers $headers
    Write-Host "GetSnippet call successful!" -ForegroundColor Green
    Write-Host "Retrieved snippet content: $($getResponse.result.content)" -ForegroundColor Cyan
}
catch {
    Write-Host "GetSnippet call failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nOAuth authentication test completed!" -ForegroundColor Green
Write-Host "Access token (first 50 chars): $($accessToken.Substring(0, [Math]::Min(50, $accessToken.Length)))..." -ForegroundColor Gray

# Decode the token to see what we got
Write-Host "`nDecoding access token..." -ForegroundColor Yellow
try {
    & ".\decode-token.ps1" -Token $accessToken
}
catch {
    Write-Host "Failed to decode token: $($_.Exception.Message)" -ForegroundColor Red
}
