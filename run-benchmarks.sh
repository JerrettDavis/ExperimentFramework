#!/bin/bash

# ExperimentFramework Performance Benchmark Runner
# Runs benchmarks in Release configuration and saves results

echo -e "\033[0;36mExperimentFramework Performance Benchmarks\033[0m"
echo -e "\033[0;36m==========================================\033[0m"
echo ""

BENCHMARK_PROJECT="benchmarks/ExperimentFramework.Benchmarks/ExperimentFramework.Benchmarks.csproj"

# Check if project exists
if [ ! -f "$BENCHMARK_PROJECT" ]; then
    echo -e "\033[0;31mError: Benchmark project not found at $BENCHMARK_PROJECT\033[0m"
    exit 1
fi

# Ensure we're in release mode
echo -e "\033[0;33mBuilding benchmarks in Release configuration...\033[0m"
dotnet build "$BENCHMARK_PROJECT" -c Release

if [ $? -ne 0 ]; then
    echo -e "\033[0;31mBuild failed!\033[0m"
    exit 1
fi

echo ""
echo -e "\033[0;33mRunning benchmarks (this will take several minutes)...\033[0m"
echo ""

# Run benchmarks
dotnet run --project "$BENCHMARK_PROJECT" -c Release --no-build

if [ $? -eq 0 ]; then
    echo ""
    echo -e "\033[0;32mBenchmarks completed successfully!\033[0m"
    echo -e "\033[0;32mResults saved to: benchmarks/ExperimentFramework.Benchmarks/BenchmarkDotNet.Artifacts/results/\033[0m"
else
    echo -e "\033[0;31mBenchmark run failed!\033[0m"
    exit 1
fi
