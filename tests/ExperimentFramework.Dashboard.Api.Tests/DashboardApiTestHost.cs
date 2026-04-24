using ExperimentFramework.Dashboard.Abstractions;
using ExperimentFramework.Dashboard.Api;
using ExperimentFramework.Governance;
using ExperimentFramework.Governance.Persistence;
using ExperimentFramework.Governance.Persistence.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using AdminExperimentInfo = ExperimentFramework.Admin.ExperimentInfo;
using IExperimentRegistry = ExperimentFramework.Admin.IExperimentRegistry;
using IMutableExperimentRegistry = ExperimentFramework.Admin.IMutableExperimentRegistry;

namespace ExperimentFramework.Dashboard.Api.Tests;

/// <summary>
/// Factory that spins up a minimal in-process host wiring up all Dashboard.Api endpoints.
/// </summary>
public sealed class DashboardApiTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;
    public HttpClient Client { get; }

    public DashboardApiTestHost(
        IExperimentRegistry? registry = null,
        IDashboardDataProvider? dataProvider = null,
        ExperimentFramework.Governance.ILifecycleManager? lifecycleManager = null,
        IGovernancePersistenceBackplane? persistenceBackplane = null,
        IAnalyticsProvider? analyticsProvider = null,
        IRolloutPersistenceBackplane? rolloutPersistence = null,
        ITargetingManagementService? targetingManagement = null,
        IPluginManagementService? pluginManagement = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();

        if (registry != null)
            builder.Services.AddSingleton(registry);
        if (dataProvider != null)
            builder.Services.AddSingleton(dataProvider);
        if (lifecycleManager != null)
            builder.Services.AddSingleton<ExperimentFramework.Governance.ILifecycleManager>(lifecycleManager);
        if (persistenceBackplane != null)
            builder.Services.AddSingleton(persistenceBackplane);
        if (analyticsProvider != null)
            builder.Services.AddSingleton(analyticsProvider);
        if (rolloutPersistence != null)
            builder.Services.AddSingleton(rolloutPersistence);
        if (targetingManagement != null)
            builder.Services.AddSingleton(targetingManagement);
        if (pluginManagement != null)
            builder.Services.AddSingleton(pluginManagement);

        _app = builder.Build();
        _app.UseRouting();
        _app.MapDashboardApi("/dashboard-api");
        _app.Start();

        Client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync();
    }
}

/// <summary>
/// Simple in-memory implementation of IExperimentRegistry for tests.
/// </summary>
public sealed class StubRegistry : IExperimentRegistry
{
    private readonly List<AdminExperimentInfo> _experiments;

    public StubRegistry(params AdminExperimentInfo[] experiments)
    {
        _experiments = [.. experiments];
    }

    public IEnumerable<AdminExperimentInfo> GetAllExperiments() => _experiments;
    public AdminExperimentInfo? GetExperiment(string name) =>
        _experiments.FirstOrDefault(e => e.Name == name);
}

/// <summary>
/// Mutable registry for toggle tests.
/// </summary>
public sealed class MutableStubRegistry : IMutableExperimentRegistry
{
    private readonly List<AdminExperimentInfo> _experiments;

    public MutableStubRegistry(params AdminExperimentInfo[] experiments)
    {
        _experiments = [.. experiments];
    }

    public IEnumerable<AdminExperimentInfo> GetAllExperiments() => _experiments;
    public AdminExperimentInfo? GetExperiment(string name) =>
        _experiments.FirstOrDefault(e => e.Name == name);
    public void SetExperimentActive(string name, bool isActive)
    {
        var exp = GetExperiment(name);
        if (exp != null) exp.IsActive = isActive;
    }
    public void SetRolloutPercentage(string name, int percentage) { }
}
