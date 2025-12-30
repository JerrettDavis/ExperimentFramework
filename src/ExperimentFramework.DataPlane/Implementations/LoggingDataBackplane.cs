using ExperimentFramework.DataPlane.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ExperimentFramework.DataPlane.Implementations;

/// <summary>
/// Data backplane that logs events using ILogger.
/// </summary>
/// <remarks>
/// This implementation is suitable for integration with existing logging infrastructure.
/// Events are serialized as JSON and logged at the Information level.
/// </remarks>
public sealed class LoggingDataBackplane : IDataBackplane
{
    private readonly ILogger<LoggingDataBackplane> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingDataBackplane"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public LoggingDataBackplane(ILogger<LoggingDataBackplane> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public ValueTask PublishAsync(DataPlaneEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            if (envelope == null)
            {
                _logger.LogWarning("Attempted to publish null envelope");
                return ValueTask.CompletedTask;
            }

            var json = JsonSerializer.Serialize(envelope, _jsonOptions);
            
            _logger.LogInformation(
                "DataPlane Event: {EventType} | {EventId} | {Timestamp} | Payload: {Payload}",
                envelope.EventType,
                envelope.EventId,
                envelope.Timestamp,
                json);

            return ValueTask.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventId}", envelope?.EventId);
            return ValueTask.CompletedTask;
        }
    }

    /// <inheritdoc />
    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        // Logging backplane doesn't buffer
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<BackplaneHealth> HealthAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(BackplaneHealth.Healthy("Logging backplane is operational"));
    }
}
