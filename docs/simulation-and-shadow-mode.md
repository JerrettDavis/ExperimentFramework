# Simulation & Shadow Mode

The `ExperimentFramework.Simulation` package provides shadow-mode execution and simulation reporting capabilities. It allows you to evaluate control and condition implementations side-by-side without affecting production traffic.

## Overview

Shadow mode execution enables you to:
- Run multiple implementations against the same inputs
- Compare results between control and experimental implementations
- Collect timing and performance metrics
- Generate deterministic, structured reports for CI artifacts
- Validate parity before ramping traffic to new implementations

## Installation

```bash
dotnet add package ExperimentFramework.Simulation
```

## Basic Usage

### Simple Simulation

```csharp
using ExperimentFramework.Simulation.Builders;
using ExperimentFramework.Simulation.Models;
using ExperimentFramework.Simulation.Comparators;

// Setup your service provider with implementations
var services = new ServiceCollection();
services.AddScoped<IMyDatabase, ControlDatabase>();
var provider = services.BuildServiceProvider();

// Create and configure the simulation runner
var sim = SimulationRunner.Create(provider)
    .For<IMyDatabase>()
    .AsRunnerFor<string>()
    .Control()
    .Condition("experimental")
    .WithComparator(SimulationComparators.Equality<string>())
    .ReturnControlResult();

// Define scenarios to test
var scenarios = new[]
{
    new Scenario<IMyDatabase, string>(
        "Ping", 
        async db => await db.PingAsync()),
    new Scenario<IMyDatabase, string>(
        "GetData", 
        async db => await db.GetDataAsync("key123"))
};

// Run the simulation
var report = await sim.RunAsync(scenarios);

// Write reports
report.WriteJson("artifacts/simulation.json");
report.WriteSummary("artifacts/simulation.txt");
```

## Features

### Shadow Execution Modes

The simulation runner supports different result return modes:

```csharp
// Return control result (default) - safest option
.ReturnControlResult()

// Return selected result - use experimental implementation result
.ReturnSelectedResult()

// Fail if differences detected
.FailIfDifferent()
```

### Built-in Comparators

#### Equality Comparator

Uses default equality comparison for simple types:

```csharp
.WithComparator(SimulationComparators.Equality<int>())
```

#### JSON Comparator

Serializes objects to JSON and compares structure:

```csharp
.WithComparator(SimulationComparators.Json<CustomerDto>())

// With custom JSON options
var jsonOptions = new JsonSerializerOptions 
{ 
    PropertyNameCaseInsensitive = true 
};
.WithComparator(SimulationComparators.Json<CustomerDto>(jsonOptions))
```

### Custom Comparators

Implement `ISimulationComparator<TResult>` for custom comparison logic:

```csharp
public class CustomComparator : ISimulationComparator<MyResult>
{
    public IReadOnlyList<string> Compare(
        MyResult? control, 
        MyResult? condition, 
        string conditionName)
    {
        var differences = new List<string>();
        
        // Custom comparison logic
        if (control?.Value != condition?.Value)
        {
            differences.Add($"{conditionName}: Value mismatch");
        }
        
        return differences;
    }
}

// Use custom comparator
.WithComparator(new CustomComparator())
```

### Multiple Conditions

Test control against multiple experimental implementations:

```csharp
var sim = SimulationRunner.Create(provider)
    .For<IDatabase>()
    .AsRunnerFor<List<Customer>>()
    .Control("baseline")
    .Condition("variant-a")
    .Condition("variant-b")
    .Condition("variant-c")
    .WithComparator(SimulationComparators.Json<List<Customer>>());
```

### Async and ValueTask Support

The framework natively supports `ValueTask<T>` to avoid allocations:

```csharp
// ValueTask scenarios (preferred)
new Scenario<IDatabase, Customer>(
    "GetCustomer",
    async db => await db.GetCustomerAsync(id))

// Works with synchronous code too
new Scenario<IDatabase, int>(
    "GetCount",
    db => new ValueTask<int>(42))
```

## Reports

### JSON Report

Contains structured data suitable for programmatic processing:

```csharp
report.WriteJson("artifacts/sim.json");
```

Example output:
```json
{
  "timestamp": "2026-01-15T03:00:00Z",
  "serviceType": "IMyDatabase",
  "controlName": "control",
  "conditionNames": ["experimental"],
  "passed": true,
  "summary": "Total Scenarios: 2, Successful: 2, With Differences: 0, Overall: PASSED",
  "scenarioResults": [...]
}
```

### Text Summary

Human-readable summary suitable for build logs:

```csharp
report.WriteSummary("artifacts/sim.txt");
```

Example output:
```
===========================================
Simulation Report
===========================================
Timestamp: 2026-01-15 03:00:00 UTC
Service: IMyDatabase
Control: control
Conditions: experimental
Status: PASSED

Summary:
Total Scenarios: 2, Successful: 2, With Differences: 0, Overall: PASSED

===========================================
Scenario Results
===========================================

Scenario: Ping
  All Succeeded: True
  Has Differences: False
  Control:
    control:
      Success: True
      Duration: 1.23ms
  Conditions:
    experimental:
      Success: True
      Duration: 1.45ms
```

## CI Integration

Use simulation reports in CI pipelines:

```yaml
# GitHub Actions example
- name: Run Simulations
  run: dotnet run --project SimulationRunner

- name: Upload Simulation Reports
  uses: actions/upload-artifact@v3
  with:
    name: simulation-reports
    path: artifacts/sim.*

- name: Check Simulation Results
  run: |
    if grep -q "Status: FAILED" artifacts/sim.txt; then
      echo "Simulation failed"
      exit 1
    fi
```

## Best Practices

1. **Start with Simple Scenarios**: Begin with basic operations before adding complex workflows

2. **Use Appropriate Comparators**: 
   - Use `Equality` for primitives and simple types
   - Use `Json` for DTOs and complex objects
   - Implement custom comparators for domain-specific logic

3. **Test Edge Cases**: Include null values, empty collections, and error scenarios

4. **Keep Scenarios Focused**: Each scenario should test a single operation

5. **Run in CI**: Integrate simulations into your CI pipeline to catch regressions early

6. **Review Reports**: Regularly review simulation reports to understand differences

7. **Gradual Rollout**: Use simulations to validate before increasing traffic percentages

## Advanced Example

```csharp
public class DatabaseSimulationRunner
{
    public async Task<SimulationReport> RunAsync(IServiceProvider services)
    {
        var sim = SimulationRunner.Create(services)
            .For<IDatabase>()
            .AsRunnerFor<List<Customer>>()
            .Control("postgres-v1")
            .Condition("postgres-v2")
            .Condition("cosmos-db")
            .WithComparator(new CustomerListComparator())
            .FailIfDifferent();

        var scenarios = new[]
        {
            new Scenario<IDatabase, List<Customer>>(
                "GetActiveCustomers",
                async db => await db.GetCustomersAsync(active: true)),
            
            new Scenario<IDatabase, List<Customer>>(
                "SearchByEmail",
                async db => await db.SearchCustomersAsync("@example.com")),
            
            new Scenario<IDatabase, List<Customer>>(
                "GetRecentCustomers",
                async db => await db.GetCustomersSinceAsync(DateTime.UtcNow.AddDays(-7)))
        };

        var report = await sim.RunAsync(scenarios);
        
        // Save reports
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        report.WriteJson($"artifacts/db-sim-{timestamp}.json");
        report.WriteSummary($"artifacts/db-sim-{timestamp}.txt");
        
        return report;
    }
}

public class CustomerListComparator : ISimulationComparator<List<Customer>>
{
    public IReadOnlyList<string> Compare(
        List<Customer>? control,
        List<Customer>? condition,
        string conditionName)
    {
        var differences = new List<string>();
        
        if (control == null && condition == null)
            return differences;
        
        if (control?.Count != condition?.Count)
        {
            differences.Add(
                $"{conditionName}: Count mismatch - Control: {control?.Count}, Condition: {condition?.Count}");
        }
        
        // Compare individual customers (order-independent)
        var controlSet = control?.Select(c => c.Id).ToHashSet() ?? new();
        var conditionSet = condition?.Select(c => c.Id).ToHashSet() ?? new();
        
        var missing = controlSet.Except(conditionSet);
        var extra = conditionSet.Except(controlSet);
        
        if (missing.Any())
            differences.Add($"{conditionName}: Missing IDs - {string.Join(", ", missing)}");
        
        if (extra.Any())
            differences.Add($"{conditionName}: Extra IDs - {string.Join(", ", extra)}");
        
        return differences;
    }
}
```

## Non-Goals

- **Load Testing**: This is not a load-testing tool. Use dedicated tools like k6 or JMeter for load testing.
- **Production Observability**: Use proper telemetry and monitoring solutions for production.
- **Real-time Decision Making**: Simulations are for pre-production validation, not runtime routing.

## See Also

- [ExperimentFramework README](../README.md)
- [Service Registration Safety](SERVICE_REGISTRATION_SAFETY.md)
