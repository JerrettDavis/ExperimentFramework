using ExperimentFramework;
using ExperimentFramework.SampleConsole;
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
            .Define<IMyDatabase>(c =>
                c.UsingFeatureFlag("UseCloudDb")
                    .AddDefaultTrial<MyDbContext>(key: "false")
                    .AddTrial<MyCloudDbContext>(key: "true")
                    .OnErrorRedirectAndReplayDefault())

            // Example 2: Configuration Value (multi-variant routing)
            .Define<IMyTaxProvider>(c =>
                c.UsingConfigurationKey("Experiments:TaxProvider")
                    .AddDefaultTrial<DefaultTaxProvider>(key: "")
                    .AddTrial<OkTaxProvider>(key: "OK")
                    .AddTrial<TxTaxProvider>(key: "TX")
                    .OnErrorRedirectAndReplayAny());
    }
}
