namespace ExperimentFramework.DataPlane.Abstractions.Events;

/// <summary>
/// Represents an analysis signal event: statistical or science alerts.
/// </summary>
/// <remarks>
/// Analysis signals include SRM detection, sequential testing checkpoints,
/// peeking warnings, and other statistical or methodological concerns.
/// </remarks>
public sealed class AnalysisSignalEvent
{
    /// <summary>
    /// Schema version for this event type.
    /// </summary>
    public const string SchemaVersion = "1.0.0";

    /// <summary>
    /// Gets or sets the experiment name.
    /// </summary>
    public required string ExperimentName { get; init; }

    /// <summary>
    /// Gets or sets the signal type.
    /// </summary>
    public required AnalysisSignalType SignalType { get; init; }

    /// <summary>
    /// Gets or sets the signal severity.
    /// </summary>
    public required SignalSeverity Severity { get; init; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets or sets the signal message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets or sets additional signal data.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Data { get; init; }
}

/// <summary>
/// Types of analysis signals.
/// </summary>
public enum AnalysisSignalType
{
    /// <summary>
    /// Sample Ratio Mismatch detected.
    /// </summary>
    SampleRatioMismatch,

    /// <summary>
    /// Sequential testing checkpoint reached.
    /// </summary>
    SequentialCheckpoint,

    /// <summary>
    /// Peeking detected before minimum sample size.
    /// </summary>
    PeekingWarning,

    /// <summary>
    /// Minimum sample size reached.
    /// </summary>
    MinimumSampleReached,

    /// <summary>
    /// Power analysis threshold met.
    /// </summary>
    PowerThresholdMet,

    /// <summary>
    /// Other custom signal.
    /// </summary>
    Custom
}

/// <summary>
/// Severity of an analysis signal.
/// </summary>
public enum SignalSeverity
{
    /// <summary>
    /// Informational signal.
    /// </summary>
    Info,

    /// <summary>
    /// Warning signal.
    /// </summary>
    Warning,

    /// <summary>
    /// Error or critical signal.
    /// </summary>
    Error
}
