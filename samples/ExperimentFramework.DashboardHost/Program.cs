using ExperimentFramework;
using ExperimentFramework.Dashboard;
using ExperimentFramework.Dashboard.Abstractions;
using ExperimentFramework.DashboardHost.Demo;
using ExperimentFramework.DashboardHost.DemoServices;
using ExperimentFramework.Governance;
using ExperimentFramework.Governance.Persistence;
using Microsoft.FeatureManagement;

var cliArgs = DocsCliArgs.Parse(args);
var frozenNow = cliArgs.FreezeDate ?? new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

var builder = WebApplication.CreateBuilder(args);

// Add feature management for feature flags
builder.Services.AddFeatureManagement();

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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Map the experiment dashboard at /dashboard
app.MapExperimentDashboard("/dashboard");

// Add a simple home page that redirects to the dashboard
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
