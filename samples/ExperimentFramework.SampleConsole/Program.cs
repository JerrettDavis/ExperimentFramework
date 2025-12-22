using ExperimentFramework;
using ExperimentFramework.SampleConsole;
using ExperimentFramework.SampleConsole.Contexts;
using ExperimentFramework.SampleConsole.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;

var builder = Host.CreateApplicationBuilder();

// Feature management (IFeatureManager + IFeatureManagerSnapshot).
// Feature flags are read from IConfiguration (appsettings.json here).
builder.Services.AddFeatureManagement();

// ========================================
// 1. Register concrete implementations
// ========================================
// The experiment framework will:
// 1) Remove IMyDatabase / IMyTaxProvider registrations
// 2) Keep the concrete implementations registered by type
// 3) Re-register IMyDatabase / IMyTaxProvider as proxy mediators
builder.Services.AddScoped<MyDbContext>();
builder.Services.AddScoped<MyCloudDbContext>();
builder.Services.AddScoped<DefaultTaxProvider>();
builder.Services.AddScoped<OkTaxProvider>();
builder.Services.AddScoped<TxTaxProvider>();

builder.Services.AddScoped<IMyDatabase, MyDbContext>();
builder.Services.AddScoped<IMyTaxProvider, DefaultTaxProvider>();

// ========================================
// 2. Configure Experiments
// ========================================
var experiments = ExperimentConfiguration.ConfigureExperiments();

// ========================================
// Other available selection modes:
// ========================================

// Example 3: Variant Feature Flag (requires Microsoft.FeatureManagement with variants)
// .Define<IMyService>(c =>
//     c.UsingVariantFeatureFlag("MyVariantFeature")
//         .AddDefaultTrial<ControlImpl>("control")
//         .AddTrial<VariantA>("variant-a")
//         .AddTrial<VariantB>("variant-b"))

// Example 4: Sticky Routing (deterministic A/B testing)
// Requires IExperimentIdentityProvider to be registered
// .Define<IMyService>(c =>
//     c.UsingStickyRouting()
//         .AddDefaultTrial<ControlImpl>("control")
//         .AddTrial<VariantA>("a")
//         .AddTrial<VariantB>("b"))

// ========================================
// 3. Register Framework
// ========================================
builder.Services.AddExperimentFramework(experiments);

// Optional: Enable OpenTelemetry distributed tracing
// builder.Services.AddOpenTelemetryExperimentTracking();
// Then configure OpenTelemetry SDK to export traces

// Optional: Register identity provider for sticky routing
// builder.Services.AddScoped<IExperimentIdentityProvider, MyIdentityProvider>();

builder.Services.AddHostedService<DemoWorker>();

var app = builder.Build();

await app.RunAsync();

// ========================================
// Example Custom Naming Convention
// ========================================
// public class MyCustomNamingConvention : IExperimentNamingConvention
// {
//     public string FeatureFlagNameFor(Type serviceType)
//         => $"Features.{serviceType.Name}";
//
//     public string VariantFlagNameFor(Type serviceType)
//         => $"Variants.{serviceType.Name}";
//
//     public string ConfigurationKeyFor(Type serviceType)
//         => $"Experiments.{serviceType.Name}";
// }

// ========================================
// Example Identity Provider (for sticky routing)
// ========================================
// public class SimpleIdentityProvider : IExperimentIdentityProvider
// {
//     private readonly string _userId;
//
//     public SimpleIdentityProvider()
//     {
//         // In a real app, get this from HttpContext, ClaimsPrincipal, etc.
//         _userId = Environment.MachineName; // Just for demo
//     }
//
//     public bool TryGetIdentity(out string identity)
//     {
//         identity = _userId;
//         return !string.IsNullOrEmpty(identity);
//     }
// }