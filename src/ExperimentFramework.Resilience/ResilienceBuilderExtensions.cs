using ExperimentFramework;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Resilience;

/// <summary>
/// Extension methods for ExperimentFrameworkBuilder to add Polly-based resilience features.
/// </summary>
public static class ResilienceBuilderExtensions
{
    /// <summary>
    /// Adds a circuit breaker to prevent cascading failures.
    /// </summary>
    public static ExperimentFrameworkBuilder WithCircuitBreaker(
        this ExperimentFrameworkBuilder builder,
        Action<CircuitBreakerOptions>? configure = null,
        ILoggerFactory? loggerFactory = null)
    {
        var options = new CircuitBreakerOptions();
        configure?.Invoke(options);

        var factory = new CircuitBreakerDecoratorFactory(options, loggerFactory);
        return builder.AddDecoratorFactory(factory);
    }

    /// <summary>
    /// Adds a circuit breaker with specific options.
    /// </summary>
    public static ExperimentFrameworkBuilder WithCircuitBreaker(
        this ExperimentFrameworkBuilder builder,
        CircuitBreakerOptions options,
        ILoggerFactory? loggerFactory = null)
    {
        var factory = new CircuitBreakerDecoratorFactory(options, loggerFactory);
        return builder.AddDecoratorFactory(factory);
    }
}
