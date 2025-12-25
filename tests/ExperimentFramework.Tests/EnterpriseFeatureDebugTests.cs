using ExperimentFramework.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Tests;

/// <summary>
/// Debug tests to isolate enterprise feature issues.
/// </summary>
public sealed class EnterpriseFeatureDebugTests
{
    private interface ITestService
    {
        Task<string> GetValueAsync();
        string GetValue();
    }

    private sealed class TestServiceA : ITestService
    {
        public async Task<string> GetValueAsync()
        {
            await Task.Delay(10);
            return "A";
        }

        public string GetValue() => "A";
    }

    private sealed class TestServiceB : ITestService
    {
        public async Task<string> GetValueAsync()
        {
            await Task.Delay(10);
            return "B";
        }

        public string GetValue() => "B";
    }

    private sealed class TestMetrics : IExperimentMetrics
    {
        public List<(string name, long value)> Counters { get; } = [];

        public void IncrementCounter(string name, long value = 1, params KeyValuePair<string, object>[] tags)
        {
            Counters.Add((name, value));
        }

        public void RecordHistogram(string name, double value, params KeyValuePair<string, object>[] tags) { }
        public void SetGauge(string name, double value, params KeyValuePair<string, object>[] tags) { }
        public void RecordSummary(string name, double value, params KeyValuePair<string, object>[] tags) { }
    }

    [Fact]
    public async Task Basic_async_method_with_metrics_works()
    {
        // Arrange
        var config = new Dictionary<string, string?>
        {
            ["Test"] = "A"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build());

        services.AddSingleton<TestServiceA>();
        services.AddSingleton<TestServiceB>();
        services.AddSingleton<ITestService, TestServiceA>();

        var metrics = new TestMetrics();
        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<ITestService>(c => c
                .UsingConfigurationKey("Test")
                .AddDefaultTrial<TestServiceA>("A")
                .AddTrial<TestServiceB>("B"))
            .WithMetrics(metrics)
            .UseDispatchProxy();

        services.AddExperimentFramework(experiments);
        var sp = services.BuildServiceProvider();

        // Act
        var service = sp.GetRequiredService<ITestService>();
        var result = await service.GetValueAsync();

        // Assert
        Assert.Equal("A", result);
        Assert.True(metrics.Counters.Count > 0, "Metrics should have been recorded");
    }

    [Fact]
    public void Basic_sync_method_with_metrics_works()
    {
        // Arrange
        var config = new Dictionary<string, string?>
        {
            ["Test"] = "A"
        };

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build());

        services.AddSingleton<TestServiceA>();
        services.AddSingleton<TestServiceB>();
        services.AddSingleton<ITestService, TestServiceA>();

        var metrics = new TestMetrics();
        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<ITestService>(c => c
                .UsingConfigurationKey("Test")
                .AddDefaultTrial<TestServiceA>("A")
                .AddTrial<TestServiceB>("B"))
            .WithMetrics(metrics)
            .UseDispatchProxy();

        services.AddExperimentFramework(experiments);
        var sp = services.BuildServiceProvider();

        // Act
        var service = sp.GetRequiredService<ITestService>();
        var result = service.GetValue();

        // Assert
        Assert.Equal("A", result);
        Assert.True(metrics.Counters.Count > 0, "Metrics should have been recorded");
    }
}
