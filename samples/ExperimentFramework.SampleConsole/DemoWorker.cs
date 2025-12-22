using ExperimentFramework.SampleConsole.Contexts;
using ExperimentFramework.SampleConsole.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.SampleConsole;

public sealed class DemoWorker(
    IServiceProvider root, 
    ILogger<DemoWorker> log) : 
    BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        log.LogInformation("==========================================================");
        log.LogInformation("ExperimentFramework Demo - Runtime Switchable Experiments");
        log.LogInformation("==========================================================");
        log.LogInformation("");
        log.LogInformation("Edit appsettings.json while running to see live switching!");
        log.LogInformation("");
        log.LogInformation("Configuration options:");
        log.LogInformation("  - FeatureManagement:UseCloudDb = true/false (Boolean Feature Flag)");
        log.LogInformation("  - Experiments:TaxProvider = \"\" | \"OK\" | \"TX\" (Configuration Value)");
        log.LogInformation("");
        log.LogInformation("Features enabled in this demo:");
        log.LogInformation("  ✓ Boolean feature flags (IFeatureManagerSnapshot)");
        log.LogInformation("  ✓ Configuration-based selection");
        log.LogInformation("  ✓ Telemetry with benchmarks and error logging");
        log.LogInformation("  ✓ Error policies (redirect and replay)");
        log.LogInformation("  ✓ Request-scoped consistency");
        log.LogInformation("");
        log.LogInformation("Additional capabilities (see README):");
        log.LogInformation("  • OpenTelemetry distributed tracing");
        log.LogInformation("  • Variant feature flags (IVariantFeatureManager)");
        log.LogInformation("  • Sticky routing for A/B testing");
        log.LogInformation("  • Custom naming conventions");
        log.LogInformation("==========================================================");
        log.LogInformation("");

        while (!stoppingToken.IsCancellationRequested)
        {
            // One scope == one request. IFeatureManagerSnapshot is scoped so the
            // evaluated feature state is consistent within this block.
            using var scope = root.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<IMyDatabase>();
            var tax = scope.ServiceProvider.GetRequiredService<IMyTaxProvider>();

            // This call evaluates the feature flag (within snapshot consistency).
            var dbName = await db.GetDatabaseNameAsync(stoppingToken);

            // This call selects based on config.
            var subtotal = 100m;
            var taxAmount = tax.CalculateTax("OK", subtotal);

            // Show which concrete types we ended up using
            // Note: Type names show generated proxy class names (e.g., "MyDatabaseExperimentProxy")
            log.LogInformation("Active DB: {DbName} ({DbType}); Tax: {TaxType} => {Tax:C}",
                dbName, db.GetType().Name, tax.GetType().Name, taxAmount);

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}