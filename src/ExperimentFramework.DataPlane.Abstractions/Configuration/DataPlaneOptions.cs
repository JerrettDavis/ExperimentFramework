namespace ExperimentFramework.DataPlane.Abstractions.Configuration;

/// <summary>
/// Configuration options for the data backplane.
/// </summary>
public sealed class DataPlaneOptions
{
    /// <summary>
    /// Gets or sets whether exposure events are enabled.
    /// </summary>
    public bool EnableExposureEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets whether assignment events are enabled.
    /// </summary>
    public bool EnableAssignmentEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets whether outcome events are enabled.
    /// </summary>
    public bool EnableOutcomeEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets whether analysis signal events are enabled.
    /// </summary>
    public bool EnableAnalysisSignals { get; set; } = true;

    /// <summary>
    /// Gets or sets whether error events are enabled.
    /// </summary>
    public bool EnableErrorEvents { get; set; } = true;

    /// <summary>
    /// Gets or sets the exposure semantics.
    /// </summary>
    public ExposureSemantics ExposureSemantics { get; set; } = ExposureSemantics.OnDecision;

    /// <summary>
    /// Gets or sets the batch size for buffering events.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the flush interval for buffered events.
    /// </summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the failure mode when the backplane is unavailable.
    /// </summary>
    public BackplaneFailureMode FailureMode { get; set; } = BackplaneFailureMode.Drop;

    /// <summary>
    /// Gets or sets the sampling rate (0.0 to 1.0).
    /// </summary>
    /// <remarks>
    /// A value of 1.0 means all events are captured. 0.5 means 50% sampling.
    /// </remarks>
    public double SamplingRate { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets PII redaction rules.
    /// </summary>
    public PiiRedactionOptions PiiRedaction { get; set; } = new();

    /// <summary>
    /// Gets or sets the maximum queue size for buffered events.
    /// </summary>
    /// <remarks>
    /// When the queue is full and FailureMode is Drop, new events are discarded.
    /// </remarks>
    public int MaxQueueSize { get; set; } = 10000;
}

/// <summary>
/// Defines when exposure events are emitted.
/// </summary>
public enum ExposureSemantics
{
    /// <summary>
    /// Emit exposure on variant decision (default).
    /// </summary>
    OnDecision,

    /// <summary>
    /// Emit exposure on first use of the variant.
    /// </summary>
    OnFirstUse,

    /// <summary>
    /// Require explicit exposure logging (user-invoked).
    /// </summary>
    Explicit
}

/// <summary>
/// Defines behavior when the backplane is unavailable.
/// </summary>
public enum BackplaneFailureMode
{
    /// <summary>
    /// Drop events when the backplane is unavailable (non-blocking).
    /// </summary>
    Drop,

    /// <summary>
    /// Block until the backplane becomes available (may impact latency).
    /// </summary>
    Block
}

/// <summary>
/// PII redaction options.
/// </summary>
public sealed class PiiRedactionOptions
{
    /// <summary>
    /// Gets or sets whether to redact subject IDs.
    /// </summary>
    public bool RedactSubjectIds { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to redact tenant IDs.
    /// </summary>
    public bool RedactTenantIds { get; set; } = false;

    /// <summary>
    /// Gets or sets custom field names to redact.
    /// </summary>
    public HashSet<string> RedactFields { get; set; } = new();

    /// <summary>
    /// Gets or sets the redaction placeholder text.
    /// </summary>
    public string RedactionPlaceholder { get; set; } = "[REDACTED]";
}
