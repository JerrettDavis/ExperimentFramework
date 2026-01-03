using System.Text.Json;
using Azure.Messaging.ServiceBus;
using ExperimentFramework.DataPlane.Abstractions;
using ExperimentFramework.DataPlane.Abstractions.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExperimentFramework.DataPlane.AzureServiceBus;

/// <summary>
/// Azure Service Bus-based data backplane for durable, cloud-native event messaging.
/// </summary>
public sealed class AzureServiceBusDataBackplane : IDataBackplane, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly Dictionary<string, ServiceBusSender> _senders;
    private readonly AzureServiceBusDataBackplaneOptions _options;
    private readonly ILogger<AzureServiceBusDataBackplane> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _batchLock;
    private readonly List<ServiceBusMessage> _batchBuffer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureServiceBusDataBackplane"/> class.
    /// </summary>
    public AzureServiceBusDataBackplane(
        IOptions<AzureServiceBusDataBackplaneOptions> options,
        ILogger<AzureServiceBusDataBackplane> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new ArgumentException("Connection string must be provided", nameof(options));
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var clientOptions = new ServiceBusClientOptions
        {
            RetryOptions = new ServiceBusRetryOptions
            {
                MaxRetries = _options.MaxRetryAttempts,
                Mode = ServiceBusRetryMode.Exponential
            }
        };

        _client = new ServiceBusClient(_options.ConnectionString, clientOptions);
        _senders = new Dictionary<string, ServiceBusSender>();
        _batchLock = new SemaphoreSlim(1, 1);
        _batchBuffer = new List<ServiceBusMessage>();

        _logger.LogInformation(
            "Azure Service Bus data backplane initialized with {DestinationType}",
            GetDestinationType());
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

            var destination = GetDestinationName(envelope.EventType);
            var message = CreateServiceBusMessage(envelope);

            await _batchLock.WaitAsync(cancellationToken);
            try
            {
                _batchBuffer.Add(message);

                if (_batchBuffer.Count >= _options.BatchSize)
                {
                    await FlushBatchAsync(destination, cancellationToken);
                }
            }
            finally
            {
                _batchLock.Release();
            }

            _logger.LogDebug(
                "Queued event {EventId} for Azure Service Bus destination {Destination}",
                envelope.EventId,
                destination);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish event {EventId} to Azure Service Bus",
                envelope?.EventId);
        }
    }

    /// <inheritdoc />
    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        await _batchLock.WaitAsync(cancellationToken);
        try
        {
            if (_batchBuffer.Count > 0)
            {
                var destination = GetDefaultDestinationName();
                await FlushBatchAsync(destination, cancellationToken);
            }

            _logger.LogDebug("Flushed pending Azure Service Bus messages");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush Azure Service Bus messages");
        }
        finally
        {
            _batchLock.Release();
        }
    }

    /// <inheritdoc />
    public ValueTask<BackplaneHealth> HealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if client is operational
            if (_client.IsClosed)
            {
                return ValueTask.FromResult(BackplaneHealth.Unhealthy("Azure Service Bus client is closed"));
            }

            return ValueTask.FromResult(BackplaneHealth.Healthy(
                $"Azure Service Bus backplane is operational ({GetDestinationType()})"));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult(BackplaneHealth.Unhealthy(
                $"Azure Service Bus backplane is unhealthy: {ex.Message}"));
        }
    }

    private async Task FlushBatchAsync(string destination, CancellationToken cancellationToken)
    {
        if (_batchBuffer.Count == 0)
            return;

        try
        {
            var sender = GetOrCreateSender(destination);
            await sender.SendMessagesAsync(_batchBuffer, cancellationToken);

            _logger.LogInformation(
                "Sent batch of {MessageCount} messages to Azure Service Bus destination {Destination}",
                _batchBuffer.Count,
                destination);

            _batchBuffer.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send batch of {MessageCount} messages to Azure Service Bus",
                _batchBuffer.Count);
            
            // Clear buffer to avoid infinite retry
            _batchBuffer.Clear();
        }
    }

    private ServiceBusSender GetOrCreateSender(string destination)
    {
        if (!_senders.TryGetValue(destination, out var sender))
        {
            sender = _client.CreateSender(destination);
            _senders[destination] = sender;
        }

        return sender;
    }

    private ServiceBusMessage CreateServiceBusMessage(DataPlaneEnvelope envelope)
    {
        var json = JsonSerializer.Serialize(envelope, _jsonOptions);
        var message = new ServiceBusMessage(json)
        {
            MessageId = envelope.EventId,
            ContentType = "application/json"
        };

        // Add application properties for filtering
        message.ApplicationProperties.Add("EventType", envelope.EventType.ToString());
        message.ApplicationProperties.Add("SchemaVersion", envelope.SchemaVersion);
        message.ApplicationProperties.Add("Timestamp", envelope.Timestamp.ToUnixTimeMilliseconds());

        if (!string.IsNullOrEmpty(envelope.CorrelationId))
        {
            message.CorrelationId = envelope.CorrelationId;
        }

        // Set session ID if sessions are enabled
        if (_options.EnableSessions)
        {
            message.SessionId = GetSessionId(envelope);
        }

        // Set time-to-live if configured
        if (_options.MessageTimeToLiveMinutes.HasValue)
        {
            message.TimeToLive = TimeSpan.FromMinutes(_options.MessageTimeToLiveMinutes.Value);
        }

        return message;
    }

    private string GetDestinationName(DataPlaneEventType eventType)
    {
        // If specific queue or topic name is provided, use it
        if (!string.IsNullOrEmpty(_options.QueueName))
        {
            return _options.QueueName;
        }

        if (!string.IsNullOrEmpty(_options.TopicName))
        {
            return _options.TopicName;
        }

        // Use type-specific destinations if enabled
        if (_options.UseTypeSpecificDestinations)
        {
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

        // Default to single queue/topic
        return "experiment-events";
    }

    private string GetDefaultDestinationName()
    {
        return !string.IsNullOrEmpty(_options.QueueName) ? _options.QueueName :
               !string.IsNullOrEmpty(_options.TopicName) ? _options.TopicName :
               "experiment-events";
    }

    private string GetDestinationType()
    {
        if (!string.IsNullOrEmpty(_options.QueueName))
            return $"queue: {_options.QueueName}";
        if (!string.IsNullOrEmpty(_options.TopicName))
            return $"topic: {_options.TopicName}";
        return _options.UseTypeSpecificDestinations ? "type-specific queues" : "queue: experiment-events";
    }

    private string GetSessionId(DataPlaneEnvelope envelope)
    {
        return _options.SessionStrategy switch
        {
            ServiceBusSessionStrategy.ByExperimentKey => ExtractExperimentKey(envelope),
            ServiceBusSessionStrategy.BySubjectId => ExtractSubjectId(envelope),
            ServiceBusSessionStrategy.ByTenantId => ExtractTenantId(envelope),
            _ => envelope.EventId
        };
    }

    private string ExtractExperimentKey(DataPlaneEnvelope envelope)
    {
        if (envelope.Payload is ExposureEvent exposure)
            return exposure.ExperimentName;
        if (envelope.Payload is AssignmentEvent assignment)
            return assignment.ExperimentName;

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

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            // Flush any remaining messages
            await FlushAsync();

            // Dispose all senders
            foreach (var sender in _senders.Values)
            {
                await sender.DisposeAsync();
            }

            _senders.Clear();

            // Dispose client
            await _client.DisposeAsync();

            _batchLock.Dispose();

            _logger.LogInformation("Azure Service Bus data backplane disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Azure Service Bus data backplane");
        }
    }
}
