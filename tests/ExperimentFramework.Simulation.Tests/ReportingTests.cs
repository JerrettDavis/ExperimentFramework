using ExperimentFramework.Simulation.Builders;
using ExperimentFramework.Simulation.Models;
using ExperimentFramework.Simulation.Reporting;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Simulation.Tests;

public class ReportingTests
{
    public interface ITestService
    {
        ValueTask<string> ExecuteAsync();
    }

    public class TestServiceImpl : ITestService
    {
        public ValueTask<string> ExecuteAsync() => new("test-result");
    }

    [Fact]
    public async Task WriteJson_CreatesJsonFile()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceImpl>();
        var provider = services.BuildServiceProvider();

        var runner = SimulationRunner.Create(provider)
            .For<ITestService>()
            .AsRunnerFor<string>()
            .Control()
            .Condition("variant");

        var scenarios = new[]
        {
            new Scenario<ITestService, string>("Test", async svc => await svc.ExecuteAsync())
        };

        var report = await runner.RunAsync(scenarios);
        var tempPath = Path.Combine(Path.GetTempPath(), $"sim-test-{Guid.NewGuid()}.json");

        try
        {
            // Act
            report.WriteJson(tempPath);

            // Assert
            Assert.True(File.Exists(tempPath));
            var content = await File.ReadAllTextAsync(tempPath);
            Assert.NotEmpty(content);
            Assert.Contains("serviceType", content);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task WriteSummary_CreatesTextFile()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceImpl>();
        var provider = services.BuildServiceProvider();

        var runner = SimulationRunner.Create(provider)
            .For<ITestService>()
            .AsRunnerFor<string>()
            .Control()
            .Condition("variant");

        var scenarios = new[]
        {
            new Scenario<ITestService, string>("Test", async svc => await svc.ExecuteAsync())
        };

        var report = await runner.RunAsync(scenarios);
        var tempPath = Path.Combine(Path.GetTempPath(), $"sim-test-{Guid.NewGuid()}.txt");

        try
        {
            // Act
            report.WriteSummary(tempPath);

            // Assert
            Assert.True(File.Exists(tempPath));
            var content = await File.ReadAllTextAsync(tempPath);
            Assert.NotEmpty(content);
            Assert.Contains("Simulation Report", content);
            Assert.Contains("Scenario Results", content);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task WriteSummary_IncludesTimingInformation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ITestService, TestServiceImpl>();
        var provider = services.BuildServiceProvider();

        var runner = SimulationRunner.Create(provider)
            .For<ITestService>()
            .AsRunnerFor<string>()
            .Control();

        var scenarios = new[]
        {
            new Scenario<ITestService, string>("Test", async svc => await svc.ExecuteAsync())
        };

        var report = await runner.RunAsync(scenarios);
        var tempPath = Path.Combine(Path.GetTempPath(), $"sim-test-{Guid.NewGuid()}.txt");

        try
        {
            // Act
            report.WriteSummary(tempPath);

            // Assert
            var content = await File.ReadAllTextAsync(tempPath);
            Assert.Contains("Duration:", content);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
}
