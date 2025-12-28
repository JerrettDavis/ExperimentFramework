using ExperimentFramework.AutoStop.Rules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExperimentFramework.AutoStop;

/// <summary>
/// Extension methods for registering auto-stop services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds auto-stopping support with default rules.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for auto-stop options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddExperimentAutoStop(
        this IServiceCollection services,
        Action<AutoStopOptions>? configure = null)
    {
        var options = new AutoStopOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);

        // Add default rules if none configured
        if (options.Rules.Count == 0)
        {
            options.Rules.Add(new MinimumSampleSizeRule(options.MinimumSampleSize));
            options.Rules.Add(new StatisticalSignificanceRule(options.ConfidenceLevel, options.MinimumSampleSize));
        }

        foreach (var rule in options.Rules)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IStoppingRule), rule));
        }

        return services;
    }

    /// <summary>
    /// Adds a custom stopping rule.
    /// </summary>
    /// <typeparam name="TRule">The stopping rule type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddStoppingRule<TRule>(this IServiceCollection services)
        where TRule : class, IStoppingRule
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStoppingRule, TRule>());
        return services;
    }
}

/// <summary>
/// Configuration options for auto-stop.
/// </summary>
public sealed class AutoStopOptions
{
    /// <summary>
    /// Gets or sets the minimum sample size required per variant.
    /// </summary>
    public long MinimumSampleSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the required confidence level (0-1).
    /// </summary>
    public double ConfidenceLevel { get; set; } = 0.95;

    /// <summary>
    /// Gets or sets how often to check stopping rules.
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets the list of stopping rules to apply.
    /// </summary>
    public List<IStoppingRule> Rules { get; } = [];
}
