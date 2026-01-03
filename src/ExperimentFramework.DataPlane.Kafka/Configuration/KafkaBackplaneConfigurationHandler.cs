using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.DataPlane.Kafka.Configuration;

/// <summary>
/// Configuration handler for the Kafka data backplane.
/// </summary>
public sealed class KafkaBackplaneConfigurationHandler : IConfigurationBackplaneHandler
{
    /// <inheritdoc />
    public string BackplaneType => "kafka";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services, DataPlaneBackplaneConfig config, ILogger? logger)
    {
        logger?.LogInformation("Configuring Kafka data backplane from configuration");

        var options = new KafkaDataBackplaneOptions
        {
            Brokers = new List<string>()
        };

        // Extract brokers
        if (config.Options?.TryGetValue("brokers", out var brokersObj) == true)
        {
            if (brokersObj is List<object> brokersList)
            {
                options.Brokers = brokersList.Select(b => b?.ToString() ?? "").Where(b => !string.IsNullOrEmpty(b)).ToList();
            }
            else if (brokersObj is string brokerString)
            {
                options.Brokers = brokerString.Split(',').Select(b => b.Trim()).Where(b => !string.IsNullOrEmpty(b)).ToList();
            }
        }

        // Extract optional topic
        if (config.Options?.TryGetValue("topic", out var topicObj) == true && topicObj != null)
        {
            options.Topic = topicObj.ToString();
        }

        // Extract partition strategy
        if (config.Options?.TryGetValue("partitionBy", out var partitionObj) == true && partitionObj != null)
        {
            var partitionStr = partitionObj.ToString();
            options.PartitionStrategy = partitionStr?.ToLowerInvariant() switch
            {
                "experimentkey" => KafkaPartitionStrategy.ByExperimentKey,
                "subjectid" => KafkaPartitionStrategy.BySubjectId,
                "tenantid" => KafkaPartitionStrategy.ByTenantId,
                "roundrobin" => KafkaPartitionStrategy.RoundRobin,
                _ => KafkaPartitionStrategy.ByExperimentKey
            };
        }

        // Extract batch size
        if (config.Options?.TryGetValue("batchSize", out var batchSizeObj) == true && batchSizeObj != null)
        {
            if (int.TryParse(batchSizeObj.ToString(), out var batchSize))
            {
                options.BatchSize = batchSize;
            }
        }

        // Extract linger ms
        if (config.Options?.TryGetValue("lingerMs", out var lingerMsObj) == true && lingerMsObj != null)
        {
            if (int.TryParse(lingerMsObj.ToString(), out var lingerMs))
            {
                options.LingerMs = lingerMs;
            }
        }

        // Extract idempotence setting
        if (config.Options?.TryGetValue("enableIdempotence", out var idempotenceObj) == true && idempotenceObj != null)
        {
            if (bool.TryParse(idempotenceObj.ToString(), out var enableIdempotence))
            {
                options.EnableIdempotence = enableIdempotence;
            }
        }

        // Extract compression type
        if (config.Options?.TryGetValue("compressionType", out var compressionObj) == true && compressionObj != null)
        {
            options.CompressionType = compressionObj.ToString() ?? "snappy";
        }

        // Extract acks
        if (config.Options?.TryGetValue("acks", out var acksObj) == true && acksObj != null)
        {
            options.Acks = acksObj.ToString() ?? "all";
        }

        // Extract client ID
        if (config.Options?.TryGetValue("clientId", out var clientIdObj) == true && clientIdObj != null)
        {
            options.ClientId = clientIdObj.ToString();
        }

        // Add Kafka backplane to services
        services.AddKafkaDataBackplane(options);

        logger?.LogInformation(
            "Kafka data backplane configured with {BrokerCount} broker(s)",
            options.Brokers.Count);
    }

    /// <inheritdoc />
    public IEnumerable<ConfigurationValidationError> Validate(DataPlaneBackplaneConfig config, string path)
    {
        var errors = new List<ConfigurationValidationError>();

        // Validate brokers
        if (config.Options == null || !config.Options.ContainsKey("brokers"))
        {
            errors.Add(new ConfigurationValidationError(
                path,
                "Kafka backplane requires 'brokers' configuration",
                ValidationSeverity.Error));
        }

        return errors;
    }
}
