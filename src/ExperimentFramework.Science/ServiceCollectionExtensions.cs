using ExperimentFramework.Science.Analysis;
using ExperimentFramework.Science.Corrections;
using ExperimentFramework.Science.EffectSize;
using ExperimentFramework.Science.Power;
using ExperimentFramework.Science.Reporting;
using ExperimentFramework.Science.Snapshots;
using ExperimentFramework.Science.Statistics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExperimentFramework.Science;

/// <summary>
/// Extension methods for registering experiment science services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds experiment science services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This registers:
    /// <list type="bullet">
    /// <item><description>Statistical tests (t-test, chi-square, Mann-Whitney U, ANOVA)</description></item>
    /// <item><description>Effect size calculators (Cohen's d, odds ratio, relative risk)</description></item>
    /// <item><description>Power analyzer</description></item>
    /// <item><description>Multiple comparison corrections</description></item>
    /// <item><description>Experiment analyzer</description></item>
    /// <item><description>Reporters (Markdown, JSON)</description></item>
    /// <item><description>Snapshot store (in-memory)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Requires <c>AddExperimentDataCollection()</c> to be called first for the outcome store.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddExperimentScience(this IServiceCollection services)
    {
        // Statistical tests
        services.TryAddSingleton<IStatisticalTest>(TwoSampleTTest.Instance);
        services.TryAddSingleton<IPairedStatisticalTest>(PairedTTest.Instance);
        services.TryAddSingleton<IMultiGroupStatisticalTest>(OneWayAnova.Instance);

        // Effect size calculators
        services.TryAddSingleton<IEffectSizeCalculator>(CohensD.Instance);
        services.TryAddSingleton<IBinaryEffectSizeCalculator>(OddsRatio.Instance);

        // Power analyzer
        services.TryAddSingleton<IPowerAnalyzer>(PowerAnalyzer.Instance);

        // Corrections
        services.TryAddSingleton<IMultipleComparisonCorrection>(BenjaminiHochbergCorrection.Instance);

        // Snapshot store
        services.TryAddSingleton<ISnapshotStore, InMemorySnapshotStore>();

        // Reporters
        services.TryAddSingleton<ReporterOptions>();
        services.TryAddSingleton<MarkdownReporter>();
        services.TryAddSingleton<JsonReporter>();
        services.TryAddSingleton<IExperimentReporter, MarkdownReporter>();

        // Experiment analyzer
        services.TryAddSingleton<IExperimentAnalyzer, ExperimentAnalyzer>();

        return services;
    }

    /// <summary>
    /// Adds experiment science services with custom options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExperimentScience(
        this IServiceCollection services,
        Action<ScienceOptions> configure)
    {
        var options = new ScienceOptions();
        configure(options);

        services.AddExperimentScience();

        if (options.ReporterOptions != null)
        {
            services.AddSingleton(options.ReporterOptions);
        }

        return services;
    }
}

/// <summary>
/// Options for configuring experiment science services.
/// </summary>
public sealed class ScienceOptions
{
    /// <summary>
    /// Gets or sets the reporter options.
    /// </summary>
    public ReporterOptions? ReporterOptions { get; set; }
}
