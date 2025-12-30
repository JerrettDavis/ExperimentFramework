using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Configuration.Extensions.Handlers;

/// <summary>
/// Configuration handler for the in-memory data backplane.
/// </summary>
public sealed class InMemoryBackplaneConfigurationHandler : IConfigurationBackplaneHandler
{
    /// <inheritdoc />
    public string BackplaneType => "inMemory";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, DataPlaneBackplaneConfig config, ILogger? logger)
    {
        logger?.LogInformation("Configuring in-memory data backplane from configuration");
        
        // Use the extension method from ExperimentFramework.DataPlane
        var addMethod = typeof(ServiceCollectionExtensions)
            .GetMethod("AddInMemoryDataBackplane", new[] { typeof(IServiceCollection) });
        
        if (addMethod != null)
        {
            addMethod.Invoke(null, new object[] { services });
        }
        else
        {
            throw new InvalidOperationException(
                "AddInMemoryDataBackplane method not found. Ensure ExperimentFramework.DataPlane is referenced.");
        }
    }

    /// <inheritdoc />
    public IEnumerable<ConfigurationValidationError> Validate(DataPlaneBackplaneConfig config, string path)
    {
        // In-memory backplane has no required configuration
        return Enumerable.Empty<ConfigurationValidationError>();
    }
}
