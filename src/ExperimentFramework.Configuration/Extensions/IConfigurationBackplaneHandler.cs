using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Configuration.Extensions;

/// <summary>
/// Interface for handling data backplane configuration from YAML/JSON.
/// Implement this interface in backplane extension packages to register custom backplanes
/// that can be configured via configuration files.
/// </summary>
/// <example>
/// In ExperimentFramework.DataPlane.Kafka package:
/// <code>
/// public class KafkaBackplaneConfigurationHandler : IConfigurationBackplaneHandler
/// {
///     public string BackplaneType => "kafka";
///
///     public void ConfigureServices(IServiceCollection services, 
///         DataPlaneBackplaneConfig config, ILogger? logger)
///     {
///         var brokers = config.Options?["brokers"] as List&lt;object&gt; 
///             ?? throw new InvalidOperationException("Kafka brokers are required");
///         
///         services.AddKafkaDataBackplane(options =>
///         {
///             options.Brokers = brokers.Select(b => b.ToString()!).ToList();
///             // ... configure other options
///         });
///     }
/// }
/// </code>
/// </example>
public interface IConfigurationBackplaneHandler
{
    /// <summary>
    /// The backplane type identifier used in configuration files.
    /// This is case-insensitive and matched against the backplane's "type" field.
    /// </summary>
    /// <example>"kafka", "azureServiceBus", "sqlServer"</example>
    string BackplaneType { get; }

    /// <summary>
    /// Configures the service collection with the backplane implementation.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="config">The backplane configuration from the configuration file.</param>
    /// <param name="logger">Optional logger for warnings and errors.</param>
    void ConfigureServices(IServiceCollection services, DataPlaneBackplaneConfig config, ILogger? logger);

    /// <summary>
    /// Validates the backplane configuration before applying.
    /// Return validation errors or warnings. Return an empty collection if valid.
    /// </summary>
    /// <param name="config">The backplane configuration to validate.</param>
    /// <param name="path">The configuration path for error reporting.</param>
    /// <returns>Collection of validation errors/warnings, or empty if valid.</returns>
    IEnumerable<ConfigurationValidationError> Validate(DataPlaneBackplaneConfig config, string path);
}
