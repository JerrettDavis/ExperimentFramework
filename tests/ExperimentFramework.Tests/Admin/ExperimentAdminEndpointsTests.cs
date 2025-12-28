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

[Feature("ExperimentAdminEndpoints provides HTTP API for experiment administration")]
public sealed class ExperimentAdminEndpointsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("GET /api/experiments returns empty array when no registry")]
    [Fact]
    public async Task Get_experiments_returns_empty_when_no_registry()
    {
        await using var app = CreateApp();
        var client = app.CreateClient();

        var response = await client.GetAsync("/api/experiments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("experiments", content);
    }

    [Scenario("GET /api/experiments returns experiments when registry available")]
    [Fact]
    public async Task Get_experiments_returns_experiments_from_registry()
    {
        var registry = new TestExperimentRegistry([
            new ExperimentInfo { Name = "exp-1", IsActive = true },
            new ExperimentInfo { Name = "exp-2", IsActive = false }
        ]);

        await using var app = CreateApp(registry);
        var client = app.CreateClient();

        var response = await client.GetAsync("/api/experiments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("exp-1", content);
        Assert.Contains("exp-2", content);
    }

    [Scenario("GET /api/experiments/{name} returns 404 when no registry")]
    [Fact]
    public async Task Get_experiment_returns_404_when_no_registry()
    {
        await using var app = CreateApp();
        var client = app.CreateClient();

        var response = await client.GetAsync("/api/experiments/test-exp");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Scenario("GET /api/experiments/{name} returns 404 when experiment not found")]
    [Fact]
    public async Task Get_experiment_returns_404_when_not_found()
    {
        var registry = new TestExperimentRegistry([]);
        await using var app = CreateApp(registry);
        var client = app.CreateClient();

        var response = await client.GetAsync("/api/experiments/missing-exp");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Scenario("GET /api/experiments/{name} returns experiment details")]
    [Fact]
    public async Task Get_experiment_returns_details()
    {
        var registry = new TestExperimentRegistry([
            new ExperimentInfo
            {
                Name = "my-experiment",
                ServiceType = typeof(IFormattable),
                IsActive = true,
                Trials =
                [
                    new TrialInfo { Key = "control", IsControl = true },
                    new TrialInfo { Key = "treatment", IsControl = false }
                ]
            }
        ]);

        await using var app = CreateApp(registry);
        var client = app.CreateClient();

        var response = await client.GetAsync("/api/experiments/my-experiment");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("my-experiment", content);
        Assert.Contains("control", content);
        Assert.Contains("treatment", content);
    }

    [Scenario("GET /api/experiments/{name}/status returns 404 when no registry")]
    [Fact]
    public async Task Get_experiment_status_returns_404_when_no_registry()
    {
        await using var app = CreateApp();
        var client = app.CreateClient();

        var response = await client.GetAsync("/api/experiments/test-exp/status");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Scenario("GET /api/experiments/{name}/status returns 404 when experiment not found")]
    [Fact]
    public async Task Get_experiment_status_returns_404_when_not_found()
    {
        var registry = new TestExperimentRegistry([]);
        await using var app = CreateApp(registry);
        var client = app.CreateClient();

        var response = await client.GetAsync("/api/experiments/missing/status");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Scenario("GET /api/experiments/{name}/status returns experiment status")]
    [Fact]
    public async Task Get_experiment_status_returns_status()
    {
        var registry = new TestExperimentRegistry([
            new ExperimentInfo { Name = "active-exp", IsActive = true }
        ]);

        await using var app = CreateApp(registry);
        var client = app.CreateClient();

        var response = await client.GetAsync("/api/experiments/active-exp/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Active", content);
    }

    [Scenario("POST /api/experiments/{name}/toggle returns 404 when no registry")]
    [Fact]
    public async Task Toggle_experiment_returns_404_when_no_registry()
    {
        await using var app = CreateApp();
        var client = app.CreateClient();

        var response = await client.PostAsync("/api/experiments/test-exp/toggle", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Scenario("POST /api/experiments/{name}/toggle returns 404 when experiment not found")]
    [Fact]
    public async Task Toggle_experiment_returns_404_when_not_found()
    {
        var registry = new TestMutableExperimentRegistry([]);
        await using var app = CreateApp(registry);
        var client = app.CreateClient();

        var response = await client.PostAsync("/api/experiments/missing/toggle", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Scenario("POST /api/experiments/{name}/toggle returns 400 when registry not mutable")]
    [Fact]
    public async Task Toggle_experiment_returns_400_when_not_mutable()
    {
        var registry = new TestExperimentRegistry([
            new ExperimentInfo { Name = "test-exp", IsActive = false }
        ]);

        await using var app = CreateApp(registry);
        var client = app.CreateClient();

        var response = await client.PostAsync("/api/experiments/test-exp/toggle", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Scenario("POST /api/experiments/{name}/toggle toggles experiment state")]
    [Fact]
    public async Task Toggle_experiment_toggles_state()
    {
        var registry = new TestMutableExperimentRegistry([
            new ExperimentInfo { Name = "toggle-exp", IsActive = false }
        ]);

        await using var app = CreateApp(registry);
        var client = app.CreateClient();

        var response = await client.PostAsync("/api/experiments/toggle-exp/toggle", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Active", content);
    }

    [Scenario("API endpoints use custom prefix")]
    [Fact]
    public async Task Api_uses_custom_prefix()
    {
        var registry = new TestExperimentRegistry([
            new ExperimentInfo { Name = "custom-exp", IsActive = true }
        ]);

        await using var app = CreateApp(registry, "/custom/path");
        var client = app.CreateClient();

        var response = await client.GetAsync("/custom/path");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static TestWebApp CreateApp(IExperimentRegistry? registry = null, string prefix = "/api/experiments")
    {
        return new TestWebApp(registry, prefix);
    }

    private sealed class TestWebApp : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly HttpClient _client;

        public TestWebApp(IExperimentRegistry? registry, string prefix)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();

            if (registry != null)
            {
                builder.Services.AddSingleton(registry);
            }

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

    private sealed class TestExperimentRegistry(ExperimentInfo[] experiments) : IExperimentRegistry
    {
        public IEnumerable<ExperimentInfo> GetAllExperiments() => experiments;

        public ExperimentInfo? GetExperiment(string name)
            => experiments.FirstOrDefault(e => e.Name == name);
    }

    private sealed class TestMutableExperimentRegistry(ExperimentInfo[] experiments) : IMutableExperimentRegistry
    {
        public IEnumerable<ExperimentInfo> GetAllExperiments() => experiments;

        public ExperimentInfo? GetExperiment(string name)
            => experiments.FirstOrDefault(e => e.Name == name);

        public void SetExperimentActive(string name, bool isActive)
        {
            var exp = GetExperiment(name);
            if (exp != null) exp.IsActive = isActive;
        }

        public void SetRolloutPercentage(string name, int percentage)
        {
            // No-op for testing
        }
    }
}
