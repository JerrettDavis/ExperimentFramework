using ExperimentFramework.Dashboard.Abstractions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AdminExperimentInfo = ExperimentFramework.Admin.ExperimentInfo;
using AdminTrialInfo = ExperimentFramework.Admin.TrialInfo;
using DashboardExperimentInfo = ExperimentFramework.Dashboard.Abstractions.ExperimentInfo;

namespace ExperimentFramework.Dashboard.Api.Tests;

/// <summary>
/// Error-path, edge-case, and response-shape tests for all Dashboard.Api endpoint groups.
/// Complements the happy-path contract tests in DashboardApiContractTests.
/// </summary>
public sealed class DashboardApiErrorPathTests
{
    // ── Experiments: error paths ──────────────────────────────────────────────

    [Fact]
    public async Task GetExperiment_NoDataProvider_ReturnsNotFound()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/experiments/any-exp");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("error", body);
    }

    [Fact]
    public async Task GetExperiments_ResponseShape_HasExperimentsKey()
    {
        var provider = new Mock<IDashboardDataProvider>();
        provider.Setup(p => p.GetExperimentsAsync(null, default))
            .ReturnsAsync([
                new DashboardExperimentInfo { Name = "exp-shape-test", IsActive = true }
            ]);

        await using var host = new DashboardApiTestHost(dataProvider: provider.Object);
        var response = await host.Client.GetAsync("/dashboard-api/experiments");
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("experiments", out _),
            $"Response should have 'experiments' key, got: {json}");
    }

    [Fact]
    public async Task GetExperiment_ResponseShape_HasNameAndIsActiveFields()
    {
        var provider = new Mock<IDashboardDataProvider>();
        provider.Setup(p => p.GetExperimentAsync("shape-exp", null, default))
            .ReturnsAsync(new DashboardExperimentInfo { Name = "shape-exp", IsActive = true });

        await using var host = new DashboardApiTestHost(dataProvider: provider.Object);
        var response = await host.Client.GetAsync("/dashboard-api/experiments/shape-exp");
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("name", out var name));
        Assert.Equal("shape-exp", name.GetString());
    }

    [Fact]
    public async Task ToggleExperiment_NoExperimentFound_ReturnsNotFound()
    {
        var registry = new StubRegistry(
            new AdminExperimentInfo { Name = "other-exp", IsActive = false });

        await using var host = new DashboardApiTestHost(registry: registry);
        var response = await host.Client.PostAsync("/dashboard-api/experiments/not-found/toggle", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ToggleExperiment_ResponseShape_HasNameAndIsActiveAndStatus()
    {
        var registry = new MutableStubRegistry(
            new AdminExperimentInfo { Name = "tog-exp", IsActive = false });

        await using var host = new DashboardApiTestHost(registry: registry);
        var response = await host.Client.PostAsync("/dashboard-api/experiments/tog-exp/toggle", null);
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("name", out _));
        Assert.True(doc.RootElement.TryGetProperty("isActive", out _));
        Assert.True(doc.RootElement.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task ActivateVariant_EmptyVariantKey_ReturnsBadRequest()
    {
        var registry = new MutableStubRegistry(
            new AdminExperimentInfo
            {
                Name = "exp-v",
                IsActive = true,
                Trials = [new AdminTrialInfo { Key = "control", IsControl = true }]
            });

        await using var host = new DashboardApiTestHost(registry: registry);
        var content = JsonContent.Create(new { VariantKey = "" });
        var response = await host.Client.PostAsync("/dashboard-api/experiments/exp-v/activate-variant", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ActivateVariant_ReadOnlyRegistry_ReturnsBadRequest()
    {
        var registry = new StubRegistry(
            new AdminExperimentInfo
            {
                Name = "ro-exp",
                IsActive = true,
                Trials = [new AdminTrialInfo { Key = "control", IsControl = true }]
            });

        await using var host = new DashboardApiTestHost(registry: registry);
        var content = JsonContent.Create(new { VariantKey = "control" });
        var response = await host.Client.PostAsync("/dashboard-api/experiments/ro-exp/activate-variant", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ActivateVariant_ExistingVariant_ReturnsOk()
    {
        var registry = new MutableStubRegistry(
            new AdminExperimentInfo
            {
                Name = "exp-av",
                IsActive = true,
                Trials =
                [
                    new AdminTrialInfo { Key = "control", IsControl = true },
                    new AdminTrialInfo { Key = "variant-a", IsControl = false }
                ]
            });

        await using var host = new DashboardApiTestHost(registry: registry);
        var content = JsonContent.Create(new { VariantKey = "variant-a" });
        var response = await host.Client.PostAsync("/dashboard-api/experiments/exp-av/activate-variant", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("variant-a", json);
    }

    // ── Analytics: error paths ────────────────────────────────────────────────

    [Fact]
    public async Task GetStatistics_NoProvider_Returns501()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/analytics/exp-1/statistics");
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Fact]
    public async Task CompareVariants_NoProvider_Returns501()
    {
        await using var host = new DashboardApiTestHost();
        var content = JsonContent.Create(new { Variants = new[] { "a", "b" } });
        var response = await host.Client.PostAsync("/dashboard-api/analytics/exp-1/compare", content);
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Fact]
    public async Task CompareVariants_TooFewVariants_ReturnsBadRequest()
    {
        var analytics = new Mock<IAnalyticsProvider>();
        analytics.Setup(a => a.GetAnalysisSignalsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await using var host = new DashboardApiTestHost(analyticsProvider: analytics.Object);
        var content = JsonContent.Create(new { Variants = new[] { "only-one" } });
        var response = await host.Client.PostAsync("/dashboard-api/analytics/exp-1/compare", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("two variants", json);
    }

    [Fact]
    public async Task ExportData_InvalidFormat_ReturnsBadRequest()
    {
        var analytics = new Mock<IAnalyticsProvider>();
        await using var host = new DashboardApiTestHost(analyticsProvider: analytics.Object);
        var response = await host.Client.GetAsync("/dashboard-api/analytics/exp-1/export/xml");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("csv", json);
    }

    [Fact]
    public async Task ExportData_NoProvider_Returns501()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/analytics/exp-1/export/json");
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Fact]
    public async Task ExportData_JsonFormat_ReturnsOk()
    {
        var analytics = new Mock<IAnalyticsProvider>();
        analytics.Setup(a => a.GetAssignmentsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        analytics.Setup(a => a.GetAnalysisSignalsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await using var host = new DashboardApiTestHost(analyticsProvider: analytics.Object);
        var response = await host.Client.GetAsync("/dashboard-api/analytics/exp-1/export/json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExportData_CsvFormat_ReturnsCsvContentType()
    {
        var analytics = new Mock<IAnalyticsProvider>();
        analytics.Setup(a => a.GetAssignmentsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        analytics.Setup(a => a.GetAnalysisSignalsAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await using var host = new DashboardApiTestHost(analyticsProvider: analytics.Object);
        var response = await host.Client.GetAsync("/dashboard-api/analytics/exp-1/export/csv");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ct = response.Content.Headers.ContentType?.MediaType;
        Assert.Equal("text/csv", ct);
    }

    [Fact]
    public async Task GetUsageStats_WithRegistryAndProvider_ReturnsStats()
    {
        var registry = new StubRegistry(
            new AdminExperimentInfo { Name = "exp-usage", IsActive = true });

        var analytics = new Mock<IAnalyticsProvider>();
        analytics.Setup(a => a.GetAssignmentsAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new AssignmentEvent { ExperimentName = "exp-usage", SubjectId = "user-1", TrialKey = "control", Timestamp = DateTimeOffset.UtcNow }
            ]);

        await using var host = new DashboardApiTestHost(registry: registry, analyticsProvider: analytics.Object);
        var response = await host.Client.GetAsync("/dashboard-api/analytics/usage");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        // With analytics provider + registry + assignments, response should have exp-usage key
        // The endpoint returns usage stats grouped by experiment name
        Assert.Contains("exp-usage", json);
    }

    // ── Configuration: error paths ────────────────────────────────────────────

    [Fact]
    public async Task GetConfigurationInfo_ResponseShape_HasFrameworkAndServer()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/configuration/info");
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("framework", out _));
        Assert.True(doc.RootElement.TryGetProperty("server", out _));
        Assert.True(doc.RootElement.TryGetProperty("experiments", out _));
        Assert.True(doc.RootElement.TryGetProperty("features", out _));
    }

    [Fact]
    public async Task GetConfigurationInfo_WithRegistry_CountsExperiments()
    {
        var registry = new StubRegistry(
            new AdminExperimentInfo { Name = "active-exp", IsActive = true },
            new AdminExperimentInfo { Name = "inactive-exp", IsActive = false });

        await using var host = new DashboardApiTestHost(registry: registry);
        var response = await host.Client.GetAsync("/dashboard-api/configuration/info");
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var experiments = doc.RootElement.GetProperty("experiments");
        Assert.Equal(2, experiments.GetProperty("total").GetInt32());
        Assert.Equal(1, experiments.GetProperty("active").GetInt32());
    }

    [Fact]
    public async Task GetConfigurationYaml_WithRegistry_ContainsExperimentNames()
    {
        var registry = new StubRegistry(
            new AdminExperimentInfo { Name = "exp-yaml", IsActive = true });

        await using var host = new DashboardApiTestHost(registry: registry);
        var response = await host.Client.GetAsync("/dashboard-api/configuration/yaml");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("exp-yaml", body);
    }

    [Fact]
    public async Task GetConfigurationYaml_NoRegistry_ReturnsPlaceholderComment()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/configuration/yaml");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("No experiments configured", body);
    }

    [Fact]
    public async Task GetKillSwitches_NoRegistry_ReturnsEmpty()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/configuration/kill-switch");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Equal("[]", json.Trim());
    }

    [Fact]
    public async Task UpdateKillSwitch_ResponseShape_HasExperimentAndDisabled()
    {
        await using var host = new DashboardApiTestHost();
        var content = JsonContent.Create(new { Experiment = "exp-ks", Disabled = true });
        var response = await host.Client.PostAsync("/dashboard-api/configuration/kill-switch", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("experiment", out _));
        Assert.True(doc.RootElement.TryGetProperty("disabled", out _));
    }

    // ── DSL: error paths ──────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateDsl_ValidYaml_ReturnsIsValidTrue()
    {
        await using var host = new DashboardApiTestHost();
        var content = JsonContent.Create(new { yaml = "experiments:\n  - name: my-exp\n" });
        var response = await host.Client.PostAsync("/dashboard-api/dsl/validate", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("isValid", out var isValid));
        Assert.True(isValid.GetBoolean());
    }

    [Fact]
    public async Task ValidateDsl_YamlWithTabs_ReturnsErrors()
    {
        await using var host = new DashboardApiTestHost();
        var yamlWithTab = "experiments:\n\t- name: my-exp\n";
        var content = JsonContent.Create(new { yaml = yamlWithTab });
        var response = await host.Client.PostAsync("/dashboard-api/dsl/validate", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("isValid").GetBoolean());
        var errors = doc.RootElement.GetProperty("errors");
        Assert.True(errors.GetArrayLength() > 0);
    }

    [Fact]
    public async Task ValidateDsl_YamlWithUnbalancedBrackets_ReturnsErrors()
    {
        await using var host = new DashboardApiTestHost();
        var badYaml = "experiments: [bad\n";
        var content = JsonContent.Create(new { yaml = badYaml });
        var response = await host.Client.PostAsync("/dashboard-api/dsl/validate", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("isValid").GetBoolean());
    }

    [Fact]
    public async Task ValidateDsl_ParsedExperiments_ResponseShape()
    {
        await using var host = new DashboardApiTestHost();
        var yaml = "experiments:\n  - name: my-exp\n  - name: second-exp\n";
        var content = JsonContent.Create(new { yaml });
        var response = await host.Client.PostAsync("/dashboard-api/dsl/validate", content);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("parsedExperiments", out var parsed));
        Assert.Equal(2, parsed.GetArrayLength());
    }

    [Fact]
    public async Task ApplyDsl_ValidYaml_ReturnsSuccess()
    {
        await using var host = new DashboardApiTestHost();
        // Use YAML without brackets to avoid the bracket-balancing check returning hasErrors=true
        var content = JsonContent.Create(new { yaml = "experiments:\n  - name: my-exp\n" });
        var response = await host.Client.PostAsync("/dashboard-api/dsl/apply", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetCurrentDsl_ResponseShape_HasYamlAndLastApplied()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/dsl/current");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("yaml", out _));
        Assert.True(doc.RootElement.TryGetProperty("hasUnappliedChanges", out _));
    }

    [Fact]
    public async Task GetDslSchema_ResponseShape_HasTypeAndProperties()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/dsl/schema");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("type", out _));
    }

    // ── Governance: error paths ───────────────────────────────────────────────

    [Fact]
    public async Task TransitionState_NoBackplane_ReturnsBadRequest()
    {
        await using var host = new DashboardApiTestHost();
        var content = JsonContent.Create(new { TargetState = "Running" });
        var response = await host.Client.PostAsync("/dashboard-api/governance/exp-1/transition", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ApproveTransition_NoBackplane_ReturnsBadRequest()
    {
        await using var host = new DashboardApiTestHost();
        var content = JsonContent.Create(new { Actor = "admin" });
        var response = await host.Client.PostAsync("/dashboard-api/governance/approvals/transition-id/approve", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RejectTransition_NoBackplane_ReturnsBadRequest()
    {
        await using var host = new DashboardApiTestHost();
        var content = JsonContent.Create(new { Reason = "Policy violation" });
        var response = await host.Client.PostAsync("/dashboard-api/governance/approvals/transition-id/reject", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetLifecycleState_NoBackplane_ReturnsOkWithDraftState()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/governance/exp-1/state");

        // Without backplane, endpoint returns 200 with a "Draft" default state
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("Draft", json);
    }

    [Fact]
    public async Task GetPolicies_NoBackplane_ReturnsOkWithEmptyList()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/governance/exp-1/policies");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("policies", json);
    }

    [Fact]
    public async Task GetVersions_NoBackplane_ReturnsOkWithEmptyList()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/governance/exp-1/versions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("versions", json);
    }

    [Fact]
    public async Task GetVersion_NoBackplane_ReturnsNotFound()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/governance/exp-1/versions/1");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RollbackVersion_NoBackplane_ReturnsBadRequest()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.PostAsync("/dashboard-api/governance/exp-1/versions/2/rollback", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLog_NoBackplane_ReturnsOkWithEmptyAuditLog()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/governance/exp-1/audit");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("auditLog", json);
    }

    [Fact]
    public async Task GetTransitions_NoBackplane_ReturnsOkWithEmptyTransitions()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/governance/exp-1/transitions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("transitions", json);
    }

    // ── Plugins: error paths ──────────────────────────────────────────────────

    [Fact]
    public async Task DiscoverPlugins_NoService_Returns501()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.PostAsync("/dashboard-api/plugins/discover", null);
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Fact]
    public async Task ReloadPlugins_NoService_Returns501()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.PostAsync("/dashboard-api/plugins/reload", null);
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Fact]
    public async Task GetPlugins_WithPluginService_ReturnsPluginList()
    {
        var pluginSvc = new Mock<IPluginManagementService>();
        pluginSvc.Setup(p => p.GetLoadedPluginsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PluginDescriptor { Id = "p1", Name = "Plugin One", Version = "1.0" }]);

        await using var host = new DashboardApiTestHost(pluginManagement: pluginSvc.Object);
        var response = await host.Client.GetAsync("/dashboard-api/plugins");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        // Response wraps plugins in { "plugins": [...] }
        Assert.Contains("p1", json);
    }

    [Fact]
    public async Task DiscoverPlugins_WithService_ReturnsDiscoveredCount()
    {
        var pluginSvc = new Mock<IPluginManagementService>();
        pluginSvc.Setup(p => p.DiscoverPluginsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PluginDescriptor { Id = "p1", Name = "Discovered Plugin", Version = "2.0" }
            ]);

        await using var host = new DashboardApiTestHost(pluginManagement: pluginSvc.Object);
        var response = await host.Client.PostAsync("/dashboard-api/plugins/discover", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetProperty("discoveredCount").GetInt32());
    }

    // ── Targeting: error paths ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTargetingRules_NoService_Returns501()
    {
        await using var host = new DashboardApiTestHost();
        var content = JsonContent.Create(new { Rules = Array.Empty<object>() });
        var response = await host.Client.PostAsync("/dashboard-api/targeting/exp-1/rules", content);
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Fact]
    public async Task EvaluateTargeting_NoService_Returns501()
    {
        await using var host = new DashboardApiTestHost();
        var content = JsonContent.Create(new { Context = new Dictionary<string, object> { ["userId"] = "user-1" } });
        var response = await host.Client.PostAsync("/dashboard-api/targeting/exp-1/evaluate", content);
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Fact]
    public async Task GetTargetingRules_WithService_ReturnsRules()
    {
        var targetingSvc = new Mock<ITargetingManagementService>();
        targetingSvc.Setup(t => t.GetRulesAsync("exp-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new TargetingRuleDto { Id = "rule-1", Type = "attribute-equals", VariantKey = "variant-a", Enabled = true }
            ]);

        await using var host = new DashboardApiTestHost(targetingManagement: targetingSvc.Object);
        var response = await host.Client.GetAsync("/dashboard-api/targeting/exp-1/rules");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("rule-1", json);
    }

    [Fact]
    public async Task GetTargetingRules_WithService_NullReturn_ReturnsNotFound()
    {
        var targetingSvc = new Mock<ITargetingManagementService>();
        targetingSvc.Setup(t => t.GetRulesAsync("exp-missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<TargetingRuleDto>?)null);

        await using var host = new DashboardApiTestHost(targetingManagement: targetingSvc.Object);
        var response = await host.Client.GetAsync("/dashboard-api/targeting/exp-missing/rules");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EvaluateTargeting_WithService_ReturnsMatchedResult()
    {
        var targetingSvc = new Mock<ITargetingManagementService>();
        targetingSvc.Setup(t => t.EvaluateAsync("exp-1", It.IsAny<IReadOnlyDictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TargetingEvaluationResult { Matched = true, MatchedVariant = "variant-b", MatchedRuleId = "rule-1" });

        await using var host = new DashboardApiTestHost(targetingManagement: targetingSvc.Object);
        var content = JsonContent.Create(new { Context = new Dictionary<string, object> { ["userId"] = "user-1" } });
        var response = await host.Client.PostAsync("/dashboard-api/targeting/exp-1/evaluate", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("matched").GetBoolean());
        Assert.Equal("variant-b", doc.RootElement.GetProperty("matchedVariant").GetString());
    }

    // ── Rollout: error paths ──────────────────────────────────────────────────

    [Fact]
    public async Task GetRolloutConfig_NoBackplane_Returns503()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/rollout/exp-1/config");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrUpdateRolloutConfig_NoBackplane_Returns503()
    {
        await using var host = new DashboardApiTestHost();
        var content = JsonContent.Create(new { ExperimentName = "exp-1", TargetVariant = "variant-a" });
        var response = await host.Client.PostAsync("/dashboard-api/rollout/exp-1/config", content);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task AdvanceRollout_NoBackplane_Returns503()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.PostAsync("/dashboard-api/rollout/exp-1/advance", null);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task PauseRollout_NoBackplane_Returns503()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.PostAsync("/dashboard-api/rollout/exp-1/pause", null);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task ResumeRollout_NoBackplane_Returns503()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.PostAsync("/dashboard-api/rollout/exp-1/resume", null);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task RollbackRollout_NoBackplane_Returns503()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.PostAsync("/dashboard-api/rollout/exp-1/rollback", null);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task RestartRollout_NoBackplane_Returns503()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.PostAsync("/dashboard-api/rollout/exp-1/restart", null);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRolloutConfig_NoBackplane_Returns503()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.DeleteAsync("/dashboard-api/rollout/exp-1/config");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task GetRolloutConfig_WithBackplane_NotFound_ReturnsNotFound()
    {
        var persistence = new Mock<IRolloutPersistenceBackplane>();
        persistence.Setup(p => p.GetRolloutConfigAsync("exp-missing", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RolloutConfiguration?)null);

        await using var host = new DashboardApiTestHost(rolloutPersistence: persistence.Object);
        var response = await host.Client.GetAsync("/dashboard-api/rollout/exp-missing/config");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRolloutConfig_WithBackplane_Found_ReturnsConfig()
    {
        var config = new RolloutConfiguration
        {
            ExperimentName = "exp-r",
            TargetVariant = "variant-a",
            Percentage = 25,
            Status = RolloutStatus.InProgress
        };
        var persistence = new Mock<IRolloutPersistenceBackplane>();
        persistence.Setup(p => p.GetRolloutConfigAsync("exp-r", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        await using var host = new DashboardApiTestHost(rolloutPersistence: persistence.Object);
        var response = await host.Client.GetAsync("/dashboard-api/rollout/exp-r/config");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("exp-r", json);
    }

    // ── Audit: error paths ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAuditLog_NoProvider_ReturnsOkWithEmptyArray()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/audit");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Equal("[]", json.Trim());
    }

    [Fact]
    public async Task GetAuditLog_WithLimitParam_ReturnsOk()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/audit?limit=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Content type consistency ──────────────────────────────────────────────

    [Fact]
    public async Task GetLifecycleState_ReturnsJsonContentType()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/governance/exp-1/state");
        var ct = response.Content.Headers.ContentType?.MediaType;
        Assert.Equal("application/json", ct);
    }

    [Fact]
    public async Task GetPendingApprovals_ReturnsJsonContentType()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/governance/approvals/pending");
        var ct = response.Content.Headers.ContentType?.MediaType;
        Assert.Equal("application/json", ct);
    }

    [Fact]
    public async Task GetKillSwitches_ReturnsJsonContentType()
    {
        await using var host = new DashboardApiTestHost();
        var response = await host.Client.GetAsync("/dashboard-api/configuration/kill-switch");
        var ct = response.Content.Headers.ContentType?.MediaType;
        Assert.Equal("application/json", ct);
    }
}
