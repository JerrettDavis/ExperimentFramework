namespace ExperimentFramework.Audit;

/// <summary>
/// Represents an audit event for experiment operations.
/// </summary>
public sealed class AuditEvent
{
    /// <summary>
    /// Gets or sets the unique event ID.
    /// </summary>
    public required string EventId { get; init; }

    /// <summary>
    /// Gets or sets the timestamp of the event.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets or sets the type of audit event.
    /// </summary>
    public required AuditEventType EventType { get; init; }

    /// <summary>
    /// Gets or sets the experiment name.
    /// </summary>
    public string? ExperimentName { get; init; }

    /// <summary>
    /// Gets or sets the service type being experimented on.
    /// </summary>
    public string? ServiceType { get; init; }

    /// <summary>
    /// Gets or sets the user or system that triggered the event.
    /// </summary>
    public string? Actor { get; init; }

    /// <summary>
    /// Gets or sets the selected trial key (for selection events).
    /// </summary>
    public string? SelectedTrialKey { get; init; }

    /// <summary>
    /// Gets or sets additional details about the event.
    /// </summary>
    public Dictionary<string, object>? Details { get; init; }

    /// <summary>
    /// Gets or sets the correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Types of audit events.
/// </summary>
public enum AuditEventType
{
    /// <summary>
    /// A variant was selected for a request.
    /// </summary>
    VariantSelected,

    /// <summary>
    /// An experiment was created.
    /// </summary>
    ExperimentCreated,

    /// <summary>
    /// An experiment was started (activated).
    /// </summary>
    ExperimentStarted,

    /// <summary>
    /// An experiment was stopped (deactivated).
    /// </summary>
    ExperimentStopped,

    /// <summary>
    /// An experiment configuration was modified.
    /// </summary>
    ExperimentModified,

    /// <summary>
    /// An experiment was deleted.
    /// </summary>
    ExperimentDeleted,

    /// <summary>
    /// A rollout percentage was changed.
    /// </summary>
    RolloutChanged,

    /// <summary>
    /// An error occurred during experiment execution.
    /// </summary>
    Error,

    /// <summary>
    /// Fallback was triggered due to an error.
    /// </summary>
    FallbackTriggered
}
