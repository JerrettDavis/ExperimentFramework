using ExperimentFramework;
using ExperimentFramework.ComprehensiveSample;
using ExperimentFramework.ComprehensiveSample.Demos;
using ExperimentFramework.ComprehensiveSample.Services.Decorator;
using ExperimentFramework.ComprehensiveSample.Services.ErrorPolicy;
using ExperimentFramework.ComprehensiveSample.Services.ReturnTypes;
using ExperimentFramework.ComprehensiveSample.Services.Telemetry;
using ExperimentFramework.ComprehensiveSample.Services.Variant;
using ExperimentFramework.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

Console.WriteLine("""
                  ╔══════════════════════════════════════════════════════════════════════════════╗
                  ║                                                                              ║
                  ║                  ExperimentFramework - Comprehensive Sample                  ║
                  ║                                                                              ║
                  ║  Demonstrates all features of the ExperimentFramework library:               ║
                  ║    • All 5 error policies (Throw, RedirectDefault, RedirectAny,              ║
                  ║                             RedirectSpecific, RedirectOrdered)               ║
                  ║    • All 4 selection modes (FeatureFlag, Config, Variant, StickyRouting)     ║
                  ║    • All 5 return types (void, Task, Task<T>, ValueTask, ValueTask<T>)       ║
                  ║    • Custom decorators (timing, caching, logging)                            ║
                  ║    • OpenTelemetry distributed tracing integration                           ║
                  ║    • Variant feature flags (multi-variant A/B/C testing)                     ║
                  ║                                                                              ║
                  ╚══════════════════════════════════════════════════════════════════════════════╝
                  """);

var builder = Host.CreateApplicationBuilder(args);

// ========================================
// 1. Feature Management
// ========================================
builder.Services.AddFeatureManagement();

// ========================================
// 2. Register Service Implementations
// ========================================

// Error Policy services
builder.Services.AddScoped<StableImplementation>();
builder.Services.AddScoped<UnstableImplementation>();
builder.Services.AddScoped<DefaultImplementation>();
builder.Services.AddScoped<ExperimentalImplementation>();
builder.Services.AddScoped<PrimaryProvider>();
builder.Services.AddScoped<SecondaryProvider>();
builder.Services.AddScoped<TertiaryProvider>();
builder.Services.AddScoped<PrimaryImplementation>();
builder.Services.AddScoped<SecondaryImplementation>();
builder.Services.AddScoped<NoopDiagnosticsHandler>();
builder.Services.AddScoped<CloudDatabaseImplementation>();
builder.Services.AddScoped<LocalCacheImplementation>();
builder.Services.AddScoped<InMemoryCacheImplementation>();
builder.Services.AddScoped<StaticDataImplementation>();

// Decorator services
builder.Services.AddScoped<DatabaseDataService>();
builder.Services.AddScoped<CacheDataService>();

// Telemetry services
builder.Services.AddScoped<EmailNotificationService>();
builder.Services.AddScoped<SmsNotificationService>();

// Variant services
builder.Services.AddScoped<StripePaymentProcessor>();
builder.Services.AddScoped<PayPalPaymentProcessor>();
builder.Services.AddScoped<SquarePaymentProcessor>();

// Return type services
builder.Services.AddScoped<VoidImplementationA>();
builder.Services.AddScoped<VoidImplementationB>();
builder.Services.AddScoped<TaskImplementationA>();
builder.Services.AddScoped<TaskImplementationB>();
builder.Services.AddScoped<TaskTImplementationA>();
builder.Services.AddScoped<TaskTImplementationB>();
builder.Services.AddScoped<ValueTaskImplementationA>();
builder.Services.AddScoped<ValueTaskImplementationB>();
builder.Services.AddScoped<ValueTaskTImplementationA>();
builder.Services.AddScoped<ValueTaskTImplementationB>();

// Register default interface implementations
builder.Services.AddScoped<IThrowPolicyService, StableImplementation>();
builder.Services.AddScoped<IRedirectDefaultService, DefaultImplementation>();
builder.Services.AddScoped<IRedirectAnyService, TertiaryProvider>();
builder.Services.AddScoped<IRedirectSpecificService, PrimaryImplementation>();
builder.Services.AddScoped<IRedirectOrderedService, CloudDatabaseImplementation>();
builder.Services.AddScoped<IDataService, DatabaseDataService>();
builder.Services.AddScoped<INotificationService, EmailNotificationService>();
builder.Services.AddScoped<IPaymentProcessor, StripePaymentProcessor>();
builder.Services.AddScoped<IVoidService, VoidImplementationA>();
builder.Services.AddScoped<ITaskService, TaskImplementationA>();
builder.Services.AddScoped<ITaskTService, TaskTImplementationA>();
builder.Services.AddScoped<IValueTaskService, ValueTaskImplementationA>();
builder.Services.AddScoped<IValueTaskTService, ValueTaskTImplementationA>();

// ========================================
// 3. OpenTelemetry Configuration
// ========================================
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("ExperimentFramework.ComprehensiveSample"))
    .WithTracing(tracing => tracing
        .AddSource("ExperimentFramework") // Capture experiment telemetry
        .AddConsoleExporter());

// Register OpenTelemetry experiment telemetry
builder.Services.AddSingleton<IExperimentTelemetry, OpenTelemetryExperimentTelemetry>();

// ========================================
// 4. Configure Experiment Framework
// ========================================
var experiments = ExperimentConfiguration.ConfigureAllExperiments();
builder.Services.AddExperimentFramework(experiments);

// ========================================
// 5. Register Demo Runners
// ========================================
builder.Services.AddScoped<ErrorPolicyDemo>();
builder.Services.AddScoped<CustomDecoratorDemo>();
builder.Services.AddScoped<OpenTelemetryDemo>();
builder.Services.AddScoped<VariantFeatureDemo>();
builder.Services.AddScoped<ReturnTypesDemo>();

var app = builder.Build();

// ========================================
// 6. Run All Demos
// ========================================
using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

try
{
    // Run each demo in sequence
    await services.GetRequiredService<ErrorPolicyDemo>().RunAsync();
    await services.GetRequiredService<CustomDecoratorDemo>().RunAsync();
    await services.GetRequiredService<OpenTelemetryDemo>().RunAsync();
    await services.GetRequiredService<VariantFeatureDemo>().RunAsync();
    await services.GetRequiredService<ReturnTypesDemo>().RunAsync();

    Console.WriteLine("\n" + new string('=', 80));
    Console.WriteLine("ALL DEMOS COMPLETED SUCCESSFULLY!");
    Console.WriteLine(new string('=', 80));
    Console.WriteLine("\nKey Takeaways:");
    Console.WriteLine("  ✅ All 3 error policies demonstrated");
    Console.WriteLine("  ✅ All 4 selection modes available (boolean flag, config, variant, sticky routing)");
    Console.WriteLine("  ✅ All 5 return types supported");
    Console.WriteLine("  ✅ Custom decorators for cross-cutting concerns");
    Console.WriteLine("  ✅ OpenTelemetry integration for distributed tracing");
    Console.WriteLine("  ✅ Source generators create zero-overhead proxies at compile-time");
    Console.WriteLine("\nFor more information, see:");
    Console.WriteLine("  - README.md in the samples directory");
    Console.WriteLine("  - Project documentation");
    Console.WriteLine("  - https://github.com/yourorg/ExperimentFramework");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ ERROR: {ex.GetType().Name}: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    return 1;
}

return 0;
