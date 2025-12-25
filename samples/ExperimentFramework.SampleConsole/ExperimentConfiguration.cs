using ExperimentFramework.SampleConsole.Contexts;
using ExperimentFramework.SampleConsole.Providers;

namespace ExperimentFramework.SampleConsole;

/// <summary>
/// Configures all experiments for the sample application.
/// </summary>
public static class ExperimentConfiguration
{
    [ExperimentCompositionRoot]
    public static ExperimentFrameworkBuilder ConfigureExperiments()
    {
        return ExperimentFrameworkBuilder.Create()
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
