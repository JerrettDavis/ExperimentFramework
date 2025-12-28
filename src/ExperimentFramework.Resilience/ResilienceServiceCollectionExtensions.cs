using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Resilience.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Resilience;

/// <summary>
/// Extension methods for registering resilience features with the experiment framework configuration system.
/// </summary>
public static class ResilienceServiceCollectionExtensions
{
    /// <summary>
    /// Adds resilience configuration handlers to the experiment framework.
    /// This enables the 'circuitBreaker' decorator type in YAML/JSON configuration files.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddExperimentResilience();
    /// services.AddExperimentFrameworkFromConfiguration(configuration);
    /// </code>
    ///
    /// Configuration file example:
    /// <code>
    /// experimentFramework:
    ///   decorators:
    ///     - type: circuitBreaker
    ///       options:
    ///         failureRatioThreshold: 0.5
    ///         minimumThroughput: 10
    ///         samplingDuration: "00:00:30"
    ///         breakDuration: "00:01:00"
    /// </code>
    /// </example>
    public static IServiceCollection AddExperimentResilience(this IServiceCollection services)
    {
        // Register the circuit breaker handler with the configuration system
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigurationDecoratorHandler>(sp =>
            {
                var loggerFactory = sp.GetService<ILoggerFactory>();
                return new CircuitBreakerDecoratorHandler(loggerFactory);
            }));

        return services;
    }
}
