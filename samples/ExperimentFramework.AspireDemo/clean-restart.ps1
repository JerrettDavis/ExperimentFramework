#!/usr/bin/env pwsh
# Clean restart script for AspireDemo

Write-Host "=== AspireDemo Clean Restart Script ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Kill all Aspire and dotnet processes
Write-Host "Step 1: Terminating all Aspire and dotnet processes..." -ForegroundColor Yellow
$processNames = @("AspireDemo.Web", "AspireDemo.AppHost", "AspireDemo.ApiService", "AspireDemo.Blog", "dotnet")

foreach ($name in $processNames) {
    $processes = Get-Process -Name $name -ErrorAction SilentlyContinue
    if ($processes) {
        Write-Host "  Killing $($processes.Count) '$name' process(es)..." -ForegroundColor Gray
        $processes | Stop-Process -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "  Waiting for processes to terminate..." -ForegroundColor Gray
Start-Sleep -Seconds 5

# Verify all terminated
$remaining = Get-Process | Where-Object { $_.ProcessName -like "AspireDemo*" }
if ($remaining) {
    Write-Host "  WARNING: Some processes still running:" -ForegroundColor Red
    $remaining | ForEach-Object { Write-Host "    - $($_.ProcessName) (PID: $($_.Id))" -ForegroundColor Red }
    Write-Host "  Attempting force kill..." -ForegroundColor Red
    $remaining | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
}

Write-Host "  ✓ All processes terminated" -ForegroundColor Green
Write-Host ""

# Step 2: Clean and rebuild
Write-Host "Step 2: Cleaning and rebuilding solution..." -ForegroundColor Yellow
Set-Location "C:\Users\jd\Downloads\ExperimentFrameworkPoc\samples\ExperimentFramework.AspireDemo"

Write-Host "  Running dotnet clean..." -ForegroundColor Gray
dotnet clean --nologo -v quiet 2>&1 | Out-Null

Write-Host "  Running dotnet build..." -ForegroundColor Gray
$buildOutput = dotnet build --no-restore --nologo 2>&1
$buildSuccess = $LASTEXITCODE -eq 0

if ($buildSuccess) {
    Write-Host "  ✓ Build succeeded" -ForegroundColor Green
} else {
    Write-Host "  ✗ Build failed:" -ForegroundColor Red
    $buildOutput | Select-Object -Last 20 | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
    Write-Host ""
    Write-Host "Aborting due to build failure." -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 3: Start Aspire AppHost
Write-Host "Step 3: Starting Aspire AppHost..." -ForegroundColor Yellow
Set-Location "AspireDemo.AppHost"

Write-Host "  Launching dotnet run in background..." -ForegroundColor Gray
$aspireJob = Start-Job -ScriptBlock {
    Set-Location "C:\Users\jd\Downloads\ExperimentFrameworkPoc\samples\ExperimentFramework.AspireDemo\AspireDemo.AppHost"
    dotnet run 2>&1
}

Write-Host "  Waiting for startup (40 seconds)..." -ForegroundColor Gray
Start-Sleep -Seconds 40

# Check if Aspire is running
$aspireWeb = Get-Process -Name "AspireDemo.Web" -ErrorAction SilentlyContinue
if ($aspireWeb) {
    Write-Host "  ✓ AspireDemo.Web is running (PID: $($aspireWeb.Id))" -ForegroundColor Green
} else {
    Write-Host "  ✗ AspireDemo.Web did not start" -ForegroundColor Red
    Write-Host "  Job output:" -ForegroundColor Red
    Receive-Job -Job $aspireJob | Select-Object -Last 20 | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
    exit 1
}
Write-Host ""

# Step 4: Test endpoints
Write-Host "Step 4: Testing endpoints..." -ForegroundColor Yellow

function Test-Endpoint {
    param(
        [string]$Url,
        [string]$Description,
        [int]$ExpectedStatus = 200,
        [int]$Timeout = 5
    )

    Write-Host "  Testing: $Description" -ForegroundColor Gray
    Write-Host "    URL: $Url" -ForegroundColor DarkGray

    try {
        $response = Invoke-WebRequest -Uri $Url -Method GET -TimeoutSec $Timeout `
            -SkipCertificateCheck -MaximumRedirection 0 -ErrorAction Stop

        $status = $response.StatusCode
        if ($status -eq $ExpectedStatus) {
            Write-Host "    ✓ Status: $status (expected: $ExpectedStatus)" -ForegroundColor Green
            return $true
        } else {
            Write-Host "    ✗ Status: $status (expected: $ExpectedStatus)" -ForegroundColor Red
            return $false
        }
    } catch {
        $status = $_.Exception.Response.StatusCode.Value__
        if ($status -eq $ExpectedStatus) {
            Write-Host "    ✓ Status: $status (expected: $ExpectedStatus)" -ForegroundColor Green
            return $true
        } else {
            Write-Host "    ✗ Error: $($_.Exception.Message)" -ForegroundColor Red
            if ($status) {
                Write-Host "    ✗ Status: $status (expected: $ExpectedStatus)" -ForegroundColor Red
            }
            return $false
        }
    }
}

$tests = @(
    @{ Url = "https://localhost:7201/Account/Login"; Description = "Login page (Razor Page)"; ExpectedStatus = 200 }
    @{ Url = "https://localhost:7201/"; Description = "Root URL (redirects to dashboard)"; ExpectedStatus = 302 }
    @{ Url = "https://localhost:7201/welcome"; Description = "Welcome page (Blazor)"; ExpectedStatus = 200 }
    @{ Url = "https://localhost:7201/dashboard"; Description = "Dashboard home (Blazor w/ auth)"; ExpectedStatus = 200 }
    @{ Url = "https://localhost:7201/demo"; Description = "Live demo page (Blazor)"; ExpectedStatus = 200 }
)

$passCount = 0
$failCount = 0

foreach ($test in $tests) {
    $result = Test-Endpoint -Url $test.Url -Description $test.Description -ExpectedStatus $test.ExpectedStatus
    if ($result) {
        $passCount++
    } else {
        $failCount++
    }
    Write-Host ""
}

# Summary
Write-Host "=== Test Summary ===" -ForegroundColor Cyan
Write-Host "  Passed: $passCount" -ForegroundColor Green
Write-Host "  Failed: $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($failCount -eq 0) {
    Write-Host "✓ All tests passed! Application is running correctly." -ForegroundColor Green
} else {
    Write-Host "✗ Some tests failed. Check the output above for details." -ForegroundColor Red
}

Write-Host ""
Write-Host "Aspire Dashboard: https://localhost:17014" -ForegroundColor Cyan
Write-Host "Application: https://localhost:7201" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Ctrl+C to stop the application and terminate all processes." -ForegroundColor Yellow
Write-Host ""

# Keep script running and monitor
try {
    while ($true) {
        Start-Sleep -Seconds 10
        $webProcess = Get-Process -Name "AspireDemo.Web" -ErrorAction SilentlyContinue
        if (-not $webProcess) {
            Write-Host "WARNING: AspireDemo.Web process terminated unexpectedly!" -ForegroundColor Red
            break
        }
    }
} finally {
    Write-Host ""
    Write-Host "Cleaning up..." -ForegroundColor Yellow
    Stop-Job -Job $aspireJob -ErrorAction SilentlyContinue
    Remove-Job -Job $aspireJob -Force -ErrorAction SilentlyContinue

    Get-Process | Where-Object { $_.ProcessName -like "AspireDemo*" } | Stop-Process -Force -ErrorAction SilentlyContinue
    Write-Host "✓ Cleanup complete" -ForegroundColor Green
}
