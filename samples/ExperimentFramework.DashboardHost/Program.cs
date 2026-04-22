using ExperimentFramework;
using ExperimentFramework.Dashboard;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

// Add feature management for feature flags
builder.Services.AddFeatureManagement();

// Register service implementations first
builder.Services.AddScoped<ControlGreetingService>();
builder.Services.AddScoped<VariantGreetingService>();
builder.Services.AddScoped<ControlCalculatorService>();
builder.Services.AddScoped<VariantCalculatorService>();

// Configure experiment framework with sample experiments
var config = ExperimentFrameworkBuilder.Create()
    .Define<IGreetingService>(experiment => experiment
        .UsingFeatureFlag("UseVariantGreeting")
        .AddDefaultTrial<ControlGreetingService>("false")
        .AddTrial<VariantGreetingService>("true"))
    .Define<ICalculatorService>(experiment => experiment
        .UsingFeatureFlag("UseVariantCalculator")
        .AddDefaultTrial<ControlCalculatorService>("false")
        .AddTrial<VariantCalculatorService>("true"))
    .UseDispatchProxy(); // Use runtime proxies for simplicity

builder.Services.AddExperimentFramework(config);

// Add dashboard services
builder.Services.AddExperimentDashboard(options =>
{
    options.PathBase = "/dashboard";
    options.Title = "ExperimentFramework Dashboard - Sample Host";
    options.EnableAnalytics = true;
    options.EnableGovernanceUI = true;
    options.ItemsPerPage = 25;
    options.RequireAuthorization = false;

    // Configure multi-tenancy (optional)
    // Uncomment one of the following to enable tenant resolution:

    // Option 1: Resolve tenant from HTTP header
    // options.TenantResolver = new HttpHeaderTenantResolver("X-Tenant-Id");

    // Option 2: Resolve tenant from subdomain
    // options.TenantResolver = new SubdomainTenantResolver();

    // Option 3: Resolve tenant from claims
    // options.TenantResolver = new ClaimTenantResolver("tenant_id");

    // Option 4: Composite resolver (try multiple strategies)
    // options.TenantResolver = new CompositeTenantResolver(
    //     new ClaimTenantResolver("tenant_id"),
    //     new HttpHeaderTenantResolver("X-Tenant-Id")
    // );
});

// Add authorization (if needed)
// builder.Services.AddAuthorization(options =>
// {
//     options.AddPolicy("DashboardAccess", policy =>
//     {
//         policy.RequireAuthenticatedUser();
//         policy.RequireRole("Admin", "Experimenter");
//     });
// });

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Add authentication/authorization if configured
// app.UseAuthentication();
// app.UseAuthorization();

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
// TODO(T5): gate on --seed=docs
// When T5 lands, wrap the existing experiment registration in an if/else on cliArgs.SeedDocs.
// The demo registration block below (currently commented out) should replace the existing
// two-experiment block when --seed=docs is passed.
//
// using ExperimentFramework.DashboardHost.DemoServices;
//
// if (cliArgs.SeedDocs)
// {
//     #region Docs Demo — five experiments for screenshot capture
//     builder.Services.AddScoped<CheckoutButtonControl>();
//     builder.Services.AddScoped<CheckoutButtonVariantA>();
//     builder.Services.AddScoped<SearchBaseline>();
//     builder.Services.AddScoped<SearchMlV1>();
//     builder.Services.AddScoped<SearchMlV2>();
//     builder.Services.AddScoped<HomepageLayoutControl>();
//     builder.Services.AddScoped<HomepageLayoutFallHero>();
//     builder.Services.AddScoped<PricingCopyOriginal>();
//     builder.Services.AddScoped<PricingCopyBenefitsLed>();
//     builder.Services.AddScoped<LegacyApiV1>();
//     builder.Services.AddScoped<LegacyApiV2>();
//
//     var demoConfig = ExperimentFrameworkBuilder.Create()
//         .Define<ICheckoutButtonService>(e => e
//             .UsingFeatureFlag("checkout-button-v2")
//             .AddDefaultTrial<CheckoutButtonControl>("control")
//             .AddTrial<CheckoutButtonVariantA>("variant-a"))
//         .Define<ISearchRankerService>(e => e
//             .UsingFeatureFlag("search-ranker-ml")
//             .AddDefaultTrial<SearchBaseline>("baseline")
//             .AddTrial<SearchMlV1>("ml-v1")
//             .AddTrial<SearchMlV2>("ml-v2"))
//         .Define<IHomepageLayoutService>(e => e
//             .UsingFeatureFlag("homepage-layout-fall2026")
//             .AddDefaultTrial<HomepageLayoutControl>("control")
//             .AddTrial<HomepageLayoutFallHero>("fall-hero"))
//         .Define<IPricingCopyService>(e => e
//             .UsingFeatureFlag("pricing-page-copy")
//             .AddDefaultTrial<PricingCopyOriginal>("control")
//             .AddTrial<PricingCopyBenefitsLed>("benefits"))
//         .Define<ILegacyApiService>(e => e
//             .UsingFeatureFlag("legacy-api-cutover")
//             .AddDefaultTrial<LegacyApiV1>("v1-api")
//             .AddTrial<LegacyApiV2>("v2-api"))
//         .UseDispatchProxy();
//
//     builder.Services.AddExperimentFramework(demoConfig);
//     #endregion
// }
// else
// {
//     // existing non-demo experiment registration (see top of file)
// }
// ============================================
