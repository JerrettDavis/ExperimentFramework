using Bunit;
using ExperimentFramework.Dashboard.UI.Components.Pages;
using ExperimentFramework.Dashboard.UI.Components.Pages.Governance;
using ExperimentFramework.Dashboard.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace ExperimentFramework.Dashboard.UI.Tests.Components;

/// <summary>
/// bUnit tests for Configuration, Rollout, DslEditor, CreateExperiment, and Governance pages.
/// These tests exercise async @code paths and validate that components render without error.
/// </summary>
public sealed class AdditionalPageTests : BunitContext
{
    // =========================================================
    // URL constants (all relative to base http://localhost/)
    // =========================================================
    private const string ExperimentsUrl = "/api/experiments";
    private const string ConfigInfoUrl = "/api/configuration/info";
    private const string ConfigYamlUrl = "/api/configuration/yaml";
    private const string DslCurrentUrl = "/api/dsl/current";

    // =========================================================
    // Shared helpers
    // =========================================================

    private static ExperimentApiClient BuildApiClient(MultiEndpointHandler handler)
        => new ExperimentApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

    private static ExperimentInfo MakeExperiment(string name = "exp-1", string status = "Active")
        => new ExperimentInfo
        {
            Name = name,
            DisplayName = name,
            Description = "Test experiment",
            Status = status,
            Category = "Revenue",
            Variants = new List<VariantInfo>
            {
                new VariantInfo { Name = "control", DisplayName = "Control" },
                new VariantInfo { Name = "variant-a", DisplayName = "Variant A" },
            },
            TargetingRules = new List<TargetingRule>(),
        };

    private static FrameworkInfo MakeFrameworkInfo()
        => new FrameworkInfo
        {
            Framework = new FrameworkDetails { Name = "ExperimentFramework", Version = "1.0.0", Runtime = "net10.0", ProxyType = "DispatchProxy" },
            Server = new ServerDetails { MachineName = "test-machine", ProcessId = 42, UpTime = "1h" },
            Experiments = new ExperimentStats { Total = 3, Active = 2, Categories = new Dictionary<string, int> { ["Revenue"] = 2 } },
            Features = new List<FeatureInfo>
            {
                new FeatureInfo { Name = "FeatureManagement", Enabled = true, Description = "Feature flags", Category = "Core" },
            }
        };

    private static HttpResponseMessage JsonOk<T>(T value)
        => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(value)
        };

    // =========================================================
    // Configuration page tests (no rendermode guard)
    // =========================================================

    [Fact]
    public async Task Configuration_OnInitializedAsync_CallsGetConfigInfo()
    {
        var called = false;
        var handler = new MultiEndpointHandler();
        handler.Add(ConfigInfoUrl, _ =>
        {
            called = true;
            return JsonOk(MakeFrameworkInfo());
        });
        handler.Add(ConfigYamlUrl, _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("experiments: []", System.Text.Encoding.UTF8, "text/plain")
        });

        Services.AddSingleton(BuildApiClient(handler));

        Render<Configuration>();
        await Task.Delay(200);

        Assert.True(called, "GetConfigInfoAsync should be called during OnInitializedAsync");
    }

    [Fact]
    public async Task Configuration_WhenApiReturnsData_RendersPage()
    {
        var handler = new MultiEndpointHandler();
        handler.Add(ConfigInfoUrl, _ => JsonOk(MakeFrameworkInfo()));
        handler.Add(ConfigYamlUrl, _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("experiments: []", System.Text.Encoding.UTF8, "text/plain")
        });

        Services.AddSingleton(BuildApiClient(handler));

        var cut = Render<Configuration>();
        await Task.Delay(200);
        cut.Render();

        Assert.NotNull(cut.Find("main[data-page='configuration']"));
    }

    [Fact]
    public async Task Configuration_WhenApiThrows_HandlesGracefully()
    {
        var handler = new MultiEndpointHandler();
        handler.Add(ConfigInfoUrl, _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        handler.Add(ConfigYamlUrl, _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        Services.AddSingleton(BuildApiClient(handler));

        var cut = Render<Configuration>();
        await Task.Delay(200);

        Assert.NotNull(cut.Find("main[data-page='configuration']"));
    }

    [Fact]
    public async Task Configuration_WhenDataLoaded_ShowsFrameworkName()
    {
        var handler = new MultiEndpointHandler();
        handler.Add(ConfigInfoUrl, _ => JsonOk(MakeFrameworkInfo()));
        handler.Add(ConfigYamlUrl, _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("experiments: []", System.Text.Encoding.UTF8, "text/plain")
        });

        Services.AddSingleton(BuildApiClient(handler));

        var cut = Render<Configuration>();
        await Task.Delay(200);
        cut.Render();

        // Should render the framework info panel
        Assert.Contains("ExperimentFramework", cut.Markup);
    }

    // =========================================================
    // Rollout page tests (@rendermode InteractiveServer)
    // =========================================================

    [Fact]
    public async Task Rollout_WithIsInteractiveTrue_CallsGetExperiments()
    {
        var called = false;
        var handler = new MultiEndpointHandler();
        handler.Add(ExperimentsUrl, _ =>
        {
            called = true;
            return JsonOk(new { experiments = new List<ExperimentInfo>() });
        });

        Services.AddSingleton(BuildApiClient(handler));
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", isInteractive: true));

        Render<Rollout>();
        await Task.Delay(200);

        Assert.True(called, "GetExperimentsAsync should be called during OnInitializedAsync when interactive");
    }

    [Fact]
    public async Task Rollout_WithIsInteractiveTrue_RendersPage()
    {
        var handler = new MultiEndpointHandler();
        handler.Add(ExperimentsUrl, _ =>
            JsonOk(new { experiments = new List<ExperimentInfo> { MakeExperiment() } }));

        Services.AddSingleton(BuildApiClient(handler));
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", isInteractive: true));

        var cut = Render<Rollout>();
        await Task.Delay(200);
        cut.Render();

        Assert.NotNull(cut.Find("main[data-page='rollout']"));
    }

    [Fact]
    public async Task Rollout_WhenApiThrows_HandlesGracefully()
    {
        var handler = new MultiEndpointHandler();
        handler.Add(ExperimentsUrl, _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        Services.AddSingleton(BuildApiClient(handler));
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", isInteractive: true));

        var cut = Render<Rollout>();
        await Task.Delay(200);

        Assert.NotNull(cut.Find("main[data-page='rollout']"));
    }

    [Fact]
    public async Task Rollout_WithExperimentsLoaded_ShowsSelector()
    {
        var handler = new MultiEndpointHandler();
        handler.Add(ExperimentsUrl, _ =>
            JsonOk(new { experiments = new List<ExperimentInfo> { MakeExperiment("pricing", "Active") } }));

        Services.AddSingleton(BuildApiClient(handler));
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", isInteractive: true));

        var cut = Render<Rollout>();
        await Task.Delay(200);
        cut.Render();

        // Experiment selector should be rendered when loading is complete
        Assert.NotNull(cut.Find("select[data-select='experiment']"));
    }

    // =========================================================
    // DslEditor page tests (@rendermode InteractiveServer)
    // =========================================================

    [Fact]
    public async Task DslEditor_WithIsInteractiveTrue_CallsGetCurrentDsl()
    {
        var called = false;
        var handler = new MultiEndpointHandler();
        handler.Add(DslCurrentUrl, _ =>
        {
            called = true;
            return JsonOk(new DslCurrentResponse { Yaml = "experiments: []", LastApplied = DateTime.UtcNow });
        });

        Services.AddSingleton(BuildApiClient(handler));
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", isInteractive: true));

        Render<DslEditor>();
        await Task.Delay(200);

        Assert.True(called, "GetCurrentDslAsync should be called during OnInitializedAsync when interactive");
    }

    [Fact]
    public async Task DslEditor_WithIsInteractiveTrue_RendersPage()
    {
        var handler = new MultiEndpointHandler();
        handler.Add(DslCurrentUrl, _ =>
            JsonOk(new DslCurrentResponse { Yaml = "experiments: []" }));

        Services.AddSingleton(BuildApiClient(handler));
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", isInteractive: true));

        var cut = Render<DslEditor>();
        await Task.Delay(200);
        cut.Render();

        Assert.NotNull(cut.Find("main[data-page='dsl']"));
    }

    [Fact]
    public async Task DslEditor_WhenApiThrows_FallsBackToDefault()
    {
        var handler = new MultiEndpointHandler();
        handler.Add(DslCurrentUrl, _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        Services.AddSingleton(BuildApiClient(handler));
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", isInteractive: true));

        var cut = Render<DslEditor>();
        await Task.Delay(200);
        cut.Render();

        // Should still render even when API fails (falls back to default YAML)
        Assert.NotNull(cut.Find("main[data-page='dsl']"));
    }

    // =========================================================
    // CreateExperiment page tests (no async init, but has sync methods)
    // =========================================================

    [Fact]
    public void CreateExperiment_RendersPage()
    {
        var handler = new MultiEndpointHandler();
        Services.AddSingleton(BuildApiClient(handler));

        var cut = Render<CreateExperiment>();

        Assert.NotNull(cut.Find("main[data-page='create']"));
    }

    [Fact]
    public void CreateExperiment_StartsOnStep1()
    {
        var handler = new MultiEndpointHandler();
        Services.AddSingleton(BuildApiClient(handler));

        var cut = Render<CreateExperiment>();

        // Step 1 should be active
        Assert.NotNull(cut.Find(".step-indicator"));
    }

    // =========================================================
    // Governance/Audit page tests (no rendermode guard)
    // =========================================================

    [Fact]
    public async Task GovernanceAudit_OnInitializedAsync_CallsGetExperiments()
    {
        var called = false;
        var handler = new MultiEndpointHandler();
        handler.Add(ExperimentsUrl, _ =>
        {
            called = true;
            return JsonOk(new { experiments = new List<ExperimentInfo>() });
        });

        Services.AddSingleton(BuildApiClient(handler));

        Render<Audit>();
        await Task.Delay(200);

        Assert.True(called, "GetExperimentsAsync should be called during GovernanceAudit OnInitializedAsync");
    }

    [Fact]
    public async Task GovernanceAudit_WhenLoaded_RendersPage()
    {
        var handler = new MultiEndpointHandler();
        handler.Add(ExperimentsUrl, _ =>
            JsonOk(new { experiments = new List<ExperimentInfo> { MakeExperiment() } }));

        Services.AddSingleton(BuildApiClient(handler));

        var cut = Render<Audit>();
        await Task.Delay(200);
        cut.Render();

        Assert.NotNull(cut.Find("main[data-page='audit']"));
    }

    [Fact]
    public async Task GovernanceAudit_WithEmptyExperimentList_RendersPage()
    {
        var handler = new MultiEndpointHandler();
        // Return empty experiments list — component uses try/finally so empty response renders fine
        handler.Add(ExperimentsUrl, _ => JsonOk(new { experiments = new List<ExperimentInfo>() }));

        Services.AddSingleton(BuildApiClient(handler));

        var cut = Render<Audit>();
        await Task.Delay(200);
        cut.Render();

        Assert.NotNull(cut.Find("main[data-page='audit']"));
    }

    // =========================================================
    // Governance/Lifecycle page tests (@rendermode InteractiveServer)
    // =========================================================

    [Fact]
    public async Task GovernanceLifecycle_WithIsInteractiveTrue_CallsGetExperiments()
    {
        var called = false;
        var handler = new MultiEndpointHandler();
        handler.Add(ExperimentsUrl, _ =>
        {
            called = true;
            return JsonOk(new { experiments = new List<ExperimentInfo>() });
        });

        Services.AddSingleton(BuildApiClient(handler));
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", isInteractive: true));

        Render<Lifecycle>();
        await Task.Delay(200);

        Assert.True(called, "GetExperimentsAsync should be called during Lifecycle OnInitializedAsync");
    }

    [Fact]
    public async Task GovernanceLifecycle_WithIsInteractiveTrue_RendersPage()
    {
        var handler = new MultiEndpointHandler();
        handler.Add(ExperimentsUrl, _ =>
            JsonOk(new { experiments = new List<ExperimentInfo> { MakeExperiment() } }));

        Services.AddSingleton(BuildApiClient(handler));
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", isInteractive: true));

        var cut = Render<Lifecycle>();
        await Task.Delay(200);
        cut.Render();

        Assert.NotNull(cut.Find("main[data-page='lifecycle']"));
    }

    [Fact]
    public async Task GovernanceLifecycle_WithEmptyExperimentList_RendersPage()
    {
        var handler = new MultiEndpointHandler();
        // Return empty experiments list — component uses try/finally so empty response renders fine
        handler.Add(ExperimentsUrl, _ => JsonOk(new { experiments = new List<ExperimentInfo>() }));

        Services.AddSingleton(BuildApiClient(handler));
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", isInteractive: true));

        var cut = Render<Lifecycle>();
        await Task.Delay(200);
        cut.Render();

        Assert.NotNull(cut.Find("main[data-page='lifecycle']"));
    }

    // =========================================================
    // Governance/Policies page tests (@rendermode InteractiveServer)
    // =========================================================

    [Fact]
    public async Task GovernancePolicies_WithIsInteractiveTrue_CallsGetExperiments()
    {
        var called = false;
        var handler = new MultiEndpointHandler();
        handler.Add(ExperimentsUrl, _ =>
        {
            called = true;
            return JsonOk(new { experiments = new List<ExperimentInfo>() });
        });

        Services.AddSingleton(BuildApiClient(handler));
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", isInteractive: true));

        Render<Policies>();
        await Task.Delay(200);

        Assert.True(called, "GetExperimentsAsync should be called during Policies OnInitializedAsync");
    }

    [Fact]
    public async Task GovernancePolicies_WithIsInteractiveTrue_RendersPage()
    {
        var handler = new MultiEndpointHandler();
        handler.Add(ExperimentsUrl, _ =>
            JsonOk(new { experiments = new List<ExperimentInfo> { MakeExperiment() } }));

        Services.AddSingleton(BuildApiClient(handler));
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", isInteractive: true));

        var cut = Render<Policies>();
        await Task.Delay(200);
        cut.Render();

        Assert.NotNull(cut.Find("main[data-page='policies']"));
    }

    [Fact]
    public async Task GovernancePolicies_WithEmptyExperimentList_RendersPage()
    {
        var handler = new MultiEndpointHandler();
        // Return empty experiments list — component uses try/finally so empty response renders fine
        handler.Add(ExperimentsUrl, _ => JsonOk(new { experiments = new List<ExperimentInfo>() }));

        Services.AddSingleton(BuildApiClient(handler));
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", isInteractive: true));

        var cut = Render<Policies>();
        await Task.Delay(200);
        cut.Render();

        Assert.NotNull(cut.Find("main[data-page='policies']"));
    }

    // =========================================================
    // Governance/Versions page tests (@rendermode InteractiveServer)
    // =========================================================

    [Fact]
    public async Task GovernanceVersions_WithIsInteractiveTrue_CallsGetExperiments()
    {
        var called = false;
        var handler = new MultiEndpointHandler();
        handler.Add(ExperimentsUrl, _ =>
        {
            called = true;
            return JsonOk(new { experiments = new List<ExperimentInfo>() });
        });

        Services.AddSingleton(BuildApiClient(handler));
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", isInteractive: true));

        Render<Versions>();
        await Task.Delay(200);

        Assert.True(called, "GetExperimentsAsync should be called during Versions OnInitializedAsync");
    }

    [Fact]
    public async Task GovernanceVersions_WithIsInteractiveTrue_RendersPage()
    {
        var handler = new MultiEndpointHandler();
        handler.Add(ExperimentsUrl, _ =>
            JsonOk(new { experiments = new List<ExperimentInfo> { MakeExperiment() } }));

        Services.AddSingleton(BuildApiClient(handler));
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", isInteractive: true));

        var cut = Render<Versions>();
        await Task.Delay(200);
        cut.Render();

        Assert.NotNull(cut.Find("main[data-page='versions']"));
    }

    [Fact]
    public async Task GovernanceVersions_WithEmptyExperimentList_RendersPage()
    {
        var handler = new MultiEndpointHandler();
        // Return empty experiments list — component uses try/finally so empty response renders fine
        handler.Add(ExperimentsUrl, _ => JsonOk(new { experiments = new List<ExperimentInfo>() }));

        Services.AddSingleton(BuildApiClient(handler));
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", isInteractive: true));

        var cut = Render<Versions>();
        await Task.Delay(200);
        cut.Render();

        Assert.NotNull(cut.Find("main[data-page='versions']"));
    }

    // =========================================================
    // Governance/Approvals page tests (informational only — no async)
    // =========================================================

    [Fact]
    public void GovernanceApprovals_RendersPage()
    {
        var handler = new MultiEndpointHandler();
        Services.AddSingleton(BuildApiClient(handler));

        var cut = Render<Approvals>();

        Assert.NotNull(cut.Find("main[data-page='approvals']"));
    }

    // =========================================================
    // Plugins page tests (@rendermode not set, but has async init)
    // =========================================================

    [Fact]
    public async Task Plugins_OnInitializedAsync_RendersPage()
    {
        var handler = new MultiEndpointHandler();
        handler.Add("/api/plugins", _ => JsonOk(new List<PluginInfo>()));
        handler.Add("/api/plugins/active", _ => JsonOk(new Dictionary<string, object>()));

        Services.AddSingleton(BuildApiClient(handler));
        Services.AddSingleton<DashboardStateService>();
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", isInteractive: true));

        var cut = Render<Plugins>();
        await Task.Delay(200);
        cut.Render();

        Assert.NotNull(cut.Find("main[data-page='plugins']"));
    }

    [Fact]
    public async Task Plugins_WithEmptyPluginList_RendersPage()
    {
        var handler = new MultiEndpointHandler();
        handler.Add("/api/plugins", _ => JsonOk(new List<PluginInfo>()));
        handler.Add("/api/plugins/active", _ => JsonOk(new Dictionary<string, object>()));

        Services.AddSingleton(BuildApiClient(handler));
        Services.AddSingleton<DashboardStateService>();
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Server", isInteractive: true));

        var cut = Render<Plugins>();
        await Task.Delay(200);
        cut.Render();

        Assert.NotNull(cut.Find("main[data-page='plugins']"));
    }

    // =========================================================
    // Utility types
    // =========================================================

    private sealed class MultiEndpointHandler : HttpMessageHandler
    {
        private readonly List<(string Path, Func<HttpRequestMessage, HttpResponseMessage> Handler)> _routes = new();

        public void Add(string pathPrefix, Func<HttpRequestMessage, HttpResponseMessage> handler)
            => _routes.Add((pathPrefix, handler));

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.PathAndQuery ?? "";
            foreach (var (prefix, handler) in _routes)
            {
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(handler(request));
            }
            // Default: empty 200 OK
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
