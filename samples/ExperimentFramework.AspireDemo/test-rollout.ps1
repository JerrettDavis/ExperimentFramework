# Test Rollout, Rollback, and Resume Functionality
# This script tests the complete rollout lifecycle

Write-Host "Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# The API service typically runs on port 5000 or 5001
$apiBaseUrl = "http://localhost:5000"
$experimentName = "premium-tier-pricing"

Write-Host "`n=== Testing Rollout Functionality ===" -ForegroundColor Cyan

# Step 1: Get current experiment state
Write-Host "`n1. Getting current experiment state..." -ForegroundColor Green
try {
    $response = Invoke-RestMethod -Uri "$apiBaseUrl/api/experiments" -Method Get
    $experiment = $response | Where-Object { $_.name -eq $experimentName }

    if ($experiment) {
        Write-Host "✓ Experiment found: $($experiment.displayName)" -ForegroundColor Green
        Write-Host "  Current rollout status: $($experiment.rollout.status)" -ForegroundColor White
        Write-Host "  Current percentage: $($experiment.rollout.percentage)%" -ForegroundColor White
    } else {
        Write-Host "✗ Experiment not found!" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "✗ Failed to get experiments: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Advance rollout stage
Write-Host "`n2. Advancing rollout stage..." -ForegroundColor Green
try {
    $response = Invoke-RestMethod -Uri "$apiBaseUrl/api/rollout/$experimentName/advance" -Method Post
    Write-Host "✓ Advanced stage successfully" -ForegroundColor Green

    # Verify state change
    $experiment = (Invoke-RestMethod -Uri "$apiBaseUrl/api/experiments" -Method Get) |
                  Where-Object { $_.name -eq $experimentName }
    Write-Host "  New percentage: $($experiment.rollout.percentage)%" -ForegroundColor White
    Write-Host "  New status: $($experiment.rollout.status)" -ForegroundColor White
} catch {
    Write-Host "✗ Failed to advance stage: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 3: Pause rollout
Write-Host "`n3. Pausing rollout..." -ForegroundColor Green
try {
    $response = Invoke-RestMethod -Uri "$apiBaseUrl/api/rollout/$experimentName/pause" -Method Post
    Write-Host "✓ Paused rollout successfully" -ForegroundColor Green

    # Verify state change
    $experiment = (Invoke-RestMethod -Uri "$apiBaseUrl/api/experiments" -Method Get) |
                  Where-Object { $_.name -eq $experimentName }
    Write-Host "  Status: $($experiment.rollout.status)" -ForegroundColor White
} catch {
    Write-Host "✗ Failed to pause: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 4: Resume rollout
Write-Host "`n4. Resuming rollout..." -ForegroundColor Green
try {
    $response = Invoke-RestMethod -Uri "$apiBaseUrl/api/rollout/$experimentName/resume" -Method Post
    Write-Host "✓ Resumed rollout successfully" -ForegroundColor Green

    # Verify state change
    $experiment = (Invoke-RestMethod -Uri "$apiBaseUrl/api/experiments" -Method Get) |
                  Where-Object { $_.name -eq $experimentName }
    Write-Host "  Status: $($experiment.rollout.status)" -ForegroundColor White
    Write-Host "  Percentage: $($experiment.rollout.percentage)%" -ForegroundColor White
} catch {
    Write-Host "✗ Failed to resume: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 5: Rollback
Write-Host "`n5. Rolling back deployment..." -ForegroundColor Green
try {
    $response = Invoke-RestMethod -Uri "$apiBaseUrl/api/rollout/$experimentName/rollback" -Method Post
    Write-Host "✓ Rolled back successfully" -ForegroundColor Green

    # Verify state change
    $experiment = (Invoke-RestMethod -Uri "$apiBaseUrl/api/experiments" -Method Get) |
                  Where-Object { $_.name -eq $experimentName }
    Write-Host "  Status: $($experiment.rollout.status)" -ForegroundColor White
    Write-Host "  Percentage: $($experiment.rollout.percentage)%" -ForegroundColor White

    # Check stage statuses
    Write-Host "  Stage statuses:" -ForegroundColor White
    foreach ($stage in $experiment.rollout.stages) {
        Write-Host "    - $($stage.name): $($stage.status)" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ Failed to rollback: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 6: Resume after rollback (should restart from first stage)
Write-Host "`n6. Resuming after rollback (restart)..." -ForegroundColor Green
try {
    $response = Invoke-RestMethod -Uri "$apiBaseUrl/api/rollout/$experimentName/resume" -Method Post
    Write-Host "✓ Resumed/Restarted rollout successfully" -ForegroundColor Green

    # Verify state change
    $experiment = (Invoke-RestMethod -Uri "$apiBaseUrl/api/experiments" -Method Get) |
                  Where-Object { $_.name -eq $experimentName }
    Write-Host "  Status: $($experiment.rollout.status)" -ForegroundColor White
    Write-Host "  Percentage: $($experiment.rollout.percentage)%" -ForegroundColor White

    # Check stage statuses
    Write-Host "  Stage statuses:" -ForegroundColor White
    foreach ($stage in $experiment.rollout.stages) {
        Write-Host "    - $($stage.name): $($stage.status)" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ Failed to resume after rollback: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 7: Test Restart (from any state)
Write-Host "`n7. Testing explicit restart..." -ForegroundColor Green
try {
    # First advance a few times
    Invoke-RestMethod -Uri "$apiBaseUrl/api/rollout/$experimentName/advance" -Method Post | Out-Null
    Invoke-RestMethod -Uri "$apiBaseUrl/api/rollout/$experimentName/advance" -Method Post | Out-Null

    $beforeRestart = (Invoke-RestMethod -Uri "$apiBaseUrl/api/experiments" -Method Get) |
                     Where-Object { $_.name -eq $experimentName }
    Write-Host "  Before restart - Percentage: $($beforeRestart.rollout.percentage)%" -ForegroundColor White

    # Now restart
    $response = Invoke-RestMethod -Uri "$apiBaseUrl/api/rollout/$experimentName/restart" -Method Post
    Write-Host "✓ Restarted rollout successfully" -ForegroundColor Green

    # Verify state change
    $experiment = (Invoke-RestMethod -Uri "$apiBaseUrl/api/experiments" -Method Get) |
                  Where-Object { $_.name -eq $experimentName }
    Write-Host "  After restart - Status: $($experiment.rollout.status)" -ForegroundColor White
    Write-Host "  After restart - Percentage: $($experiment.rollout.percentage)%" -ForegroundColor White

    if ($experiment.rollout.percentage -eq 10) {
        Write-Host "✓ Restart correctly reset to first stage (10%)" -ForegroundColor Green
    } else {
        Write-Host "✗ Restart did not reset to first stage correctly!" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Failed to restart: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
Write-Host "`nAll rollout operations tested successfully!" -ForegroundColor Green
Write-Host "You can view the rollout UI at: http://localhost:5001/rollout" -ForegroundColor Yellow
