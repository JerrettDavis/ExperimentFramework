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
    .WithResultType<string>()
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
    .WithResultType<List<Customer>>()
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

## Isolation vs Integration Models

### Understanding Output-Based vs Action-Based Experiments

**Output-Based Experiments** test operations that return data (reads):
- ✅ Safe to run with real implementations
- ✅ Compare returned values
- ✅ No side effects
- Example: `GetCustomer`, `ListOrders`, `SearchProducts`

**Action-Based Experiments** test operations that modify state (writes):
- ⚠️ Require careful consideration of side effects
- ⚠️ May need isolation or cleanup strategies
- Example: `CreateOrder`, `UpdateInventory`, `DeleteUser`

### Isolation Model - Safe Testing Without Side Effects

Use **mock or simulated implementations** to test write operations without affecting real systems:

```csharp
// Register MOCK implementations for isolated testing
services.AddKeyedScoped<IDatabase>("control", (sp, key) => new MockDatabase());
services.AddKeyedScoped<IDatabase>("new-impl", (sp, key) => new MockNewDatabase());

var sim = SimulationRunner.Create(services)
    .For<IDatabase>()
    .WithResultType<int>()
    .Control("control")
    .Condition("new-impl")
    .WithComparator(SimulationComparators.Equality<int>())
    .ReturnControlResult();

// Safe to test write operations - no real database is affected
var scenarios = new[]
{
    new Scenario<IDatabase, int>(
        "CreateCustomer",
        async db => await db.CreateCustomerAsync(customer))
};
```

**Benefits of Isolation Model:**
- ✅ No cleanup required
- ✅ Safe to run in CI/CD repeatedly
- ✅ Fast execution
- ✅ Prevents cascading to dependent systems
- ✅ Ideal for development and pre-production testing

### Integration Model - Real Shadow Writes

Use **real implementations** to validate behavior with actual systems:

```csharp
// Register REAL implementations for shadow writes
services.AddKeyedScoped<IDatabase>("control", (sp, key) => new ProductionDatabase());
services.AddKeyedScoped<IDatabase>("new-impl", (sp, key) => new NewDatabase());

var sim = SimulationRunner.Create(services)
    .For<IDatabase>()
    .WithResultType<int>()
    .Control("control")
    .Condition("new-impl")
    .WithComparator(SimulationComparators.Equality<int>())
    .ReturnControlResult(); // Returns control result

// Both implementations will write to real databases
var scenarios = new[]
{
    new Scenario<IDatabase, int>(
        "CreateCustomer",
        async db => await db.CreateCustomerAsync(customer))
};
```

**Use Cases for Integration Model:**
- Final validation before production deployment
- Performance testing with real infrastructure
- Validating integration points
- Testing with actual data volumes

**Important Considerations:**
- ⚠️ Both control and condition implementations execute
- ⚠️ Real side effects occur (database writes, API calls, etc.)
- ⚠️ Requires cleanup/rollback strategy
- ⚠️ Consider data volume and performance impact
- ⚠️ Review compliance and security implications

### Preventing Dependency Cascading

When testing a service with dependencies, control which implementations are used to prevent unwanted cascading:

```csharp
// Example: Testing OrderService without cascading to payment gateway
services.AddKeyedScoped<IOrderDatabase>("test", (sp, key) => new MockOrderDatabase());
services.AddKeyedScoped<IPaymentGateway>("test", (sp, key) => new MockPaymentGateway()); // Mock!
services.AddKeyedScoped<IInventoryService>("test", (sp, key) => new MockInventoryService()); // Mock!
services.AddKeyedScoped<IOrderService, OrderService>(); // Real service with mocked dependencies

// Now OrderService uses mocked dependencies
// Writes to order database occur, but payment and inventory are isolated
```

**Best Practices for Dependency Control:**
1. **Mock external services**: APIs, message queues, third-party systems
2. **Use in-memory alternatives**: For databases, caches, file systems
3. **Scope your tests**: Test one layer at a time
4. **Be explicit**: Make it clear which dependencies are real vs mocked
5. **Document side effects**: Clearly indicate when real systems are affected

### Choosing the Right Model

| Scenario | Recommended Model | Reason |
|----------|-------------------|--------|
| Development testing | Isolation | Fast, safe, repeatable |
| CI/CD pipeline | Isolation | No cleanup, fast feedback |
| Pre-production validation | Integration | Validates real behavior |
| Performance testing | Integration | Tests actual system performance |
| Testing with sensitive data | Isolation | Prevents data exposure |
| Testing cascading operations | Isolation | Controls scope of impact |

## Advanced Example

### Using Keyed Services for Multiple Implementations

To test different implementations, register them as keyed services in your DI container:

```csharp
// Register different implementations with unique keys
services.AddKeyedScoped<IDatabase, PostgresV1Database>("postgres-v1");
services.AddKeyedScoped<IDatabase, PostgresV2Database>("postgres-v2");
services.AddKeyedScoped<IDatabase, CosmosDatabase>("cosmos-db");
```

Then reference those keys in your simulation:

```csharp
public class DatabaseSimulationRunner
{
    public async Task<SimulationReport> RunAsync(IServiceProvider services)
    {
        var sim = SimulationRunner.Create(services)
            .For<IDatabase>()
            .WithResultType<List<Customer>>()
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

## Comprehensive Sample

A complete working sample demonstrating all simulation patterns is available:

**[ExperimentFramework.SimulationSample](../samples/ExperimentFramework.SimulationSample/)**

The sample includes:

1. **Output-Based Simulation** - Testing read operations safely
2. **Action-Based Isolation Model** - Testing writes with mocks
3. **Action-Based Integration Model** - Testing writes with shadow writes
4. **Dependency Control** - Preventing cascading effects

Run the sample:
```bash
cd samples/ExperimentFramework.SimulationSample
dotnet run
```

The sample demonstrates:
- When to use isolation vs integration models
- How to prevent unintended side effects
- Controlling dependencies to avoid cascading
- Safe testing patterns for write operations
- Real-world simulation scenarios

## See Also

- [ExperimentFramework README](../README.md)
- [Service Registration Safety](SERVICE_REGISTRATION_SAFETY.md)
- [Simulation Sample](../samples/ExperimentFramework.SimulationSample/README.md)
