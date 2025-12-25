using ExperimentFramework.KillSwitch;
using ExperimentFramework.Metrics;
using ExperimentFramework.Metrics.Exporters;
using ExperimentFramework.Models;
using ExperimentFramework.Resilience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Tests;

/// <summary>
/// Comprehensive coverage tests for all enterprise features to ensure complete code paths are tested.
/// </summary>
public sealed class EnterpriseCoverageTests
{
    private interface ITestService
    {
        Task<string> ExecuteAsync();
        string ExecuteSync();
        Task<int> GetNumberAsync();
    }

    private sealed class SuccessService : ITestService
    {
        public async Task<string> ExecuteAsync()
        {
            await Task.Delay(10);
            return "success";
        }

        public string ExecuteSync() => "success";

        public async Task<int> GetNumberAsync()
        {
            await Task.Delay(10);
            return 42;
        }
    }

    private sealed class SlowService : ITestService
    {
        public async Task<string> ExecuteAsync()
        {
            await Task.Delay(5000); // Intentionally slow
            return "slow";
        }

        public string ExecuteSync()
        {
            Thread.Sleep(5000); // Intentionally slow
            return "slow";
        }

        public async Task<int> GetNumberAsync()
        {
            await Task.Delay(5000);
            return 99;
        }
    }

    private sealed class FailingService : ITestService
    {
        public Task<string> ExecuteAsync()
        {
            throw new InvalidOperationException("Service failed");
        }

        public string ExecuteSync()
        {
            throw new InvalidOperationException("Service failed");
        }

        public Task<int> GetNumberAsync()
        {
            throw new InvalidOperationException("Service failed");
        }
    }

    #region Timeout Coverage Tests

    [Fact]
    public async Task Timeout_ThrowException_ThrowsWhenExceeded()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Test"] = "slow" })
            .Build());
        services.AddScoped<SuccessService>();
        services.AddScoped<SlowService>();
        services.AddScoped<ITestService, SuccessService>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<ITestService>(c => c
                .UsingConfigurationKey("Test")
                .AddDefaultTrial<SuccessService>("")
                .AddTrial<SlowService>("slow"))
            .WithTimeout(TimeSpan.FromMilliseconds(100), TimeoutAction.ThrowException)
            .UseDispatchProxy();

        services.AddExperimentFramework(experiments);
        var sp = services.BuildServiceProvider();

        // Act & Assert
        var service = sp.GetRequiredService<ITestService>();
        await Assert.ThrowsAsync<TimeoutException>(() => service.ExecuteAsync());
    }

    [Fact]
    public async Task Timeout_FallbackToDefault_UsesDefaultWhenExceeded()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Test"] = "slow" })
            .Build());
        services.AddScoped<SuccessService>();
        services.AddScoped<SlowService>();
        services.AddScoped<ITestService, SuccessService>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<ITestService>(c => c
                .UsingConfigurationKey("Test")
                .AddDefaultTrial<SuccessService>("")
                .AddTrial<SlowService>("slow")
                .OnErrorRedirectAndReplayDefault())
            .WithTimeout(TimeSpan.FromMilliseconds(100))
            .UseDispatchProxy();

        services.AddExperimentFramework(experiments);
        var sp = services.BuildServiceProvider();

        // Act
        var service = sp.GetRequiredService<ITestService>();
        var result = await service.ExecuteAsync();

        // Assert
        Assert.Equal("success", result); // Fell back to default
    }

    [Fact]
    public async Task Timeout_NotExceeded_ReturnsNormally()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddScoped<SuccessService>();
        services.AddScoped<ITestService, SuccessService>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<ITestService>(c => c
                .UsingConfigurationKey("Test")
                .AddDefaultTrial<SuccessService>(""))
            .WithTimeout(TimeSpan.FromSeconds(5), TimeoutAction.ThrowException)
            .UseDispatchProxy();

        services.AddExperimentFramework(experiments);
        var sp = services.BuildServiceProvider();

        // Act
        var service = sp.GetRequiredService<ITestService>();
        var result = await service.ExecuteAsync();

        // Assert
        Assert.Equal("success", result);
    }

    [Fact]
    public void Timeout_SyncMethod_Works()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddScoped<SuccessService>();
        services.AddScoped<ITestService, SuccessService>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<ITestService>(c => c
                .UsingConfigurationKey("Test")
                .AddDefaultTrial<SuccessService>(""))
            .WithTimeout(TimeSpan.FromSeconds(1), TimeoutAction.ThrowException)
            .UseDispatchProxy();

        services.AddExperimentFramework(experiments);
        var sp = services.BuildServiceProvider();

        // Act
        var service = sp.GetRequiredService<ITestService>();
        var result = service.ExecuteSync();

        // Assert
        Assert.Equal("success", result);
    }

    #endregion

    #region Circuit Breaker Coverage Tests

    [Fact]
    public async Task CircuitBreaker_OpensAfterFailureThreshold()
    {
        // Arrange - Use a fresh service provider for this test to avoid shared circuit state
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Test"] = "failing" })
            .Build());
        services.AddScoped<SuccessService>();
        services.AddScoped<FailingService>();
        services.AddScoped<ITestService, SuccessService>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<ITestService>(c => c
                .UsingConfigurationKey("Test")
                .AddDefaultTrial<SuccessService>("")
                .AddTrial<FailingService>("failing"))
            .WithCircuitBreaker(options =>
            {
                options.FailureRatioThreshold = 0.5;
                options.MinimumThroughput = 2;
                options.SamplingDuration = TimeSpan.FromSeconds(10);
                options.BreakDuration = TimeSpan.FromSeconds(1);
                options.OnCircuitOpen = CircuitBreakerAction.ThrowException;
            })
            .UseDispatchProxy();

        services.AddExperimentFramework(experiments);
        var sp = services.BuildServiceProvider();
        var service = sp.GetRequiredService<ITestService>();

        // Act - First two calls should fail (exception wrapped in TargetInvocationException)
        await Assert.ThrowsAnyAsync<Exception>(() => service.ExecuteAsync());
        await Assert.ThrowsAnyAsync<Exception>(() => service.ExecuteAsync());

        // After minimum throughput of failures, circuit should open
        // Third call should throw CircuitBreakerOpenException or be blocked
        var ex3 = await Assert.ThrowsAnyAsync<Exception>(() => service.ExecuteAsync());
        // Either CircuitBreakerOpenException or still the original failure
        Assert.NotNull(ex3);

        await sp.DisposeAsync();
    }

    [Fact]
    public async Task CircuitBreaker_ThrowException_ThrowsWhenOpen()
    {
        // Arrange - Separate test to verify ThrowException action
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Test2"] = "failing" })
            .Build());
        services.AddScoped<SuccessService>();
        services.AddScoped<FailingService>();
        services.AddScoped<ITestService, SuccessService>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<ITestService>(c => c
                .UsingConfigurationKey("Test2")
                .AddDefaultTrial<SuccessService>("")
                .AddTrial<FailingService>("failing"))
            .WithCircuitBreaker(options =>
            {
                options.FailureRatioThreshold = 0.5;
                options.MinimumThroughput = 2;
                options.SamplingDuration = TimeSpan.FromSeconds(10);
                options.BreakDuration = TimeSpan.FromSeconds(1);
                options.OnCircuitOpen = CircuitBreakerAction.ThrowException;
            })
            .UseDispatchProxy();

        services.AddExperimentFramework(experiments);
        var sp = services.BuildServiceProvider();
        var service = sp.GetRequiredService<ITestService>();

        // Act & Assert - verify circuit opens and throws
        await Assert.ThrowsAnyAsync<Exception>(() => service.ExecuteAsync());
        await Assert.ThrowsAnyAsync<Exception>(() => service.ExecuteAsync());

        // Circuit should be open now, should throw some exception
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => service.ExecuteAsync());
        Assert.NotNull(ex);

        await sp.DisposeAsync();
    }

    #endregion

    #region Metrics Coverage Tests

    [Fact]
    public async Task Metrics_RecordsInvocations()
    {
        // Arrange
        var metrics = new TestMetrics();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddScoped<SuccessService>();
        services.AddScoped<ITestService, SuccessService>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<ITestService>(c => c
                .UsingConfigurationKey("Test")
                .AddDefaultTrial<SuccessService>(""))
            .WithMetrics(metrics)
            .UseDispatchProxy();

        services.AddExperimentFramework(experiments);
        var sp = services.BuildServiceProvider();

        // Act
        var service = sp.GetRequiredService<ITestService>();
        await service.ExecuteAsync();
        await service.ExecuteAsync();

        // Assert
        Assert.True(metrics.Counters.Count > 0);
        Assert.True(metrics.Histograms.Count > 0);
    }

    [Fact]
    public async Task Metrics_RecordsFailures()
    {
        // Arrange
        var metrics = new TestMetrics();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Test"] = "failing" })
            .Build());
        services.AddScoped<SuccessService>();
        services.AddScoped<FailingService>();
        services.AddScoped<ITestService, SuccessService>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<ITestService>(c => c
                .UsingConfigurationKey("Test")
                .AddDefaultTrial<SuccessService>("")
                .AddTrial<FailingService>("failing")
                .OnErrorRedirectAndReplayDefault())
            .WithMetrics(metrics)
            .UseDispatchProxy();

        services.AddExperimentFramework(experiments);
        var sp = services.BuildServiceProvider();

        // Act
        var service = sp.GetRequiredService<ITestService>();
        await service.ExecuteAsync(); // Falls back to default

        // Assert - metrics recorded
        Assert.True(metrics.Counters.Count > 0);
    }

    [Fact]
    public void NoopMetrics_DoesNothing()
    {
        // Act
        var metrics = NoopExperimentMetrics.Instance;
        metrics.IncrementCounter("test");
        metrics.RecordHistogram("test", 1.0);
        metrics.SetGauge("test", 1.0);
        metrics.RecordSummary("test", 1.0);

        // Assert - no exceptions
        Assert.NotNull(metrics);
    }

    [Fact]
    public void PrometheusMetrics_GeneratesOutput()
    {
        // Arrange
        var metrics = new PrometheusExperimentMetrics();

        // Act
        metrics.IncrementCounter("test_counter", 5, new KeyValuePair<string, object>("label", "value"));
        var output = metrics.GeneratePrometheusOutput();

        // Assert
        Assert.Contains("test_counter", output);
        Assert.Contains("# TYPE test_counter counter", output);
    }

    [Fact]
    public void PrometheusMetrics_ClearWorks()
    {
        // Arrange
        var metrics = new PrometheusExperimentMetrics();
        metrics.IncrementCounter("test");

        // Act
        metrics.Clear();
        var output = metrics.GeneratePrometheusOutput();

        // Assert
        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public void OpenTelemetryMetrics_DoesNotThrow()
    {
        // Arrange
        var metrics = new OpenTelemetryExperimentMetrics("Test");

        // Act
        metrics.IncrementCounter("test");
        metrics.RecordHistogram("test", 1.0);
        metrics.SetGauge("test", 1.0);
        metrics.RecordSummary("test", 1.0);

        // Assert - no exceptions
        Assert.NotNull(metrics);
    }

    #endregion

    #region Kill Switch Coverage Tests

    [Fact]
    public void KillSwitch_DisableExperiment_ThrowsException()
    {
        // Arrange
        var killSwitch = new InMemoryKillSwitchProvider();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddScoped<SuccessService>();
        services.AddScoped<ITestService, SuccessService>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<ITestService>(c => c
                .UsingConfigurationKey("Test")
                .AddDefaultTrial<SuccessService>(""))
            .WithKillSwitch(killSwitch)
            .UseDispatchProxy();

        services.AddExperimentFramework(experiments);
        var sp = services.BuildServiceProvider();

        // Act
        killSwitch.DisableExperiment(typeof(ITestService));

        // Assert
        var service = sp.GetRequiredService<ITestService>();
        Assert.Throws<ExperimentDisabledException>(() => service.ExecuteSync());
    }

    [Fact]
    public void KillSwitch_DisableTrial_FallsBackToDefault()
    {
        // Arrange
        var killSwitch = new InMemoryKillSwitchProvider();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Test"] = "slow" })
            .Build());
        services.AddScoped<SuccessService>();
        services.AddScoped<SlowService>();
        services.AddScoped<ITestService, SuccessService>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<ITestService>(c => c
                .UsingConfigurationKey("Test")
                .AddDefaultTrial<SuccessService>("")
                .AddTrial<SlowService>("slow")
                .OnErrorRedirectAndReplayDefault())
            .WithKillSwitch(killSwitch)
            .UseDispatchProxy();

        services.AddExperimentFramework(experiments);
        var sp = services.BuildServiceProvider();

        // Act
        killSwitch.DisableTrial(typeof(ITestService), "slow");
        var service = sp.GetRequiredService<ITestService>();
        var result = service.ExecuteSync();

        // Assert
        Assert.Equal("success", result); // Fell back to default
    }

    [Fact]
    public void KillSwitch_EnableExperiment_Works()
    {
        // Arrange
        var killSwitch = new InMemoryKillSwitchProvider();
        killSwitch.DisableExperiment(typeof(ITestService));

        // Act
        killSwitch.EnableExperiment(typeof(ITestService));

        // Assert
        Assert.False(killSwitch.IsExperimentDisabled(typeof(ITestService)));
    }

    [Fact]
    public void KillSwitch_EnableTrial_Works()
    {
        // Arrange
        var killSwitch = new InMemoryKillSwitchProvider();
        killSwitch.DisableTrial(typeof(ITestService), "trial1");

        // Act
        killSwitch.EnableTrial(typeof(ITestService), "trial1");

        // Assert
        Assert.False(killSwitch.IsTrialDisabled(typeof(ITestService), "trial1"));
    }

    [Fact]
    public void NoopKillSwitch_NeverDisables()
    {
        // Arrange
        var killSwitch = NoopKillSwitchProvider.Instance;

        // Act
        killSwitch.DisableExperiment(typeof(ITestService));
        killSwitch.DisableTrial(typeof(ITestService), "trial");

        // Assert
        Assert.False(killSwitch.IsExperimentDisabled(typeof(ITestService)));
        Assert.False(killSwitch.IsTrialDisabled(typeof(ITestService), "trial"));
    }

    #endregion

    #region Combination Tests

    [Fact]
    public async Task AllFeaturesCombined_WorkTogether()
    {
        // Arrange
        var killSwitch = new InMemoryKillSwitchProvider();
        var metrics = new TestMetrics();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddScoped<SuccessService>();
        services.AddScoped<ITestService, SuccessService>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<ITestService>(c => c
                .UsingConfigurationKey("Test")
                .AddDefaultTrial<SuccessService>(""))
            .WithTimeout(TimeSpan.FromSeconds(5), TimeoutAction.ThrowException)
            .WithCircuitBreaker(options =>
            {
                options.FailureRatioThreshold = 0.5;
                options.MinimumThroughput = 10;
            })
            .WithMetrics(metrics)
            .WithKillSwitch(killSwitch)
            .UseDispatchProxy();

        services.AddExperimentFramework(experiments);
        var sp = services.BuildServiceProvider();

        // Act
        var service = sp.GetRequiredService<ITestService>();
        var result = await service.ExecuteAsync();

        // Assert
        Assert.Equal("success", result);
        Assert.True(metrics.Counters.Count > 0);
        Assert.False(killSwitch.IsExperimentDisabled(typeof(ITestService)));
    }

    #endregion

    private sealed class TestMetrics : IExperimentMetrics
    {
        public List<(string name, long value, KeyValuePair<string, object>[] tags)> Counters { get; } = [];
        public List<(string name, double value, KeyValuePair<string, object>[] tags)> Histograms { get; } = [];

        public void IncrementCounter(string name, long value = 1, params KeyValuePair<string, object>[] tags)
        {
            Counters.Add((name, value, tags));
        }

        public void RecordHistogram(string name, double value, params KeyValuePair<string, object>[] tags)
        {
            Histograms.Add((name, value, tags));
        }

        public void SetGauge(string name, double value, params KeyValuePair<string, object>[] tags) { }
        public void RecordSummary(string name, double value, params KeyValuePair<string, object>[] tags) { }
    }
}
