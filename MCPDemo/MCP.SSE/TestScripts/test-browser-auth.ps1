#!/usr/bin/env pwsh

# Test script for browser authentication flow
param(
    [string]$BaseUrl = "http://localhost:5116"
)

Write-Host "Testing Browser Authentication Flow" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green
Write-Host ""

# Step 1: Get authorization URL
Write-Host "`n1. Getting authorization URL..." -ForegroundColor Yellow
try {
    $authResponse = Invoke-RestMethod -Uri "$BaseUrl/auth/authorize" -Method POST -ContentType "application/json" -Body "{}"
    Write-Host "✓ Authorization URL received" -ForegroundColor Green
    Write-Host "State: $($authResponse.state)" -ForegroundColor Cyan
    Write-Host "Authorization URL: $($authResponse.authorization_url)" -ForegroundColor Cyan
    Write-Host "Redirect URI: $($authResponse.redirect_uri)" -ForegroundColor Cyan
    
    # Store state for later use
    $state = $authResponse.state
    
    Write-Host "`n2. Opening browser for authentication..." -ForegroundColor Yellow
    Write-Host "Please complete authentication in your browser and copy the authorization code" -ForegroundColor Cyan
    
    # Open browser (Windows)
    if ($IsWindows -or $PSVersionTable.PSVersion.Major -le 5) {
        Start-Process $authResponse.authorization_url
    }
    # Open browser (macOS)
    elseif ($IsMacOS) {
        & open $authResponse.authorization_url
    }
    # Open browser (Linux)
    elseif ($IsLinux) {
        & xdg-open $authResponse.authorization_url
    }
    
    # Wait for user to provide authorization code
    Write-Host "`n3. Waiting for authorization code..." -ForegroundColor Yellow
    $authCode = Read-Host "Enter the authorization code from the browser callback"
    
    if ([string]::IsNullOrWhiteSpace($authCode)) {
        Write-Host "✗ No authorization code provided" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "`n4. Exchanging authorization code for access token..." -ForegroundColor Yellow
    
    $tokenRequest = @{
        code = $authCode
        state = $state
    } | ConvertTo-Json
    
    $tokenResponse = Invoke-RestMethod -Uri "$BaseUrl/auth/token" -Method POST -ContentType "application/json" -Body $tokenRequest
    
    if ($tokenResponse.access_token) {
        Write-Host "✓ Access token received successfully!" -ForegroundColor Green
        Write-Host "Token Type: $($tokenResponse.token_type)" -ForegroundColor Cyan
        Write-Host "Expires In: $($tokenResponse.expires_in) seconds" -ForegroundColor Cyan
        Write-Host "Scope: $($tokenResponse.scope)" -ForegroundColor Cyan
        Write-Host "Access Token (first 50 chars): $($tokenResponse.access_token.Substring(0, [Math]::Min(50, $tokenResponse.access_token.Length)))..." -ForegroundColor Cyan
        
        # Store token for testing protected endpoints
        $accessToken = $tokenResponse.access_token
        
        Write-Host "`n5. Testing protected MCP endpoint with Authorization header..." -ForegroundColor Yellow
        
        $headers = @{
            'Authorization' = "Bearer $accessToken"
            'Content-Type' = 'application/json'
        }
        
        $mcpRequest = @{
            jsonrpc = "2.0"
            method = "tools/list"
            id = 1
        } | ConvertTo-Json
        
        try {
            # Test the custom MCP endpoint that bypasses session validation
            $mcpResponse = Invoke-RestMethod -Uri "$BaseUrl/mcp-custom" -Method POST -Headers $headers -Body $mcpRequest
            Write-Host "✓ MCP request to custom endpoint successful!" -ForegroundColor Green
            Write-Host "Response: $($mcpResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Cyan
        }
        catch {
            Write-Host "✗ MCP request failed: $($_.Exception.Message)" -ForegroundColor Red
            if ($_.Exception.Response) {
                $errorStream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($errorStream)
                $errorBody = $reader.ReadToEnd()
                Write-Host "Error details: $errorBody" -ForegroundColor Red
            }
        }
        
        Write-Host "`n6. Getting authenticated SSE URL..." -ForegroundColor Yellow
        
        $sseRequest = @{
            access_token = $accessToken
        } | ConvertTo-Json
        
        try {
            $sseResponse = Invoke-RestMethod -Uri "$BaseUrl/auth/sse-url" -Method POST -ContentType "application/json" -Body $sseRequest
            Write-Host "✓ Authenticated SSE URL generated!" -ForegroundColor Green
            Write-Host "SSE URL: $($sseResponse.sse_url)" -ForegroundColor Cyan
            Write-Host "Expires In: $($sseResponse.expires_in) seconds" -ForegroundColor Cyan
            
            Write-Host "`n7. Instructions for VS Code MCP configuration:" -ForegroundColor Yellow
            Write-Host "Update your .vscode/mcp.json file to use the custom HTTP endpoint:" -ForegroundColor Cyan
            Write-Host @"
{
    "servers": {
        "local-mcp-sse-browser-auth": {
            "type": "http", 
            "url": "$BaseUrl/mcp-custom",
            "headers": {
                "Authorization": "Bearer $accessToken"
            }
        }
    }
}
"@ -ForegroundColor Green
            
            Write-Host "`nNote: This configuration uses the custom HTTP endpoint with Bearer token authentication." -ForegroundColor Yellow
            Write-Host "The token will need to be refreshed when it expires." -ForegroundColor Yellow
        }
        catch {
            Write-Host "✗ Failed to get SSE URL: $($_.Exception.Message)" -ForegroundColor Red
            if ($_.Exception.Response) {
                $errorStream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($errorStream)
                $errorBody = $reader.ReadToEnd()
                Write-Host "Error details: $errorBody" -ForegroundColor Red
            }
        }
    }
    else {
        Write-Host "✗ Token exchange failed" -ForegroundColor Red
        Write-Host "Response: $($tokenResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Red
    }
}
catch {
    Write-Host "✗ Error during authentication flow: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $errorStream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorStream)
        $errorBody = $reader.ReadToEnd()
        Write-Host "Error details: $errorBody" -ForegroundColor Red
    }
}

Write-Host "`nBrowser authentication flow test completed." -ForegroundColor Green
