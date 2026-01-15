namespace ExperimentFramework.Testing;

/// <summary>
/// Represents a single recorded experiment event for testing and verification.
/// </summary>
public sealed class ExperimentTraceEvent
{
    /// <summary>
    /// Gets or sets the type of service being experimented on.
    /// </summary>
    public Type ServiceType { get; init; } = null!;

    /// <summary>
    /// Gets or sets the name of the method that was invoked.
    /// </summary>
    public string? MethodName { get; init; }

    /// <summary>
    /// Gets or sets the selected trial key (e.g., "control", "true", "variant-a").
    /// </summary>
    public string? SelectedTrialKey { get; init; }

    /// <summary>
    /// Gets or sets the proxy mode used (e.g., "DispatchProxy", "SourceGenerated").
    /// </summary>
    public string? ProxyMode { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the event started.
    /// </summary>
    public DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the event ended.
    /// </summary>
    public DateTimeOffset? EndTime { get; init; }

    /// <summary>
    /// Gets or sets the duration of the invocation.
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Gets or sets whether a fallback occurred.
    /// </summary>
    public bool IsFallback { get; init; }

    /// <summary>
    /// Gets or sets the exception that occurred, if any.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets or sets optional metadata/tags for the event.
    /// </summary>
    public IDictionary<string, object?>? Metadata { get; init; }

    /// <summary>
    /// Gets or sets the trial keys that were attempted (in order).
    /// </summary>
    public IReadOnlyList<string>? AttemptedTrialKeys { get; init; }
}
