using System.Text.Json;
using Confluent.Kafka;
using ExperimentFramework.DataPlane.Abstractions;
using ExperimentFramework.DataPlane.Abstractions.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExperimentFramework.DataPlane.Kafka;

/// <summary>
/// Kafka-based data backplane for durable, scalable event streaming.
/// </summary>
public sealed class KafkaDataBackplane : IDataBackplane, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaDataBackplaneOptions _options;
    private readonly ILogger<KafkaDataBackplane> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaDataBackplane"/> class.
    /// </summary>
    public KafkaDataBackplane(
        IOptions<KafkaDataBackplaneOptions> options,
        ILogger<KafkaDataBackplane> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (_options.Brokers == null || _options.Brokers.Count == 0)
        {
            throw new ArgumentException("At least one Kafka broker must be configured", nameof(options));
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Build Kafka producer configuration
        var config = new ProducerConfig
        {
            BootstrapServers = string.Join(",", _options.Brokers),
            BatchSize = _options.BatchSize,
            LingerMs = _options.LingerMs,
            EnableIdempotence = _options.EnableIdempotence,
            CompressionType = Enum.Parse<CompressionType>(_options.CompressionType, ignoreCase: true),
            Acks = ParseAcks(_options.Acks),
            RequestTimeoutMs = _options.RequestTimeoutMs,
            MaxInFlight = _options.MaxInFlight
        };

        if (!string.IsNullOrEmpty(_options.ClientId))
        {
            config.ClientId = _options.ClientId;
        }

        // Apply additional configuration
        if (_options.AdditionalConfig != null)
        {
            foreach (var kvp in _options.AdditionalConfig)
            {
                config.Set(kvp.Key, kvp.Value);
            }
        }

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
            {
                _logger.LogError("Kafka producer error: {Reason} (Code: {Code})", error.Reason, error.Code);
            })
            .Build();

        _logger.LogInformation(
            "Kafka data backplane initialized with brokers: {Brokers}",
            string.Join(", ", _options.Brokers));
    }

    /// <inheritdoc />
    public async ValueTask PublishAsync(DataPlaneEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            if (envelope == null)
            {
                _logger.LogWarning("Attempted to publish null envelope");
                return;
            }

            var topic = GetTopicForEvent(envelope.EventType);
            var key = GetPartitionKey(envelope);
            var value = JsonSerializer.Serialize(envelope, _jsonOptions);

            var message = new Message<string, string>
            {
                Key = key,
                Value = value,
                Headers = new Headers
                {
                    { "event-id", System.Text.Encoding.UTF8.GetBytes(envelope.EventId) },
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(envelope.EventType.ToString()) },
                    { "schema-version", System.Text.Encoding.UTF8.GetBytes(envelope.SchemaVersion) }
                }
            };

            if (!string.IsNullOrEmpty(envelope.CorrelationId))
            {
                message.Headers.Add("correlation-id", System.Text.Encoding.UTF8.GetBytes(envelope.CorrelationId));
            }

            var result = await _producer.ProduceAsync(topic, message, cancellationToken);

            _logger.LogDebug(
                "Published event {EventId} to Kafka topic {Topic} (partition: {Partition}, offset: {Offset})",
                envelope.EventId,
                result.Topic,
                result.Partition.Value,
                result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish event {EventId} to Kafka: {Reason}",
                envelope?.EventId,
                ex.Error.Reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error publishing event {EventId} to Kafka",
                envelope?.EventId);
        }
    }

    /// <inheritdoc />
    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _producer.Flush(cancellationToken);
            _logger.LogDebug("Flushed pending Kafka messages");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush Kafka producer");
        }
    }

    /// <inheritdoc />
    public ValueTask<BackplaneHealth> HealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if producer is healthy by checking its name (will throw if disposed)
            var name = _producer.Name;
            
            return ValueTask.FromResult(BackplaneHealth.Healthy(
                $"Kafka backplane is operational (brokers: {string.Join(", ", _options.Brokers)})"));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(BackplaneHealth.Unhealthy(
                $"Kafka backplane is unhealthy: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets the topic name for the given event type.
    /// </summary>
    private string GetTopicForEvent(DataPlaneEventType eventType)
    {
        if (!string.IsNullOrEmpty(_options.Topic))
        {
            return _options.Topic;
        }

        return eventType switch
        {
            DataPlaneEventType.Exposure => "experiment-exposures",
            DataPlaneEventType.Assignment => "experiment-assignments",
            DataPlaneEventType.Outcome => "experiment-outcomes",
            DataPlaneEventType.AnalysisSignal => "experiment-analysis-signals",
            DataPlaneEventType.Error => "experiment-errors",
            _ => "experiment-events"
        };
    }

    /// <summary>
    /// Gets the partition key for the envelope based on the configured strategy.
    /// </summary>
    private string GetPartitionKey(DataPlaneEnvelope envelope)
    {
        return _options.PartitionStrategy switch
        {
            KafkaPartitionStrategy.ByExperimentKey => ExtractExperimentKey(envelope),
            KafkaPartitionStrategy.BySubjectId => ExtractSubjectId(envelope),
            KafkaPartitionStrategy.ByTenantId => ExtractTenantId(envelope),
            KafkaPartitionStrategy.RoundRobin => envelope.EventId, // Kafka will distribute based on hash
            _ => envelope.EventId
        };
    }

    private string ExtractExperimentKey(DataPlaneEnvelope envelope)
    {
        if (envelope.Payload is ExposureEvent exposure)
            return exposure.ExperimentName;
        if (envelope.Payload is AssignmentEvent assignment)
            return assignment.ExperimentName;
        
        // Try to extract from metadata
        if (envelope.Metadata?.TryGetValue("experimentName", out var expName) == true)
            return expName?.ToString() ?? envelope.EventId;

        return envelope.EventId;
    }

    private string ExtractSubjectId(DataPlaneEnvelope envelope)
    {
        if (envelope.Payload is ExposureEvent exposure)
            return exposure.SubjectId;
        if (envelope.Payload is AssignmentEvent assignment)
            return assignment.SubjectId;

        if (envelope.Metadata?.TryGetValue("subjectId", out var subjId) == true)
            return subjId?.ToString() ?? envelope.EventId;

        return envelope.EventId;
    }

    private string ExtractTenantId(DataPlaneEnvelope envelope)
    {
        if (envelope.Payload is ExposureEvent exposure && !string.IsNullOrEmpty(exposure.TenantId))
            return exposure.TenantId;

        if (envelope.Metadata?.TryGetValue("tenantId", out var tenantId) == true && tenantId != null)
            return tenantId.ToString()!;

        return "default";
    }

    private static Acks ParseAcks(string acks)
    {
        return acks.ToLowerInvariant() switch
        {
            "all" or "-1" => Acks.All,
            "1" => Acks.Leader,
            "0" => Acks.None,
            _ => Acks.All
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
            _logger.LogInformation("Kafka data backplane disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Kafka data backplane");
        }
    }
}
