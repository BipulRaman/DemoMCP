# Test MCP request with fresh token
param(
    [Parameter(Mandatory=$true)]
    [string]$Token
)

$headers = @{
    'Authorization' = "Bearer $Token"
    'Content-Type' = 'application/json'
}

$body = @{
    'jsonrpc' = '2.0'
    'id' = 1
    'method' = 'tools/list'
    'params' = @{}
} | ConvertTo-Json

try {
    Write-Host "Testing MCP tools/list endpoint..." -ForegroundColor Green
    Write-Host "Token length: $($Token.Length) characters" -ForegroundColor Cyan
    
    $response = Invoke-RestMethod -Uri "http://localhost:5116/" -Method POST -Headers $headers -Body $body
    Write-Host "SUCCESS: MCP request worked!" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 10 | Write-Host
} catch {
    Write-Host "FAILED: MCP request failed" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response: $responseBody" -ForegroundColor Yellow
    }
}
