using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.DataPlane.AzureServiceBus.Configuration;

/// <summary>
/// Configuration handler for the Azure Service Bus data backplane.
/// </summary>
public sealed class AzureServiceBusBackplaneConfigurationHandler : IConfigurationBackplaneHandler
{
    /// <inheritdoc />
    public string BackplaneType => "azureServiceBus";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, DataPlaneBackplaneConfig config, ILogger? logger)
    {
        logger?.LogInformation("Configuring Azure Service Bus data backplane from configuration");

        var options = new AzureServiceBusDataBackplaneOptions
        {
            ConnectionString = string.Empty
        };

        // Extract connection string
        if (config.Options?.TryGetValue("connectionString", out var connStrObj) == true && connStrObj != null)
        {
            options.ConnectionString = connStrObj.ToString() ?? string.Empty;
        }

        // Extract queue name
        if (config.Options?.TryGetValue("queueName", out var queueObj) == true && queueObj != null)
        {
            options.QueueName = queueObj.ToString();
        }

        // Extract topic name
        if (config.Options?.TryGetValue("topicName", out var topicObj) == true && topicObj != null)
        {
            options.TopicName = topicObj.ToString();
        }

        // Extract batch size
        if (config.Options?.TryGetValue("batchSize", out var batchSizeObj) == true && batchSizeObj != null)
        {
            if (int.TryParse(batchSizeObj.ToString(), out var batchSize))
            {
                options.BatchSize = batchSize;
            }
        }

        // Extract max retry attempts
        if (config.Options?.TryGetValue("maxRetryAttempts", out var retryObj) == true && retryObj != null)
        {
            if (int.TryParse(retryObj.ToString(), out var retries))
            {
                options.MaxRetryAttempts = retries;
            }
        }

        // Extract enable sessions
        if (config.Options?.TryGetValue("enableSessions", out var sessionsObj) == true && sessionsObj != null)
        {
            if (bool.TryParse(sessionsObj.ToString(), out var enableSessions))
            {
                options.EnableSessions = enableSessions;
            }
        }

        // Add Azure Service Bus backplane to services
        services.AddAzureServiceBusDataBackplane(options);

        logger?.LogInformation(
            "Azure Service Bus data backplane configured");
    }

    /// <inheritdoc />
    public IEnumerable<ConfigurationValidationError> Validate(DataPlaneBackplaneConfig config, string path)
    {
        var errors = new List<ConfigurationValidationError>();

        // Validate connection string
        if (config.Options == null || !config.Options.ContainsKey("connectionString"))
        {
            errors.Add(new ConfigurationValidationError(
                path,
                "Azure Service Bus backplane requires 'connectionString' configuration",
                ValidationSeverity.Error));
        }

        return errors;
    }
}
