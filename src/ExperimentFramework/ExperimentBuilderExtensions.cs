using ExperimentFramework.Decorators;
using ExperimentFramework.KillSwitch;
using ExperimentFramework.Metrics;
using ExperimentFramework.Models;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework;

/// <summary>
/// Extension methods for ExperimentFrameworkBuilder to add resilience and monitoring features.
/// </summary>
public static class ExperimentBuilderExtensions
{
    /// <summary>
    /// Adds timeout enforcement to all trials in the experiment.
    /// </summary>
    public static ExperimentFrameworkBuilder WithTimeout(
        this ExperimentFrameworkBuilder builder,
        TimeSpan timeout,
        TimeoutAction onTimeout = TimeoutAction.FallbackToDefault,
        string? fallbackTrialKey = null)
    {
        var policy = new TimeoutPolicy
        {
            Timeout = timeout,
            OnTimeout = onTimeout,
            FallbackTrialKey = fallbackTrialKey
        };

        var factory = new TimeoutDecoratorFactory(policy);
        return builder.AddDecoratorFactory(factory);
    }

    /// <summary>
    /// Adds metrics collection to track experiment performance.
    /// </summary>
    public static ExperimentFrameworkBuilder WithMetrics(
        this ExperimentFrameworkBuilder builder,
        IExperimentMetrics metrics)
    {
        var factory = new MetricsDecoratorFactory(metrics);
        return builder.AddDecoratorFactory(factory);
    }

    /// <summary>
    /// Adds kill switch functionality for emergency experiment disabling.
    /// </summary>
    public static ExperimentFrameworkBuilder WithKillSwitch(
        this ExperimentFrameworkBuilder builder,
        IKillSwitchProvider killSwitch,
        ILoggerFactory? loggerFactory = null)
    {
        var factory = new KillSwitchDecoratorFactory(killSwitch, loggerFactory);
        return builder.AddDecoratorFactory(factory);
    }
}
