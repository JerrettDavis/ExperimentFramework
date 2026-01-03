using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Configuration.Extensions.Handlers;

/// <summary>
/// Configuration handler for the OpenTelemetry data backplane.
/// </summary>
public sealed class OpenTelemetryBackplaneConfigurationHandler : IConfigurationBackplaneHandler
{
    /// <inheritdoc />
    public string BackplaneType => "openTelemetry";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, DataPlaneBackplaneConfig config, ILogger? logger)
    {
        logger?.LogInformation("Configuring OpenTelemetry data backplane from configuration");
        
        // Use the extension method from ExperimentFramework.DataPlane
        var addMethod = typeof(ServiceCollectionExtensions)
            .GetMethod("AddOpenTelemetryDataBackplane", new[] { typeof(IServiceCollection) });
        
        if (addMethod != null)
        {
            addMethod.Invoke(null, new object[] { services });
        }
        else
        {
            throw new InvalidOperationException(
                "AddOpenTelemetryDataBackplane method not found. Ensure ExperimentFramework.DataPlane is referenced.");
        }
    }

    /// <inheritdoc />
    public IEnumerable<ConfigurationValidationError> Validate(DataPlaneBackplaneConfig config, string path)
    {
        // OpenTelemetry backplane has no required configuration
        return Enumerable.Empty<ConfigurationValidationError>();
    }
}
