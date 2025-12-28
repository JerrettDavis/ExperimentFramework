using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Configuration.Extensions;

/// <summary>
/// Interface for handling decorator configuration from YAML/JSON.
/// Implement this interface in extension packages to register custom decorator types
/// that can be configured via configuration files.
/// </summary>
/// <example>
/// In ExperimentFramework.Resilience package:
/// <code>
/// public class CircuitBreakerDecoratorHandler : IConfigurationDecoratorHandler
/// {
///     public string DecoratorType => "circuitBreaker";
///
///     public void Apply(ExperimentFrameworkBuilder builder, DecoratorConfig config, ILogger? logger)
///     {
///         var options = new CircuitBreakerOptions();
///         // Configure from config.Options
///         builder.WithCircuitBreaker(options);
///     }
/// }
/// </code>
/// </example>
public interface IConfigurationDecoratorHandler
{
    /// <summary>
    /// The decorator type identifier used in configuration files.
    /// This is case-insensitive and matched against the decorator's "type" field.
    /// </summary>
    /// <example>"circuitBreaker", "outcomeCollection", "retry"</example>
    string DecoratorType { get; }

    /// <summary>
    /// Applies the decorator configuration to the experiment framework builder.
    /// </summary>
    /// <param name="builder">The experiment framework builder to configure.</param>
    /// <param name="config">The decorator configuration from the configuration file.</param>
    /// <param name="logger">Optional logger for warnings and errors.</param>
    void Apply(ExperimentFrameworkBuilder builder, DecoratorConfig config, ILogger? logger);

    /// <summary>
    /// Validates the decorator configuration before applying.
    /// Return validation errors or warnings. Return an empty collection if valid.
    /// </summary>
    /// <param name="config">The decorator configuration to validate.</param>
    /// <param name="path">The configuration path for error reporting (e.g., "decorators[0]").</param>
    /// <returns>Collection of validation errors/warnings, or empty if valid.</returns>
    IEnumerable<ConfigurationValidationError> Validate(DecoratorConfig config, string path);
}
