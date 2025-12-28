using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Configuration.Extensions;

/// <summary>
/// Interface for handling selection mode configuration from YAML/JSON.
/// Implement this interface in extension packages to register custom selection modes
/// that can be configured via configuration files.
/// </summary>
/// <example>
/// In ExperimentFramework.Rollout package:
/// <code>
/// public class PercentageRolloutSelectionModeHandler : IConfigurationSelectionModeHandler
/// {
///     public string ModeType => "percentageRollout";
///
///     public void Apply&lt;TService&gt;(ServiceExperimentBuilder&lt;TService&gt; builder,
///         SelectionModeConfig config, ILogger? logger) where TService : class
///     {
///         var percentage = config.Options?["percentage"] as double? ?? 50.0;
///         builder.UsingCustomMode("PercentageRollout", percentage.ToString());
///     }
/// }
/// </code>
/// </example>
public interface IConfigurationSelectionModeHandler
{
    /// <summary>
    /// The selection mode type identifier used in configuration files.
    /// This is case-insensitive and matched against the selectionMode's "type" field.
    /// </summary>
    /// <example>"percentageRollout", "segment", "weightedRandom"</example>
    string ModeType { get; }

    /// <summary>
    /// Applies the selection mode configuration to the service experiment builder.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <param name="builder">The service experiment builder to configure.</param>
    /// <param name="config">The selection mode configuration from the configuration file.</param>
    /// <param name="logger">Optional logger for warnings and errors.</param>
    void Apply<TService>(ServiceExperimentBuilder<TService> builder, SelectionModeConfig config, ILogger? logger)
        where TService : class;

    /// <summary>
    /// Validates the selection mode configuration before applying.
    /// Return validation errors or warnings. Return an empty collection if valid.
    /// </summary>
    /// <param name="config">The selection mode configuration to validate.</param>
    /// <param name="path">The configuration path for error reporting.</param>
    /// <returns>Collection of validation errors/warnings, or empty if valid.</returns>
    IEnumerable<ConfigurationValidationError> Validate(SelectionModeConfig config, string path);
}
