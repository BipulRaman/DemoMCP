# Simple test to check if Authorization header is being sent
param(
    [string]$ServerUrl = "http://localhost:5116",
    [string]$Token = ""
)

if (-not $Token) {
    Write-Host "Please provide a token as parameter" -ForegroundColor Red
    exit 1
}

Write-Host "Testing Authorization header with server..." -ForegroundColor Green

$headers = @{
    "Authorization" = "Bearer $Token"
    "Content-Type" = "application/json"
}

$testRequest = @{
    jsonrpc = "2.0"
    id = 999
    method = "tools/call"
    params = @{
        name = "ListSnippets" 
        arguments = @{}
    }
} | ConvertTo-Json -Depth 5

Write-Host "Making request with headers:" -ForegroundColor Yellow
$headers | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri $ServerUrl -Method POST -Body $testRequest -Headers $headers
    Write-Host "Response Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response Content: $($response.Content)" -ForegroundColor Gray
}
catch {
    Write-Host "Request failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        try {
            $errorStream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorStream)
            $errorContent = $reader.ReadToEnd()
            Write-Host "Error Content: $errorContent" -ForegroundColor Red
        }
        catch {
            Write-Host "Could not read error content" -ForegroundColor Red
        }
    }
}
