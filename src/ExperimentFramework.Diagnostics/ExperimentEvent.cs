namespace ExperimentFramework.Diagnostics;

/// <summary>
/// Discriminated union-style event representing various experiment lifecycle events.
/// </summary>
/// <remarks>
/// This type uses a discriminated union pattern with the <see cref="Kind"/> property
/// indicating which specific event occurred. Additional properties provide event-specific data.
/// </remarks>
public sealed record ExperimentEvent
{
    /// <summary>
    /// Gets the kind of event that occurred.
    /// </summary>
    public required ExperimentEventKind Kind { get; init; }

    /// <summary>
    /// Gets the timestamp when the event occurred (UTC).
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the service type being invoked.
    /// </summary>
    public required Type ServiceType { get; init; }

    /// <summary>
    /// Gets the method name being called.
    /// </summary>
    public required string MethodName { get; init; }

    /// <summary>
    /// Gets the trial key selected for this invocation.
    /// </summary>
    public required string TrialKey { get; init; }

    /// <summary>
    /// Gets the selector name used for trial selection (feature flag or configuration key).
    /// </summary>
    public string? SelectorName { get; init; }

    /// <summary>
    /// Gets the exception that occurred (for ExceptionThrown and FallbackOccurred events).
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets the fallback trial key (for FallbackOccurred events).
    /// </summary>
    public string? FallbackKey { get; init; }

    /// <summary>
    /// Gets the duration of the operation (for TrialEnded and MethodCompleted events).
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Gets whether the operation succeeded (for TrialEnded and MethodCompleted events).
    /// </summary>
    public bool? Success { get; init; }

    /// <summary>
    /// Gets additional context data for the event.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Context { get; init; }
}

/// <summary>
/// Defines the kinds of experiment events that can be captured.
/// </summary>
public enum ExperimentEventKind
{
    /// <summary>
    /// A trial invocation has started.
    /// </summary>
    TrialStarted = 0,

    /// <summary>
    /// A trial invocation has ended (with success or failure).
    /// </summary>
    TrialEnded = 1,

    /// <summary>
    /// A route (trial key) was selected based on selection rules.
    /// </summary>
    RouteSelected = 2,

    /// <summary>
    /// Execution fell back to another trial due to an error.
    /// </summary>
    FallbackOccurred = 3,

    /// <summary>
    /// An exception was thrown during trial execution.
    /// </summary>
    ExceptionThrown = 4,

    /// <summary>
    /// A method invocation started (decorator-level tracking).
    /// </summary>
    MethodInvoked = 5,

    /// <summary>
    /// A method invocation completed (decorator-level tracking).
    /// </summary>
    MethodCompleted = 6
}
