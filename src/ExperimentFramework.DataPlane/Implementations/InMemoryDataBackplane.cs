using ExperimentFramework.DataPlane.Abstractions;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.DataPlane.Implementations;

/// <summary>
/// In-memory data backplane that stores events in memory.
/// </summary>
/// <remarks>
/// This implementation is suitable for testing and development.
/// Events are stored in a concurrent collection and can be retrieved for inspection.
/// </remarks>
public sealed class InMemoryDataBackplane : IDataBackplane
{
    private readonly ILogger<InMemoryDataBackplane> _logger;
    private readonly System.Collections.Concurrent.ConcurrentBag<DataPlaneEnvelope> _events = new();
    private volatile bool _isHealthy = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryDataBackplane"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public InMemoryDataBackplane(ILogger<InMemoryDataBackplane> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all events that have been published.
    /// </summary>
    public IReadOnlyCollection<DataPlaneEnvelope> Events => _events.ToArray();

    /// <summary>
    /// Clears all stored events.
    /// </summary>
    public void Clear() => _events.Clear();

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

            _events.Add(envelope);
            _logger.LogDebug("Published event {EventId} of type {EventType}", 
                envelope.EventId, envelope.EventType);

            return ValueTask.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event");
            _isHealthy = false;
            return ValueTask.CompletedTask;
        }
    }

    /// <inheritdoc />
    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        // In-memory implementation doesn't need flushing
        _logger.LogDebug("Flush called on in-memory backplane (no-op)");
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<BackplaneHealth> HealthAsync(CancellationToken cancellationToken = default)
    {
        var health = _isHealthy
            ? BackplaneHealth.Healthy($"In-memory backplane: {_events.Count} events stored")
            : BackplaneHealth.Unhealthy("In-memory backplane encountered errors");

        return ValueTask.FromResult(health);
    }
}
