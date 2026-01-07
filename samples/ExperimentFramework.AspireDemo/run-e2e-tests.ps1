# Script to run E2E tests
# Make sure the application is running first!

Write-Host "Checking if application is running at https://localhost:7201..." -ForegroundColor Cyan

try {
    $response = Invoke-WebRequest -Uri "https://localhost:7201" -SkipCertificateCheck -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
    Write-Host "Application is running!" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Application is not running at https://localhost:7201" -ForegroundColor Red
    Write-Host "Please start the AspireDemo application first (from AspireDemo.AppHost)" -ForegroundColor Yellow
    exit 1
}

Write-Host "`nRunning E2E tests..." -ForegroundColor Cyan
Set-Location "AspireDemo.E2ETests"
dotnet test --logger "console;verbosity=detailed"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nAll tests passed!" -ForegroundColor Green
} else {
    Write-Host "`nTests failed. Check screenshots in AspireDemo.E2ETests/bin/Debug/net10.0/" -ForegroundColor Yellow
    Write-Host "Screenshots will show what the browser sees." -ForegroundColor Yellow
}
