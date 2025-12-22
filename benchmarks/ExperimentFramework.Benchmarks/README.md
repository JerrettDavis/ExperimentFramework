# ExperimentFramework Performance Benchmarks

This project contains comprehensive performance benchmarks to measure the overhead of using ExperimentFramework proxies compared to direct service invocation.

## Purpose

These benchmarks provide quantifiable data about:

1. **Raw proxy overhead** - How much time does the proxy layer add to method calls?
2. **Real-world impact** - Is the overhead significant compared to actual business logic?
3. **Async vs sync performance** - Does the proxy affect async methods differently?
4. **Generic interface performance** - Is there additional overhead for generic types?

## Running the Benchmarks

### Prerequisites

- .NET 10.0 SDK or later
- Release build configuration (benchmarks should always run in Release mode)

### Quick Start

Run all benchmarks:

```bash
dotnet run -c Release
```

Run specific benchmark:

```bash
dotnet run -c Release
# Then select option 1 or 2 when prompted
```

Run from project root:

```bash
dotnet run --project benchmarks/ExperimentFramework.Benchmarks -c Release
```

### Important Notes

**Always run benchmarks in Release mode** - Debug builds include extra overhead that doesn't reflect production performance.

Benchmarks take several minutes to run because BenchmarkDotNet:
- Warms up the JIT compiler
- Runs multiple iterations to ensure statistical accuracy
- Measures memory allocations
- Validates results across multiple runs

## Benchmark Suites

### 1. ProxyOverheadBenchmarks

Measures raw proxy overhead with minimal business logic.

**Scenarios:**
- Direct service invocation (baseline)
- Proxied with feature flag selection
- Proxied with configuration value selection
- Synchronous methods
- Asynchronous methods (Task<T>)
- Generic interfaces (IGenericService<T>)

**Purpose:** Quantify the absolute overhead added by the proxy layer.

### 2. RealWorldScenarioBenchmarks

Simulates realistic workloads to show proxy overhead in context.

**Scenarios:**
- I/O-bound operations (simulated database calls with 2-15ms latency)
- CPU-bound operations (SHA256 hashing)
- Synchronous and asynchronous variants

**Purpose:** Demonstrate that proxy overhead is negligible compared to actual work.

## Interpreting Results

### Understanding the Output

BenchmarkDotNet produces detailed results including:

```
| Method                          | Mean       | Error    | StdDev   | Ratio | Rank | Allocated |
|-------------------------------- |-----------:|---------:|---------:|------:|-----:|----------:|
| Direct: Sync method             |   450.2 ns | 2.1 ns   | 1.9 ns   | 1.00  | 1    |     152 B |
| Proxied (FeatureFlag): Sync    | 3,450.8 ns | 15.2 ns  | 14.2 ns  | 7.67  | 2    |   1,024 B |
```

**Key columns:**
- **Mean**: Average execution time
- **Ratio**: How many times slower than baseline (1.00)
- **Allocated**: Memory allocated per operation
- **Rank**: Performance ranking (1 = fastest)

### Expected Results

#### Raw Overhead (ProxyOverheadBenchmarks)

| Scenario | Expected Overhead | Typical Time |
|----------|------------------|--------------|
| Sync method with feature flag | 5-10x | 2-5 μs |
| Sync method with config | 3-6x | 1-3 μs |
| Async method with feature flag | 3-7x | 3-6 μs |
| Generic interface | 5-10x | 2-5 μs |

**Why the overhead exists:**
- Feature flag evaluation via IFeatureManagerSnapshot
- Trial resolution from DI container
- Scope creation per invocation
- Decorator pipeline execution
- Task<T> type conversion for async methods

#### Real-World Impact (RealWorldScenarioBenchmarks)

| Scenario | Expected Overhead | Typical Time |
|----------|------------------|--------------|
| I/O-bound (5ms delay) | < 0.1% | ~5ms + 5μs |
| I/O-bound (2ms delay) | < 0.3% | ~2ms + 5μs |
| CPU-bound (SHA256) | < 1% | ~10-50μs + 5μs |

**Key insight:** When methods perform actual work (I/O, CPU operations), the proxy overhead becomes negligible.

### Example Analysis

If a proxied method takes 5,000 ns total:

```
Total time:        5,000 ns (5 μs)
Proxy overhead:    3,000 ns (3 μs)
Actual work:       2,000 ns (2 μs)
Overhead %:        60%
```

But if the method does real work:

```
Total time:        5,003,000 ns (5.003 ms)
Proxy overhead:        3,000 ns (3 μs)
Actual work:       5,000,000 ns (5 ms)
Overhead %:        0.06%  ← Negligible!
```

## Benchmark Configuration

BenchmarkDotNet automatically:
- Runs warmup iterations to eliminate JIT compilation overhead
- Executes multiple iterations for statistical accuracy
- Measures memory allocations
- Validates outliers and re-runs if necessary
- Generates detailed reports

Results are saved to:
```
BenchmarkDotNet.Artifacts/results/
```

## Performance Optimization Tips

If you need to minimize overhead further:

1. **Use configuration values** instead of feature flags
   - Configuration lookup is faster than feature evaluation
   - Best for environments where selection criteria rarely change

2. **Use singleton services** when appropriate
   - Reduces proxy creation overhead
   - Especially beneficial for stateless services

3. **Batch operations** when possible
   - Single proxied call to a method that processes a list
   - Better than multiple proxied calls for individual items

4. **Profile your actual workload**
   - These benchmarks use artificial delays
   - Your real services may have different performance characteristics

## Continuous Performance Monitoring

To track performance over time:

1. Run benchmarks before major changes:
   ```bash
   dotnet run -c Release > baseline.txt
   ```

2. Make your changes

3. Run benchmarks again and compare:
   ```bash
   dotnet run -c Release > new-results.txt
   diff baseline.txt new-results.txt
   ```

4. BenchmarkDotNet can also compare results automatically:
   ```bash
   dotnet run -c Release --filter *ProxyOverhead* --join
   ```

## Additional Resources

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [Understanding BenchmarkDotNet Results](https://benchmarkdotnet.org/articles/guides/console-args.html)
- [.NET Performance Best Practices](https://learn.microsoft.com/en-us/dotnet/core/performance/)

## Contributing Benchmarks

When adding new benchmarks:

1. Inherit from a base class or create a new file
2. Mark class with `[MemoryDiagnoser]` to track allocations
3. Use `[Benchmark(Baseline = true)]` to designate the comparison baseline
4. Include realistic scenarios, not just synthetic tests
5. Document expected results and what the benchmark measures
