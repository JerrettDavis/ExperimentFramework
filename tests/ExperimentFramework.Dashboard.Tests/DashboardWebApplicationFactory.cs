using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ExperimentFramework;
using ExperimentFramework.Admin;
using ExperimentFramework.Dashboard;
using ExperimentFramework.Dashboard.Abstractions;
using AdminExperimentInfo = ExperimentFramework.Admin.ExperimentInfo;
using AdminTrialInfo = ExperimentFramework.Admin.TrialInfo;

namespace ExperimentFramework.Dashboard.Tests;

/// <summary>
/// Test web application factory for integration testing.
/// </summary>
public class DashboardWebApplicationFactory : WebApplicationFactory<TestProgram>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Add a simple in-memory experiment registry for testing
            services.AddSingleton<IExperimentRegistry>(sp => new TestExperimentRegistry());

            // Register stub implementations for optional services so endpoints return 200
            // instead of 501 NotImplemented when the optional service isn't wired up.
            services.AddSingleton<IPluginManagementService>(new StubPluginManagementService());
            services.AddSingleton<ITargetingManagementService>(new StubTargetingManagementService());
            services.AddSingleton<IAnalyticsProvider>(new StubAnalyticsProvider());
            services.AddSingleton<IRolloutPersistenceBackplane>(new StubRolloutPersistenceBackplane());
        });
    }
}

/// <summary>
/// Simple test implementation of IExperimentRegistry.
/// </summary>
public class TestExperimentRegistry : IExperimentRegistry
{
    private readonly List<AdminExperimentInfo> _experiments = new()
    {
        new AdminExperimentInfo
        {
            Name = "test-experiment",
            ServiceType = typeof(ITestService),
            IsActive = true,
            Trials = new List<AdminTrialInfo>
            {
                new AdminTrialInfo { Key = "control", ImplementationType = typeof(TestControlService), IsControl = true },
                new AdminTrialInfo { Key = "variant-a", ImplementationType = typeof(TestVariantService), IsControl = false }
            }
        }
    };

    public IEnumerable<AdminExperimentInfo> GetAllExperiments() => _experiments;

    public AdminExperimentInfo? GetExperiment(string name) =>
        _experiments.FirstOrDefault(e => e.Name == name);
}

/// <summary>
/// Test marker interface for experiments.
/// </summary>
public interface ITestService { }

/// <summary>
/// Test control implementation.
/// </summary>
public class TestControlService : ITestService { }

/// <summary>
/// Test variant implementation.
/// </summary>
public class TestVariantService : ITestService { }

/// <summary>
/// No-op plugin management service for integration tests.
/// Ensures plugin endpoints return 200 with an empty list rather than 501.
/// </summary>
internal sealed class StubPluginManagementService : IPluginManagementService
{
    public Task<IReadOnlyList<PluginDescriptor>> GetLoadedPluginsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PluginDescriptor>>([]);

    public Task<IReadOnlyList<PluginDescriptor>> DiscoverPluginsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PluginDescriptor>>([]);

    public Task<IReadOnlyList<PluginDescriptor>> ReloadAllPluginsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PluginDescriptor>>([]);
}

/// <summary>
/// No-op targeting management service for integration tests.
/// Ensures targeting endpoints return 200 with empty rules rather than 501.
/// </summary>
internal sealed class StubTargetingManagementService : ITargetingManagementService
{
    public Task<IReadOnlyList<TargetingRuleDto>?> GetRulesAsync(
        string experimentName,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<TargetingRuleDto>?>([]);

    public Task SetRulesAsync(
        string experimentName,
        IReadOnlyList<TargetingRuleDto> rules,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<TargetingEvaluationResult> EvaluateAsync(
        string experimentName,
        IReadOnlyDictionary<string, object> context,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new TargetingEvaluationResult { Matched = false });
}

/// <summary>
/// No-op analytics provider for integration tests.
/// Ensures analytics endpoints return 200 with empty data rather than 501.
/// </summary>
internal sealed class StubAnalyticsProvider : IAnalyticsProvider
{
    public Task<IEnumerable<AssignmentEvent>> GetAssignmentsAsync(
        string experimentName,
        string? tenantId = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<AssignmentEvent>());

    public Task<IEnumerable<ExposureEvent>> GetExposuresAsync(
        string experimentName,
        string? tenantId = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<ExposureEvent>());

    public Task<IEnumerable<AnalysisSignalEvent>> GetAnalysisSignalsAsync(
        string experimentName,
        string? tenantId = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<AnalysisSignalEvent>());
}

/// <summary>
/// In-memory rollout persistence backplane for integration tests.
/// Ensures rollout endpoints return 200 with a pre-seeded config for
/// "test-experiment" rather than 503/404.
/// </summary>
internal sealed class StubRolloutPersistenceBackplane : IRolloutPersistenceBackplane
{
    private readonly Dictionary<string, RolloutConfiguration> _configs = new()
    {
        ["test-experiment"] = new RolloutConfiguration
        {
            ExperimentName = "test-experiment",
            Enabled = true,
            TargetVariant = "variant-a",
            Percentage = 10,
            Status = RolloutStatus.NotStarted,
            Stages =
            [
                new RolloutStageDto { Name = "Stage 1", Percentage = 10, Status = RolloutStageStatus.Pending }
            ]
        }
    };

    public Task<RolloutConfiguration?> GetRolloutConfigAsync(
        string experimentName,
        string? tenantId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_configs.TryGetValue(experimentName, out var cfg) ? cfg : null);

    public Task SaveRolloutConfigAsync(
        RolloutConfiguration config,
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        _configs[config.ExperimentName] = config;
        return Task.CompletedTask;
    }

    public Task DeleteRolloutConfigAsync(
        string experimentName,
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        _configs.Remove(experimentName);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RolloutConfiguration>> GetActiveRolloutsAsync(
        string? tenantId = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<RolloutConfiguration>>([.. _configs.Values]);
}
