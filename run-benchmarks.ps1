# ExperimentFramework Performance Benchmark Runner
# Runs benchmarks in Release configuration and saves results

Write-Host "ExperimentFramework Performance Benchmarks" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

$benchmarkProject = "benchmarks\ExperimentFramework.Benchmarks\ExperimentFramework.Benchmarks.csproj"

# Check if project exists
if (-not (Test-Path $benchmarkProject)) {
    Write-Host "Error: Benchmark project not found at $benchmarkProject" -ForegroundColor Red
    exit 1
}

# Ensure we're in release mode
Write-Host "Building benchmarks in Release configuration..." -ForegroundColor Yellow
dotnet build $benchmarkProject -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Running benchmarks (this will take several minutes)..." -ForegroundColor Yellow
Write-Host ""

# Run benchmarks
dotnet run --project $benchmarkProject -c Release --no-build

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Benchmarks completed successfully!" -ForegroundColor Green
    Write-Host "Results saved to: benchmarks\ExperimentFramework.Benchmarks\BenchmarkDotNet.Artifacts\results\" -ForegroundColor Green
} else {
    Write-Host "Benchmark run failed!" -ForegroundColor Red
    exit 1
}
