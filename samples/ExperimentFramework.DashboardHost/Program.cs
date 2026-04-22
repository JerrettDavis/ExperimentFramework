using ExperimentFramework;
using ExperimentFramework.Dashboard;
using ExperimentFramework.Dashboard.Abstractions;
using ExperimentFramework.Dashboard.UI.Components;
using ExperimentFramework.Dashboard.UI.Services;
using ExperimentFramework.DashboardHost.Demo;
using ExperimentFramework.DashboardHost.DemoServices;
using ExperimentFramework.Governance;
using ExperimentFramework.Governance.Persistence;
using Microsoft.Extensions.FileProviders;
using Microsoft.FeatureManagement;

var cliArgs = DocsCliArgs.Parse(args);
var frozenNow = cliArgs.FreezeDate ?? new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

var builder = WebApplication.CreateBuilder(args);

// Enable static web assets from Razor Class Libraries in all environments
// (by default they are only enabled in Development)
builder.WebHost.UseStaticWebAssets();

// Add feature management for feature flags
builder.Services.AddFeatureManagement();

// Add authorization with an open-access policy so the Dashboard.UI Blazor components
// (which require the "CanAccessExperiments" policy) work without login in docs-demo mode.
builder.Services.AddAuthentication();
builder.Services.AddAuthorization(options =>
{
    // Open policy — allows all requests (docs demo runs without auth)
    options.AddPolicy("CanAccessExperiments", policy => policy.RequireAssertion(_ => true));
    options.AddPolicy("CanModifyExperiments", policy => policy.RequireAssertion(_ => true));
    options.AddPolicy("CanManageRollouts",    policy => policy.RequireAssertion(_ => true));
    options.AddPolicy("AdminOnly",            policy => policy.RequireAssertion(_ => true));
});

// Add Razor Pages for the stub login page
builder.Services.AddRazorPages();

// Add Blazor server-side rendering support for Dashboard UI
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register Dashboard UI services
builder.Services.AddScoped<DashboardStateService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<ExperimentCodeGenerator>();

// Register API client — the Dashboard.UI ExperimentApiClient calls paths like /api/experiments.
// The REST API is mounted at /dashboard/api/... so set base address to /dashboard/.
builder.Services.AddHttpClient<ExperimentApiClient>(client =>
{
    // Client calls relative paths like "api/experiments"; prepend "/dashboard/" to route correctly
    client.BaseAddress = new Uri("http://localhost:5195/dashboard/");
});

if (cliArgs.SeedDocs)
{
    // ---- T5: Docs Demo mode ------------------------------------------------
    // Register demo service concrete types under their interfaces so the
    // registration safety plan builder can locate each service type.
    builder.Services.AddScoped<ICheckoutButtonService, CheckoutButtonControl>();
    builder.Services.AddScoped<ISearchRankerService, SearchBaseline>();
    builder.Services.AddScoped<IHomepageLayoutService, HomepageLayoutControl>();
    builder.Services.AddScoped<IPricingCopyService, PricingCopyOriginal>();
    builder.Services.AddScoped<ILegacyApiService, LegacyApiV1>();

    // Five demo experiments via builder-object pattern
    var demoConfig = ExperimentFrameworkBuilder.Create()
        .Define<ICheckoutButtonService>(e => e
            .UsingFeatureFlag("checkout-button-v2")
            .AddDefaultTrial<CheckoutButtonControl>("control")
            .AddTrial<CheckoutButtonVariantA>("variant-a"))
        .Define<ISearchRankerService>(e => e
            .UsingFeatureFlag("search-ranker-ml")
            .AddDefaultTrial<SearchBaseline>("baseline")
            .AddTrial<SearchMlV1>("ml-v1")
            .AddTrial<SearchMlV2>("ml-v2"))
        .Define<IHomepageLayoutService>(e => e
            .UsingFeatureFlag("homepage-layout-fall2026")
            .AddDefaultTrial<HomepageLayoutControl>("control")
            .AddTrial<HomepageLayoutFallHero>("fall-hero"))
        .Define<IPricingCopyService>(e => e
            .UsingFeatureFlag("pricing-page-copy")
            .AddDefaultTrial<PricingCopyOriginal>("control")
            .AddTrial<PricingCopyBenefitsLed>("benefits"))
        .Define<ILegacyApiService>(e => e
            .UsingFeatureFlag("legacy-api-cutover")
            .AddDefaultTrial<LegacyApiV1>("v1-api")
            .AddTrial<LegacyApiV2>("v2-api"))
        .UseDispatchProxy();

    builder.Services.AddExperimentFramework(demoConfig);

    // Bridge: expose the five demo experiments via IExperimentRegistry so that the
    // Dashboard API (ExperimentEndpoints / DefaultDashboardDataProvider) can list them.
    // The core ExperimentFramework registry is internal, so we supply this thin adapter.
    builder.Services.AddSingleton<ExperimentFramework.Admin.IExperimentRegistry, DemoExperimentRegistry>();

    // Three demo policies via AddExperimentGovernance builder
    builder.Services.AddExperimentGovernance(governance =>
    {
        governance
            .WithPolicy(DemoPolicyDefinitions.RequireTwoApprovers)
            .WithPolicy(DemoPolicyDefinitions.NoFridayDeploys)
            .WithPolicy(DemoPolicyDefinitions.MinSampleSize1000);
    });

    // In-memory governance persistence (resolved after app.Build() for seeding)
    builder.Services.AddInMemoryGovernancePersistence();

    // Demo analytics provider with frozen clock
    var demoAnalytics = new DemoAnalyticsProvider(frozenNow);

    // Add dashboard services with demo analytics wired through DashboardOptions
    builder.Services.AddExperimentDashboard(options =>
    {
        options.PathBase = "/dashboard";
        options.Title = "ExperimentFramework Dashboard — Docs Demo";
        options.EnableAnalytics = true;
        options.EnableGovernanceUI = true;
        options.ItemsPerPage = 25;
        options.RequireAuthorization = false;
        options.AnalyticsProvider = demoAnalytics;
    });
}
else
{
    // ---- Normal mode: existing two-experiment registration -----------------
    // Register concrete types under their interface so the registration safety
    // plan builder can find the IXxx service type in the snapshot.
    builder.Services.AddScoped<IGreetingService, ControlGreetingService>();
    builder.Services.AddScoped<ICalculatorService, ControlCalculatorService>();

    var config = ExperimentFrameworkBuilder.Create()
        .Define<IGreetingService>(experiment => experiment
            .UsingFeatureFlag("UseVariantGreeting")
            .AddDefaultTrial<ControlGreetingService>("false")
            .AddTrial<VariantGreetingService>("true"))
        .Define<ICalculatorService>(experiment => experiment
            .UsingFeatureFlag("UseVariantCalculator")
            .AddDefaultTrial<ControlCalculatorService>("false")
            .AddTrial<VariantCalculatorService>("true"))
        .UseDispatchProxy();

    builder.Services.AddExperimentFramework(config);

    builder.Services.AddExperimentDashboard(options =>
    {
        options.PathBase = "/dashboard";
        options.Title = "ExperimentFramework Dashboard - Sample Host";
        options.EnableAnalytics = true;
        options.EnableGovernanceUI = true;
        options.ItemsPerPage = 25;
        options.RequireAuthorization = false;
    });
}

var app = builder.Build();

// ---- Post-build governance seeding (docs demo only) -----------------------
if (cliArgs.SeedDocs)
{
    app.Logger.LogInformation("[DocsDemo] Seeding governance demo data (frozenNow={FrozenNow})…", frozenNow);
    using var scope = app.Services.CreateScope();
    var backplane = scope.ServiceProvider.GetRequiredService<IGovernancePersistenceBackplane>();
    await GovernanceDemoSeeder.SeedAsync(backplane, frozenNow);
    app.Logger.LogInformation("[DocsDemo] Governance seeding complete.");
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Do NOT use HTTPS redirect — we run HTTP only in docs-demo mode
// app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

// Serve static assets from the Dashboard.UI RCL's wwwroot under /_content/ExperimentFramework.Dashboard.UI/
// AppContext.BaseDirectory = bin/Debug/net10.0 → go 5 levels up to reach repo root
// bin/Debug/net10.0 → bin/Debug → bin → DashboardHost → samples → repo-root
var repoRoot = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var dashboardUiWwwroot = Path.Combine(repoRoot, "src", "ExperimentFramework.Dashboard.UI", "wwwroot");

app.Logger.LogInformation("RCL wwwroot path: {Path}, exists: {Exists}", dashboardUiWwwroot, Directory.Exists(dashboardUiWwwroot));

if (Directory.Exists(dashboardUiWwwroot))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(dashboardUiWwwroot),
        RequestPath  = "/_content/ExperimentFramework.Dashboard.UI"
    });
}
else
{
    app.UseStaticFiles();  // serves wwwroot if present
}

// MapStaticAssets handles /_content/... RCL static assets (CSS, JS, images)
app.MapStaticAssets();

// Map dashboard REST API at /dashboard/api
app.MapExperimentDashboard("/dashboard");

// Map Razor Pages (login stub)
app.MapRazorPages();

// Map Blazor SSR components (serves /dashboard, /dashboard/experiments, etc.)
// AddInteractiveServerRenderMode() registers the SignalR hub (/_blazor) automatically.
// MapBlazorHub() is legacy Blazor Server and must NOT be called here.
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Home redirect
app.MapGet("/", () => Results.Redirect("/dashboard"));

app.Run();

// ============================================
// Sample Service Interfaces and Implementations
// ============================================

/// <summary>
/// Sample greeting service interface for A/B testing different greeting styles.
/// </summary>
public interface IGreetingService
{
    string GetGreeting(string name);
}

/// <summary>
/// Control implementation - traditional greeting.
/// </summary>
public class ControlGreetingService : IGreetingService
{
    public string GetGreeting(string name) => $"Hello, {name}!";
}

/// <summary>
/// Variant implementation - casual greeting with emoji.
/// </summary>
public class VariantGreetingService : IGreetingService
{
    public string GetGreeting(string name) => $"Hey there, {name}! 👋";
}

/// <summary>
/// Sample calculator service interface for testing different calculation strategies.
/// </summary>
public interface ICalculatorService
{
    int Add(int a, int b);
}

/// <summary>
/// Control implementation - basic addition.
/// </summary>
public class ControlCalculatorService : ICalculatorService
{
    public int Add(int a, int b) => a + b;
}

/// <summary>
/// Variant implementation - addition with telemetry logging.
/// </summary>
public class VariantCalculatorService : ICalculatorService
{
    private readonly ILogger<VariantCalculatorService> _logger;

    public VariantCalculatorService(ILogger<VariantCalculatorService> logger)
    {
        _logger = logger;
    }

    public int Add(int a, int b)
    {
        var result = a + b;
        _logger.LogInformation("Calculated {A} + {B} = {Result}", a, b, result);
        return result;
    }
}

// Make Program class accessible for integration tests (if needed)
public partial class Program { }

// ============================================
// CLI arg parsing (file-scoped helper — C# 11)
// Supports:
//   --seed=docs          (or env EXPERIMENT_DEMO_SEED=docs)
//   --freeze-date <ISO>  (e.g. 2026-04-01T12:00:00+00:00)
// ============================================

file sealed record DocsCliArgs(bool SeedDocs, DateTimeOffset? FreezeDate)
{
    public static DocsCliArgs Parse(string[] args)
    {
        var seedDocs = false;
        DateTimeOffset? freezeDate = null;

        // Check environment variable first
        var envSeed = Environment.GetEnvironmentVariable("EXPERIMENT_DEMO_SEED");
        if (string.Equals(envSeed, "docs", StringComparison.OrdinalIgnoreCase))
            seedDocs = true;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            // --seed=docs  or  --seed docs
            if (arg.Equals("--seed=docs", StringComparison.OrdinalIgnoreCase))
            {
                seedDocs = true;
            }
            else if (arg.Equals("--seed", StringComparison.OrdinalIgnoreCase) &&
                     i + 1 < args.Length &&
                     string.Equals(args[i + 1], "docs", StringComparison.OrdinalIgnoreCase))
            {
                seedDocs = true;
                i++;
            }
            // --freeze-date <ISO-8601>
            else if (arg.Equals("--freeze-date", StringComparison.OrdinalIgnoreCase) &&
                     i + 1 < args.Length)
            {
                if (DateTimeOffset.TryParse(args[i + 1],
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.RoundtripKind,
                        out var parsed))
                {
                    freezeDate = parsed;
                }
                i++;
            }
        }

        return new DocsCliArgs(seedDocs, freezeDate);
    }
}
