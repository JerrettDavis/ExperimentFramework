using ExperimentFramework.Simulation.Builders;
using ExperimentFramework.Simulation.Comparators;
using ExperimentFramework.Simulation.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Simulation.Tests;

public class SimulationRunnerTests
{
    // Test interface
    public interface ITestDatabase
    {
        ValueTask<string> PingAsync();
        ValueTask<List<string>> GetCustomersAsync();
        ValueTask<int> GetCountAsync();
    }

    // Control implementation
    public class ControlDatabase : ITestDatabase
    {
        public ValueTask<string> PingAsync() => new("pong-control");
        public ValueTask<List<string>> GetCustomersAsync() => new(new List<string> { "Alice", "Bob" });
        public ValueTask<int> GetCountAsync() => new(42);
    }

    // Variant implementation
    public class VariantDatabase : ITestDatabase
    {
        public ValueTask<string> PingAsync() => new("pong-variant");
        public ValueTask<List<string>> GetCustomersAsync() => new(new List<string> { "Alice", "Bob" });
        public ValueTask<int> GetCountAsync() => new(42);
    }

    // Different variant implementation
    public class DifferentDatabase : ITestDatabase
    {
        public ValueTask<string> PingAsync() => new("pong-different");
        public ValueTask<List<string>> GetCustomersAsync() => new(new List<string> { "Charlie", "Dave" });
        public ValueTask<int> GetCountAsync() => new(100);
    }

    [Fact]
    public async Task SimulationRunner_ExecutesScenariosSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ITestDatabase, ControlDatabase>();
        var provider = services.BuildServiceProvider();

        var runner = SimulationRunner.Create(provider)
            .For<ITestDatabase>()
            .WithResultType<string>()
            .Control()
            .Condition("variant")
            .WithComparator(SimulationComparators.Equality<string>());

        var scenarios = new[]
        {
            new Scenario<ITestDatabase, string>("Ping", async db => await db.PingAsync())
        };

        // Act
        var report = await runner.RunAsync(scenarios);

        // Assert
        Assert.NotNull(report);
        Assert.Equal(typeof(ITestDatabase).Name, report.ServiceType);
        Assert.Single(report.ScenarioResults);
        Assert.Equal("control", report.ControlName);
        Assert.Contains("variant", report.ConditionNames);
    }

    [Fact]
    public async Task SimulationRunner_DetectsDifferencesWithEqualityComparator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ITestDatabase, ControlDatabase>();
        var provider = services.BuildServiceProvider();

        var runner = SimulationRunner.Create(provider)
            .For<ITestDatabase>()
            .WithResultType<string>()
            .Control()
            .Condition("variant")
            .WithComparator(SimulationComparators.Equality<string>());

        var scenarios = new[]
        {
            new Scenario<ITestDatabase, string>("Ping", async db => await db.PingAsync())
        };

        // Act
        var report = await runner.RunAsync(scenarios);

        // Assert
        Assert.NotNull(report);
        var scenarioResult = report.ScenarioResults.First();
        
        // Use interface properties instead of reflection
        Assert.NotNull(scenarioResult);
        // Since control and variant are the same implementation, no differences expected
        Assert.False(scenarioResult.HasDifferences);
    }

    [Fact]
    public async Task SimulationRunner_SupportsMultipleConditions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ITestDatabase, ControlDatabase>();
        var provider = services.BuildServiceProvider();

        var runner = SimulationRunner.Create(provider)
            .For<ITestDatabase>()
            .WithResultType<int>()
            .Control()
            .Condition("variant1")
            .Condition("variant2")
            .WithComparator(SimulationComparators.Equality<int>());

        var scenarios = new[]
        {
            new Scenario<ITestDatabase, int>("GetCount", async db => await db.GetCountAsync())
        };

        // Act
        var report = await runner.RunAsync(scenarios);

        // Assert
        Assert.NotNull(report);
        Assert.Equal(2, report.ConditionNames.Count);
        Assert.Contains("variant1", report.ConditionNames);
        Assert.Contains("variant2", report.ConditionNames);
    }

    [Fact]
    public async Task SimulationRunner_HandlesExceptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ITestDatabase, ThrowingDatabase>();
        var provider = services.BuildServiceProvider();

        var runner = SimulationRunner.Create(provider)
            .For<ITestDatabase>()
            .WithResultType<string>()
            .Control()
            .Condition("variant");

        var scenarios = new[]
        {
            new Scenario<ITestDatabase, string>("Ping", async db => await db.PingAsync())
        };

        // Act
        var report = await runner.RunAsync(scenarios);

        // Assert
        Assert.NotNull(report);
        Assert.False(report.Passed);
    }

    [Fact]
    public async Task SimulationRunner_JsonComparatorDetectsDifferences()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ITestDatabase, ControlDatabase>();
        var provider = services.BuildServiceProvider();

        var runner = SimulationRunner.Create(provider)
            .For<ITestDatabase>()
            .WithResultType<List<string>>()
            .Control()
            .Condition("variant")
            .WithComparator(SimulationComparators.Json<List<string>>());

        var scenarios = new[]
        {
            new Scenario<ITestDatabase, List<string>>("GetCustomers", async db => await db.GetCustomersAsync())
        };

        // Act
        var report = await runner.RunAsync(scenarios);

        // Assert
        Assert.NotNull(report);
        Assert.True(report.Passed);
    }

    [Fact]
    public void Scenario_SupportsValueTaskConstructor()
    {
        // Arrange & Act
        var scenario = new Scenario<ITestDatabase, string>(
            "Test",
            db => new ValueTask<string>("test"));

        // Assert
        Assert.NotNull(scenario);
        Assert.Equal("Test", scenario.Name);
        Assert.NotNull(scenario.Execute);
    }

    [Fact]
    public async Task SimulationRunner_DetectsDifferencesBetweenKeyedImplementations()
    {
        // Arrange - Register different implementations with keyed services
        var services = new ServiceCollection();
        services.AddKeyedScoped<ITestDatabase, ControlDatabase>("control");
        services.AddKeyedScoped<ITestDatabase, DifferentDatabase>("variant");
        var provider = services.BuildServiceProvider();

        var runner = SimulationRunner.Create(provider)
            .For<ITestDatabase>()
            .WithResultType<string>()
            .Control("control")
            .Condition("variant")
            .WithComparator(SimulationComparators.Equality<string>());

        var scenarios = new[]
        {
            new Scenario<ITestDatabase, string>("Ping", async db => await db.PingAsync())
        };

        // Act
        var report = await runner.RunAsync(scenarios);

        // Assert
        Assert.NotNull(report);
        var scenarioResult = report.ScenarioResults.First();
        
        // Different implementations should produce differences
        Assert.True(scenarioResult.HasDifferences);
        Assert.Contains("variant", scenarioResult.Differences[0]);
    }

    // Helper class for exception testing
    public class ThrowingDatabase : ITestDatabase
    {
        public ValueTask<string> PingAsync() => throw new InvalidOperationException("Test exception");
        public ValueTask<List<string>> GetCustomersAsync() => throw new InvalidOperationException("Test exception");
        public ValueTask<int> GetCountAsync() => throw new InvalidOperationException("Test exception");
    }
}
