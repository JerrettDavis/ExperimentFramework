using ExperimentFramework.Admin;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Admin;

[Feature("Admin API RBAC – registry mutation and state transitions")]
public sealed class AdminRbacTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // ────────── helpers ──────────

    private static TestWebApp CreateApp(
        IExperimentRegistry? registry = null,
        string prefix = "/api/experiments")
        => new(registry, prefix);

    private sealed class TestWebApp : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly HttpClient _client;

        public TestWebApp(IExperimentRegistry? registry, string prefix)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            if (registry != null)
                builder.Services.AddSingleton(registry);
            _app = builder.Build();
            _app.MapExperimentAdminApi(prefix);
            _app.Start();
            _client = _app.GetTestClient();
        }

        public HttpClient CreateClient() => _client;

        public async ValueTask DisposeAsync()
        {
            _client.Dispose();
            await _app.DisposeAsync();
        }
    }

    private sealed class SimpleRegistry(ExperimentInfo[] experiments) : IExperimentRegistry
    {
        public IEnumerable<ExperimentInfo> GetAllExperiments() => experiments;
        public ExperimentInfo? GetExperiment(string name) =>
            experiments.FirstOrDefault(e => e.Name == name);
    }

    private sealed class MutableRegistry(ExperimentInfo[] experiments) : IMutableExperimentRegistry
    {
        private readonly List<ExperimentInfo> _items = [.. experiments];

        public IEnumerable<ExperimentInfo> GetAllExperiments() => _items;
        public ExperimentInfo? GetExperiment(string name) =>
            _items.FirstOrDefault(e => e.Name == name);

        public void SetExperimentActive(string name, bool isActive)
        {
            var exp = GetExperiment(name);
            if (exp != null) exp.IsActive = isActive;
        }

        public void SetRolloutPercentage(string name, int percentage) { /* no-op */ }
    }

    // ────────── Admin registry boundary tests ──────────

    [Scenario("Read-only registry rejects toggle with 400")]
    [Fact]
    public async Task ReadOnly_registry_rejects_toggle()
    {
        var registry = new SimpleRegistry([new ExperimentInfo { Name = "exp-a", IsActive = true }]);
        await using var app = CreateApp(registry);
        var client = app.CreateClient();

        var response = await client.PostAsync("/api/experiments/exp-a/toggle", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Scenario("Mutable registry allows activation")]
    [Fact]
    public async Task Mutable_registry_activates_experiment()
    {
        var registry = new MutableRegistry([new ExperimentInfo { Name = "exp-b", IsActive = false }]);
        await using var app = CreateApp(registry);
        var client = app.CreateClient();

        var response = await client.PostAsync("/api/experiments/exp-b/toggle", null);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Active", body);
    }

    [Scenario("Mutable registry deactivates an already-active experiment")]
    [Fact]
    public async Task Mutable_registry_deactivates_active_experiment()
    {
        var registry = new MutableRegistry([new ExperimentInfo { Name = "active-exp", IsActive = true }]);
        await using var app = CreateApp(registry);
        var client = app.CreateClient();

        var response = await client.PostAsync("/api/experiments/active-exp/toggle", null);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Inactive", body);
    }

    [Scenario("Registry returns all experiments including inactive ones")]
    [Fact]
    public async Task Registry_returns_all_experiments()
    {
        var registry = new SimpleRegistry([
            new ExperimentInfo { Name = "active-exp", IsActive = true },
            new ExperimentInfo { Name = "inactive-exp", IsActive = false },
            new ExperimentInfo { Name = "paused-exp", IsActive = false }
        ]);
        await using var app = CreateApp(registry);
        var client = app.CreateClient();

        var response = await client.GetAsync("/api/experiments");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("active-exp", body);
        Assert.Contains("inactive-exp", body);
        Assert.Contains("paused-exp", body);
    }

    [Scenario("Experiment with trials shows control trial")]
    [Fact]
    public async Task Experiment_with_control_trial_shows_control()
    {
        var registry = new SimpleRegistry([
            new ExperimentInfo
            {
                Name = "multi-trial-exp",
                IsActive = true,
                Trials =
                [
                    new TrialInfo { Key = "control", IsControl = true },
                    new TrialInfo { Key = "variant-a", IsControl = false },
                    new TrialInfo { Key = "variant-b", IsControl = false }
                ]
            }
        ]);
        await using var app = CreateApp(registry);
        var client = app.CreateClient();

        var response = await client.GetAsync("/api/experiments/multi-trial-exp");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("control", body);
        Assert.Contains("variant-a", body);
        Assert.Contains("variant-b", body);
    }

    [Scenario("Status endpoint shows Inactive for disabled experiment")]
    [Fact]
    public async Task Status_shows_inactive_for_disabled_experiment()
    {
        var registry = new SimpleRegistry([
            new ExperimentInfo { Name = "off-exp", IsActive = false }
        ]);
        await using var app = CreateApp(registry);
        var client = app.CreateClient();

        var response = await client.GetAsync("/api/experiments/off-exp/status");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Inactive", body);
    }

    [Scenario("Custom prefix routes correctly")]
    [Fact]
    public async Task Custom_prefix_routes_correctly()
    {
        var registry = new SimpleRegistry([
            new ExperimentInfo { Name = "prefix-exp", IsActive = true }
        ]);
        await using var app = CreateApp(registry, "/admin/v1/experiments");
        var client = app.CreateClient();

        var response = await client.GetAsync("/admin/v1/experiments");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("prefix-exp", body);
    }

    [Scenario("Toggle returns 404 for missing experiment with mutable registry")]
    [Fact]
    public async Task Toggle_returns_404_for_missing_experiment()
    {
        var registry = new MutableRegistry([]);
        await using var app = CreateApp(registry);
        var client = app.CreateClient();

        var response = await client.PostAsync("/api/experiments/ghost-exp/toggle", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Scenario("Experiment with metadata roundtrips metadata keys")]
    [Fact]
    public async Task Experiment_with_metadata_returns_details()
    {
        var registry = new SimpleRegistry([
            new ExperimentInfo
            {
                Name = "meta-exp",
                IsActive = true,
                Metadata = new Dictionary<string, object>
                {
                    ["owner"] = "team-a",
                    ["version"] = "2"
                }
            }
        ]);
        await using var app = CreateApp(registry);
        var client = app.CreateClient();

        var response = await client.GetAsync("/api/experiments/meta-exp");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Scenario("Registry with service type includes type name in output")]
    [Fact]
    public async Task Registry_with_service_type_returns_type()
    {
        var registry = new SimpleRegistry([
            new ExperimentInfo
            {
                Name = "typed-exp",
                ServiceType = typeof(IFormattable),
                IsActive = true
            }
        ]);
        await using var app = CreateApp(registry);
        var client = app.CreateClient();

        var response = await client.GetAsync("/api/experiments/typed-exp");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("typed-exp", body);
    }

    [Scenario("After toggling twice experiment returns to original state")]
    [Fact]
    public async Task Double_toggle_restores_state()
    {
        var registry = new MutableRegistry([
            new ExperimentInfo { Name = "toggle-restore-exp", IsActive = true }
        ]);
        await using var app = CreateApp(registry);
        var client = app.CreateClient();

        await client.PostAsync("/api/experiments/toggle-restore-exp/toggle", null);
        var response = await client.PostAsync("/api/experiments/toggle-restore-exp/toggle", null);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Active", body);
    }

    [Scenario("No registry returns empty list not 500")]
    [Fact]
    public async Task No_registry_returns_empty_list()
    {
        await using var app = CreateApp(null);
        var client = app.CreateClient();

        var response = await client.GetAsync("/api/experiments");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("experiments", body);
    }

    [Scenario("Experiment names are case-sensitive in lookup")]
    [Fact]
    public async Task Experiment_names_are_case_sensitive()
    {
        var registry = new SimpleRegistry([
            new ExperimentInfo { Name = "MyExperiment", IsActive = true }
        ]);
        await using var app = CreateApp(registry);
        var client = app.CreateClient();

        var response = await client.GetAsync("/api/experiments/myexperiment");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
