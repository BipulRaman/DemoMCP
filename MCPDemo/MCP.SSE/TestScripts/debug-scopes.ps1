# Debug scope matching logic
$clientId = "77b032b7-d8b0-4310-b18d-a8f49d52e861"
$scopesConfig = @("mcp:tools", "mcp:resources")
$requiredScopes = $scopesConfig | ForEach-Object { "api://$clientId/$_" }
$userScopes = @("mcp:tools", "mcp:resources")

Write-Host "Required scopes (full URIs): $($requiredScopes -join ', ')" -ForegroundColor Yellow
Write-Host "User scopes (from token): $($userScopes -join ', ')" -ForegroundColor Cyan

# Original logic (fails)
$hasMatchOriginal = $userScopes | Where-Object { $requiredScopes -contains $_ }
Write-Host "Original logic matches: $($hasMatchOriginal.Count -gt 0)" -ForegroundColor Red

# Fixed logic (should work)
$requiredScopeNames = $requiredScopes | ForEach-Object { $_.Split('/') | Select-Object -Last 1 }
Write-Host "Required scope names: $($requiredScopeNames -join ', ')" -ForegroundColor Yellow
$hasMatchFixed = $userScopes | Where-Object { $requiredScopeNames -contains $_ }
Write-Host "Fixed logic matches: $($hasMatchFixed.Count -gt 0)" -ForegroundColor Green
