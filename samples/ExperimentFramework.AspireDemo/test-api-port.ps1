# Test script to find and test API service ports

Write-Host "Finding AspireDemo.ApiService process..."
$apiProc = Get-Process -Name "AspireDemo.ApiService" -ErrorAction SilentlyContinue

if ($apiProc) {
    Write-Host "Found ApiService PID: $($apiProc.Id)"

    # Try common Aspire ports
    $testPorts = @(5000, 5001, 5106, 5107, 7000, 7001, 8080, 8081, 5197, 5198, 7197, 7198)

    foreach ($port in $testPorts) {
        try {
            $response = Invoke-WebRequest -Uri "https://localhost:$port/api/test/hello" -SkipCertificateCheck -TimeoutSec 2 -ErrorAction Stop
            Write-Host "FOUND API at https://localhost:$port - Response: $($response.Content)" -ForegroundColor Green

            # Test governance endpoint
            $govResponse = Invoke-WebRequest -Uri "https://localhost:$port/api/governance/test" -SkipCertificateCheck -TimeoutSec 2 -ErrorAction Stop
            Write-Host "Governance endpoint: $($govResponse.Content)" -ForegroundColor Green
            break
        } catch {
            # Port not responding, continue
        }
    }
} else {
    Write-Host "ApiService process not found!" -ForegroundColor Red
}
