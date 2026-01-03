namespace ExperimentFramework.DataPlane.Abstractions;

/// <summary>
/// Envelope containing a versioned experimentation event.
/// </summary>
public sealed class DataPlaneEnvelope
{
    /// <summary>
    /// Gets or sets the unique event identifier.
    /// </summary>
    public required string EventId { get; init; }

    /// <summary>
    /// Gets or sets the event timestamp.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public required DataPlaneEventType EventType { get; init; }

    /// <summary>
    /// Gets or sets the schema version of the event payload.
    /// </summary>
    public required string SchemaVersion { get; init; }

    /// <summary>
    /// Gets or sets the event payload.
    /// </summary>
    public required object Payload { get; init; }

    /// <summary>
    /// Gets or sets optional correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets or sets optional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Types of data plane events.
/// </summary>
public enum DataPlaneEventType
{
    /// <summary>
    /// Exposure event: a subject was exposed to a variant.
    /// </summary>
    Exposure,

    /// <summary>
    /// Assignment event: a subject's assignment changed.
    /// </summary>
    Assignment,

    /// <summary>
    /// Outcome event: an experiment outcome was recorded.
    /// </summary>
    Outcome,

    /// <summary>
    /// Analysis signal event: a statistical or science signal.
    /// </summary>
    AnalysisSignal,

    /// <summary>
    /// Error event: an error occurred during experiment execution.
    /// </summary>
    Error
}
