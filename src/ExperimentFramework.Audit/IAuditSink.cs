namespace ExperimentFramework.Audit;

/// <summary>
/// Receives and persists audit events.
/// </summary>
public interface IAuditSink
{
    /// <summary>
    /// Records an audit event.
    /// </summary>
    /// <param name="auditEvent">The event to record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Aggregates multiple audit sinks.
/// </summary>
public sealed class CompositeAuditSink : IAuditSink
{
    private readonly IAuditSink[] _sinks;

    /// <summary>
    /// Creates a composite sink from multiple sinks.
    /// </summary>
    /// <param name="sinks">The sinks to aggregate.</param>
    public CompositeAuditSink(IEnumerable<IAuditSink> sinks)
    {
        _sinks = sinks.ToArray();
    }

    /// <inheritdoc />
    public async ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        foreach (var sink in _sinks)
        {
            await sink.RecordAsync(auditEvent, cancellationToken);
        }
    }
}
