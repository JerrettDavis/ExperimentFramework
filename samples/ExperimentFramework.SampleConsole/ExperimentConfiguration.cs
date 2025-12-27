using ExperimentFramework.SampleConsole.Contexts;
using ExperimentFramework.SampleConsole.Providers;

namespace ExperimentFramework.SampleConsole;

/// <summary>
/// Configures all experiments for the sample application.
///
/// This sample demonstrates using DispatchProxy for runtime proxy generation.
/// DispatchProxy is useful when:
///   - You want simpler deployment (no source generators)
///   - You're adding experiments to legacy code
///   - You need maximum flexibility at runtime
///
/// For compile-time source generators (better performance), see:
///   - SampleWebApp: Uses .UseSourceGenerators() fluent API
///   - ComprehensiveSample: Uses [ExperimentCompositionRoot] attribute
/// </summary>
public static class ExperimentConfiguration
{
    public static ExperimentFrameworkBuilder ConfigureExperiments()
    {
        return ExperimentFrameworkBuilder.Create()
            // Use DispatchProxy for runtime proxy generation
            // Alternative: .UseSourceGenerators() for compile-time generation
            .UseDispatchProxy()

            // Add built-in decorators for logging
            .AddLogger(l => l.AddBenchmarks().AddErrorLogging())

            // Example 1: Boolean Feature Flag (true/false routing)
            .Trial<IMyDatabase>(t =>
                t.UsingFeatureFlag("UseCloudDb")
                    .AddControl<MyDbContext>()
                    .AddCondition<MyCloudDbContext>("true")
                    .OnErrorFallbackToControl())

            // Example 2: Configuration Value (multi-variant routing)
            .Trial<IMyTaxProvider>(t =>
                t.UsingConfigurationKey("Experiments:TaxProvider")
                    .AddControl<DefaultTaxProvider>()
                    .AddVariant<OkTaxProvider>("OK")
                    .AddVariant<TxTaxProvider>("TX")
                    .OnErrorTryAny());
    }
}
