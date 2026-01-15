using ExperimentFramework.Simulation.Builders;
using ExperimentFramework.Simulation.Comparators;
using ExperimentFramework.Simulation.Models;
using ExperimentFramework.SimulationSample;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("=================================================");
Console.WriteLine("ExperimentFramework.Simulation - Comprehensive Demo");
Console.WriteLine("=================================================\n");

// Scenario 1: OUTPUT-BASED (Read Operations) - Safe Simulation with Isolated Dependencies
await RunOutputBasedSimulation();

Console.WriteLine("\n" + new string('=', 80) + "\n");

// Scenario 2: ACTION-BASED (Write Operations) - Isolation Model with Mock Dependencies
await RunActionBasedSimulationIsolated();

Console.WriteLine("\n" + new string('=', 80) + "\n");

// Scenario 3: ACTION-BASED (Write Operations) - Integration Model with Real Shadow Writes
await RunActionBasedSimulationIntegrated();

Console.WriteLine("\n" + new string('=', 80) + "\n");

// Scenario 4: Mixed Operations - Demonstrating Dependency Control
await RunMixedOperationsWithDependencyControl();

Console.WriteLine("\n=================================================");
Console.WriteLine("All simulation scenarios completed!");
Console.WriteLine("=================================================");

// ============================================================
// SCENARIO 1: OUTPUT-BASED SIMULATION
// Testing read operations - comparing returned values
// Safe to run against real implementations
// ============================================================
async Task RunOutputBasedSimulation()
{
    Console.WriteLine("SCENARIO 1: Output-Based Simulation (Read Operations)");
    Console.WriteLine("------------------------------------------------------");
    Console.WriteLine("✓ Testing READ operations");
    Console.WriteLine("✓ Comparing returned values");
    Console.WriteLine("✓ Safe to run with real database implementations");
    Console.WriteLine();

    // Setup: Register different implementations with sample data
    var services = new ServiceCollection();
    
    // Control: Current production implementation
    services.AddKeyedScoped<ICustomerDatabase>("control", (sp, key) =>
    {
        var db = new RealCustomerDatabase();
        // Pre-populate with test data
        db.CreateCustomerAsync(new Customer(0, "Alice Johnson", "alice@example.com", 1000m)).AsTask().Wait();
        db.CreateCustomerAsync(new Customer(0, "Bob Smith", "bob@example.com", 2000m)).AsTask().Wait();
        return db;
    });
    
    // Condition: New implementation being tested
    services.AddKeyedScoped<ICustomerDatabase>("new-impl", (sp, key) =>
    {
        var db = new NewCustomerDatabase();
        // Pre-populate with same test data
        db.CreateCustomerAsync(new Customer(0, "Alice Johnson", "alice@example.com", 1000m)).AsTask().Wait();
        db.CreateCustomerAsync(new Customer(0, "Bob Smith", "bob@example.com", 2000m)).AsTask().Wait();
        return db;
    });
    
    var provider = services.BuildServiceProvider();

    // Configure simulation
    var sim = SimulationRunner.Create(provider)
        .For<ICustomerDatabase>()
        .WithResultType<List<Customer>>()
        .Control("control")
        .Condition("new-impl")
        .WithComparator(SimulationComparators.Json<List<Customer>>())
        .ReturnControlResult(); // Safe: always return control result

    // Define read-only scenarios
    var scenarios = new[]
    {
        new Scenario<ICustomerDatabase, List<Customer>>(
            "GetAllCustomers",
            async db => await db.GetAllCustomersAsync())
    };

    // Execute simulation
    var report = await sim.RunAsync(scenarios);
    
    // Display results
    Console.WriteLine($"\nResults: {(report.Passed ? "✓ PASSED" : "✗ FAILED")}");
    Console.WriteLine($"Summary: {report.Summary}");
    
    if (!report.Passed)
    {
        foreach (var scenario in report.ScenarioResults)
        {
            if (scenario.HasDifferences)
            {
                Console.WriteLine($"\nDifferences in {scenario.ScenarioName}:");
                foreach (var diff in scenario.Differences)
                {
                    Console.WriteLine($"  - {diff}");
                }
            }
        }
    }
}

// ============================================================
// SCENARIO 2: ACTION-BASED SIMULATION - ISOLATION MODEL
// Testing write operations WITHOUT affecting real systems
// Uses mock implementations to prevent side effects
// ============================================================
async Task RunActionBasedSimulationIsolated()
{
    Console.WriteLine("SCENARIO 2: Action-Based Simulation - ISOLATION MODEL");
    Console.WriteLine("------------------------------------------------------");
    Console.WriteLine("✓ Testing WRITE operations");
    Console.WriteLine("✓ Using MOCK implementations");
    Console.WriteLine("✓ NO real database side effects");
    Console.WriteLine("✓ Safe for testing before production deployment");
    Console.WriteLine();

    // Setup: Register MOCK implementations to prevent real writes
    var services = new ServiceCollection();
    
    // Control: Mock of current implementation
    services.AddKeyedScoped<ICustomerDatabase>("control", (sp, key) => new MockCustomerDatabase());
    
    // Condition: Mock of new implementation
    services.AddKeyedScoped<ICustomerDatabase>("new-impl", (sp, key) => new MockCustomerDatabase());
    
    var provider = services.BuildServiceProvider();

    // Configure simulation for write operations
    var sim = SimulationRunner.Create(provider)
        .For<ICustomerDatabase>()
        .WithResultType<int>()
        .Control("control")
        .Condition("new-impl")
        .WithComparator(SimulationComparators.Equality<int>())
        .ReturnControlResult(); // Return control result (from mock)

    // Define write scenarios
    var scenarios = new[]
    {
        new Scenario<ICustomerDatabase, int>(
            "CreateCustomer",
            async db => await db.CreateCustomerAsync(
                new Customer(0, "Charlie Brown", "charlie@example.com", 1500m)))
    };

    // Execute simulation
    Console.WriteLine("Executing write operations (mocked)...");
    var report = await sim.RunAsync(scenarios);
    
    // Display results
    Console.WriteLine($"\nResults: {(report.Passed ? "✓ PASSED" : "✗ FAILED")}");
    Console.WriteLine($"Summary: {report.Summary}");
    Console.WriteLine("\n✓ No real databases were modified");
    Console.WriteLine("✓ Safe to run repeatedly without cleanup");
}

// ============================================================
// SCENARIO 3: ACTION-BASED SIMULATION - INTEGRATION MODEL
// Testing write operations WITH real shadow writes
// Demonstrates controlled real-world testing
// ============================================================
async Task RunActionBasedSimulationIntegrated()
{
    Console.WriteLine("SCENARIO 3: Action-Based Simulation - INTEGRATION MODEL");
    Console.WriteLine("------------------------------------------------------");
    Console.WriteLine("✓ Testing WRITE operations");
    Console.WriteLine("✓ Using REAL implementations");
    Console.WriteLine("⚠ REAL database side effects will occur");
    Console.WriteLine("✓ Useful for final validation before production");
    Console.WriteLine();

    // Setup: Register REAL implementations for shadow writes
    var services = new ServiceCollection();
    
    // Control: Real current implementation
    services.AddKeyedScoped<ICustomerDatabase>("control", (sp, key) => new RealCustomerDatabase());
    
    // Condition: Real new implementation
    services.AddKeyedScoped<ICustomerDatabase>("new-impl", (sp, key) => new NewCustomerDatabase());
    
    var provider = services.BuildServiceProvider();

    // Configure simulation for real shadow writes
    var sim = SimulationRunner.Create(provider)
        .For<ICustomerDatabase>()
        .WithResultType<int>()
        .Control("control")
        .Condition("new-impl")
        .WithComparator(SimulationComparators.Equality<int>())
        .ReturnControlResult(); // Return control result (from real DB)

    // Define write scenarios
    var scenarios = new[]
    {
        new Scenario<ICustomerDatabase, int>(
            "CreateCustomer_ShadowWrite",
            async db => await db.CreateCustomerAsync(
                new Customer(0, "Diana Prince", "diana@example.com", 3000m)))
    };

    // Execute simulation with real writes
    Console.WriteLine("Executing write operations (REAL shadow writes)...");
    var report = await sim.RunAsync(scenarios);
    
    // Display results
    Console.WriteLine($"\nResults: {(report.Passed ? "✓ PASSED" : "✗ FAILED")}");
    Console.WriteLine($"Summary: {report.Summary}");
    Console.WriteLine("\n⚠ Both control and new implementations wrote to their databases");
    Console.WriteLine("⚠ Cleanup may be required");
}

// ============================================================
// SCENARIO 4: MIXED OPERATIONS WITH DEPENDENCY CONTROL
// Demonstrates controlling dependencies to prevent cascading
// ============================================================
async Task RunMixedOperationsWithDependencyControl()
{
    Console.WriteLine("SCENARIO 4: Mixed Operations with Dependency Control");
    Console.WriteLine("------------------------------------------------------");
    Console.WriteLine("✓ Mixing read and write operations");
    Console.WriteLine("✓ Demonstrating dependency isolation strategies");
    Console.WriteLine("✓ Preventing unintended cascade effects");
    Console.WriteLine();

    // Strategy 1: Use mocks for write-heavy dependencies
    // Strategy 2: Use real implementations for read-only dependencies
    
    var services = new ServiceCollection();
    
    // Register mock implementations for controlled testing
    services.AddKeyedScoped<ICustomerDatabase>("control", (sp, key) =>
    {
        var db = new MockCustomerDatabase();
        // Pre-populate with test data
        db.CreateCustomerAsync(new Customer(0, "Eve Adams", "eve@example.com", 2500m)).AsTask().Wait();
        return db;
    });
    
    services.AddKeyedScoped<ICustomerDatabase>("new-impl", (sp, key) =>
    {
        var db = new MockCustomerDatabase();
        // Pre-populate with test data
        db.CreateCustomerAsync(new Customer(0, "Eve Adams", "eve@example.com", 2500m)).AsTask().Wait();
        return db;
    });
    
    var provider = services.BuildServiceProvider();

    // Test read operation first
    Console.WriteLine("Phase 1: Testing read operation");
    var readSim = SimulationRunner.Create(provider)
        .For<ICustomerDatabase>()
        .WithResultType<Customer?>()
        .Control("control")
        .Condition("new-impl")
        .WithComparator(SimulationComparators.Json<Customer?>())
        .ReturnControlResult();

    var readScenarios = new[]
    {
        new Scenario<ICustomerDatabase, Customer?>(
            "GetCustomer",
            async db => await db.GetCustomerAsync(1))
    };

    var readReport = await readSim.RunAsync(readScenarios);
    Console.WriteLine($"  Read test: {(readReport.Passed ? "✓ PASSED" : "✗ FAILED")}");

    // Test write operation with controlled dependencies
    Console.WriteLine("\nPhase 2: Testing write operation (with mocks)");
    var writeSim = SimulationRunner.Create(provider)
        .For<ICustomerDatabase>()
        .WithResultType<int>()
        .Control("control")
        .Condition("new-impl")
        .WithComparator(SimulationComparators.Equality<int>())
        .ReturnControlResult();

    var writeScenarios = new[]
    {
        new Scenario<ICustomerDatabase, int>(
            "UpdateCustomer",
            async db =>
            {
                var customer = await db.GetCustomerAsync(1);
                if (customer != null)
                {
                    var updated = customer with { Balance = customer.Balance + 500m };
                    await db.UpdateCustomerAsync(updated);
                    return updated.Id;
                }
                return 0;
            })
    };

    var writeReport = await writeSim.RunAsync(writeScenarios);
    Console.WriteLine($"  Write test: {(writeReport.Passed ? "✓ PASSED" : "✗ FAILED")}");
    
    Console.WriteLine("\n✓ Dependency isolation prevented cascading to other systems");
    Console.WriteLine("✓ Only controlled mock databases were affected");
}
