using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Resilience.Configuration;

/// <summary>
/// Configuration handler for the circuit breaker decorator.
/// This handler allows the circuit breaker to be configured via YAML/JSON configuration files.
/// </summary>
public sealed class CircuitBreakerDecoratorHandler : IConfigurationDecoratorHandler
{
    private readonly ILoggerFactory? _loggerFactory;

    /// <summary>
    /// Creates a new circuit breaker decorator handler.
    /// </summary>
    /// <param name="loggerFactory">Optional logger factory for circuit breaker logging.</param>
    public CircuitBreakerDecoratorHandler(ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public string DecoratorType => "circuitBreaker";

    /// <inheritdoc />
    public void Apply(ExperimentFrameworkBuilder builder, DecoratorConfig config, ILogger? logger)
    {
        var options = ParseOptions(config.Options);
        builder.WithCircuitBreaker(options, _loggerFactory);
    }

    /// <inheritdoc />
    public IEnumerable<ConfigurationValidationError> Validate(DecoratorConfig config, string path)
    {
        if (config.Options == null)
        {
            yield return ConfigurationValidationError.Warning(
                $"{path}.options",
                "No circuit breaker options specified, using defaults");
            yield break;
        }

        // Validate failure ratio threshold
        if (TryGetDoubleOption(config.Options, "failureRatioThreshold", out var ratio))
        {
            if (ratio <= 0 || ratio > 1)
            {
                yield return ConfigurationValidationError.Error(
                    $"{path}.options.failureRatioThreshold",
                    "Failure ratio threshold must be between 0 (exclusive) and 1 (inclusive)");
            }
        }

        // Validate minimum throughput
        if (TryGetIntOption(config.Options, "minimumThroughput", out var throughput))
        {
            if (throughput <= 0)
            {
                yield return ConfigurationValidationError.Error(
                    $"{path}.options.minimumThroughput",
                    "Minimum throughput must be positive");
            }
        }

        // Validate durations
        if (TryGetTimeSpanOption(config.Options, "samplingDuration", out var sampling))
        {
            if (sampling <= TimeSpan.Zero)
            {
                yield return ConfigurationValidationError.Error(
                    $"{path}.options.samplingDuration",
                    "Sampling duration must be positive");
            }
        }

        if (TryGetTimeSpanOption(config.Options, "breakDuration", out var breakDur))
        {
            if (breakDur <= TimeSpan.Zero)
            {
                yield return ConfigurationValidationError.Error(
                    $"{path}.options.breakDuration",
                    "Break duration must be positive");
            }
        }
    }

    private static CircuitBreakerOptions ParseOptions(Dictionary<string, object>? options)
    {
        var result = new CircuitBreakerOptions();

        if (options == null)
            return result;

        if (TryGetDoubleOption(options, "failureRatioThreshold", out var ratio))
        {
            result.FailureRatioThreshold = ratio;
        }

        if (TryGetIntOption(options, "minimumThroughput", out var throughput))
        {
            result.MinimumThroughput = throughput;
        }

        if (TryGetTimeSpanOption(options, "samplingDuration", out var sampling))
        {
            result.SamplingDuration = sampling;
        }

        if (TryGetTimeSpanOption(options, "breakDuration", out var breakDur))
        {
            result.BreakDuration = breakDur;
        }

        if (options.TryGetValue("fallbackTrialKey", out var fallback) && fallback is string fallbackStr)
        {
            result.FallbackTrialKey = fallbackStr;
        }

        return result;
    }

    private static bool TryGetDoubleOption(Dictionary<string, object> options, string key, out double result)
    {
        result = 0;
        if (!options.TryGetValue(key, out var value))
            return false;

        return value switch
        {
            double d => (result = d) == d,
            int i => (result = i) == i,
            long l => (result = l) == l,
            string s => double.TryParse(s, out result),
            _ => false
        };
    }

    private static bool TryGetIntOption(Dictionary<string, object> options, string key, out int result)
    {
        result = 0;
        if (!options.TryGetValue(key, out var value))
            return false;

        return value switch
        {
            int i => (result = i) == i,
            long l => (result = (int)l) == (int)l,
            string s => int.TryParse(s, out result),
            _ => false
        };
    }

    private static bool TryGetTimeSpanOption(Dictionary<string, object> options, string key, out TimeSpan result)
    {
        result = default;
        if (!options.TryGetValue(key, out var value))
            return false;

        return value switch
        {
            TimeSpan ts => (result = ts) == ts,
            string s => TimeSpan.TryParse(s, out result),
            _ => false
        };
    }
}
