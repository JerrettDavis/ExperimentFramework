using ExperimentFramework.Dashboard.Abstractions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using AdminExperimentInfo = ExperimentFramework.Admin.ExperimentInfo;
using AdminTrialInfo = ExperimentFramework.Admin.TrialInfo;
using DashboardExperimentInfo = ExperimentFramework.Dashboard.Abstractions.ExperimentInfo;

namespace ExperimentFramework.Dashboard.Api.Tests;

/// <summary>
/// Contract tests for all Dashboard.Api endpoint groups.
/// Uses a minimal in-process host wired via DashboardApiTestHost.
/// </summary>
public sealed class DashboardApiContractTests
{
    // ── /dashboard-api/experiments ────────────────────────────────────────────

    [Fact]
    public async Task GetExperiments_WithNoDataProvider_ReturnsOkWithEmptyExperiments()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/experiments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("experiments", body);
    }

    [Fact]
    public async Task GetExperiments_WithDataProvider_ReturnsExperimentList()
    {
        var provider = new Mock<IDashboardDataProvider>();
        provider.Setup(p => p.GetExperimentsAsync(null, default))
            .ReturnsAsync([
                new DashboardExperimentInfo { Name = "exp-1", IsActive = true },
                new DashboardExperimentInfo { Name = "exp-2", IsActive = false }
            ]);

        await using var host = new DashboardApiTestHost(dataProvider: provider.Object);
        var response = await host.Client.GetAsync("/dashboard-api/experiments");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("exp-1", body);
        Assert.Contains("exp-2", body);
    }

    [Fact]
    public async Task GetExperiment_WithExistingExperiment_ReturnsOk()
    {
        var provider = new Mock<IDashboardDataProvider>();
        provider.Setup(p => p.GetExperimentAsync("my-exp", null, default))
            .ReturnsAsync(new DashboardExperimentInfo { Name = "my-exp", IsActive = true });

        await using var host = new DashboardApiTestHost(dataProvider: provider.Object);
        var response = await host.Client.GetAsync("/dashboard-api/experiments/my-exp");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("my-exp", body);
    }

    [Fact]
    public async Task GetExperiment_WithNonexistentExperiment_ReturnsNotFound()
    {
        var provider = new Mock<IDashboardDataProvider>();
        provider.Setup(p => p.GetExperimentAsync("ghost", null, default))
            .ReturnsAsync((DashboardExperimentInfo?)null);

        await using var host = new DashboardApiTestHost(dataProvider: provider.Object);
        var response = await host.Client.GetAsync("/dashboard-api/experiments/ghost");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ToggleExperiment_WithNoRegistry_ReturnsNotFound()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.PostAsync("/dashboard-api/experiments/any-exp/toggle", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ToggleExperiment_WithMutableRegistry_TogglesState()
    {
        var registry = new MutableStubRegistry(
            new AdminExperimentInfo { Name = "togglable", IsActive = false });

        await using var host = new DashboardApiTestHost(registry: registry);
        var response = await host.Client.PostAsync("/dashboard-api/experiments/togglable/toggle", null);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("isActive", body);
    }

    [Fact]
    public async Task ToggleExperiment_WithReadOnlyRegistry_ReturnsBadRequest()
    {
        var registry = new StubRegistry(
            new AdminExperimentInfo { Name = "readonly-exp", IsActive = true });

        await using var host = new DashboardApiTestHost(registry: registry);
        var response = await host.Client.PostAsync("/dashboard-api/experiments/readonly-exp/toggle", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── /dashboard-api/analytics ──────────────────────────────────────────────

    [Fact]
    public async Task GetAnalyticsStatistics_WithNoProvider_ReturnsOkOrNotFoundOrNotImplemented()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/analytics/any-exp/statistics");
        // 501 when no IAnalyticsProvider is registered; 200/404 when one is present
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.NotImplemented,
            $"Unexpected status: {response.StatusCode}");
    }

    [Fact]
    public async Task GetUsageStats_WithNoRegistry_ReturnsOkWithEmpty()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/analytics/usage");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── /dashboard-api/governance ─────────────────────────────────────────────

    [Fact]
    public async Task GetGovernanceState_WithNoManager_ReturnsOkOrNotFound()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/governance/test-exp/state");
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Unexpected status: {response.StatusCode}");
    }

    [Fact]
    public async Task GetPendingApprovals_WithNoPersistence_ReturnsOk()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/governance/approvals/pending");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetGovernanceVersions_WithNoPersistence_ReturnsOk()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/governance/test-exp/versions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetGovernanceTransitions_WithNoPersistence_ReturnsOk()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/governance/test-exp/transitions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── /dashboard-api/configuration ─────────────────────────────────────────

    [Fact]
    public async Task GetConfigurationInfo_ReturnsOk()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/configuration/info");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetConfigurationYaml_ReturnsOk()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/configuration/yaml");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── /dashboard-api/plugins ────────────────────────────────────────────────

    [Fact]
    public async Task GetPlugins_WithNoPluginService_ReturnsNotImplemented()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/plugins");
        // Endpoint returns 501 when IPluginManagementService is not registered
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    // ── /dashboard-api/rollout ────────────────────────────────────────────────

    [Fact]
    public async Task GetRolloutStages_ReturnsOkOrNotFound()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/rollout/test-exp/stages");
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Unexpected status: {response.StatusCode}");
    }

    // ── /dashboard-api/targeting ──────────────────────────────────────────────

    [Fact]
    public async Task GetTargetingRules_WithNoService_ReturnsNotImplemented()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/targeting/test-exp/rules");
        // Endpoint returns 501 when ITargetingManagementService is not registered
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    // ── content-type ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetExperiments_ReturnsJsonContentType()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/experiments");
        var ct = response.Content.Headers.ContentType?.MediaType;
        Assert.Equal("application/json", ct);
    }

    [Fact]
    public async Task GetConfigurationInfo_ReturnsJsonContentType()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/configuration/info");
        var ct = response.Content.Headers.ContentType?.MediaType;
        Assert.Equal("application/json", ct);
    }

    // ── ActivateVariant ───────────────────────────────────────────────────────

    [Fact]
    public async Task ActivateVariant_WithNoRegistry_ReturnsNotFound()
    {
        await using var host = new DashboardApiTestHost();
        var content = JsonContent.Create(new { VariantKey = "variant-a" });
        var response = await host.Client.PostAsync("/dashboard-api/experiments/test-exp/activate-variant", content);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ActivateVariant_WithNonexistentVariant_ReturnsNotFound()
    {
        var registry = new MutableStubRegistry(
            new AdminExperimentInfo
            {
                Name = "exp-with-variants",
                IsActive = true,
                Trials = [new AdminTrialInfo { Key = "control", IsControl = true }]
            });

        await using var host = new DashboardApiTestHost(registry: registry);
        var content = JsonContent.Create(new { VariantKey = "ghost-variant" });
        var response = await host.Client.PostAsync(
            "/dashboard-api/experiments/exp-with-variants/activate-variant", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
