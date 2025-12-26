namespace ExperimentFramework.Data.Recording;

/// <summary>
/// Configuration options for the outcome recorder.
/// </summary>
public sealed class OutcomeRecorderOptions
{
    /// <summary>
    /// Gets or sets whether to automatically generate outcome IDs.
    /// Default is true.
    /// </summary>
    public bool AutoGenerateIds { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to automatically set timestamps if not provided.
    /// Default is true.
    /// </summary>
    public bool AutoSetTimestamps { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to collect invocation duration automatically.
    /// Used by the decorator for automatic collection.
    /// Default is true.
    /// </summary>
    public bool CollectDuration { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to collect error information automatically.
    /// Used by the decorator for automatic collection.
    /// Default is true.
    /// </summary>
    public bool CollectErrors { get; set; } = true;

    /// <summary>
    /// Gets or sets the default metric name for duration outcomes.
    /// Default is "duration_seconds".
    /// </summary>
    public string DurationMetricName { get; set; } = "duration_seconds";

    /// <summary>
    /// Gets or sets the default metric name for error outcomes.
    /// Default is "error".
    /// </summary>
    public string ErrorMetricName { get; set; } = "error";

    /// <summary>
    /// Gets or sets the default metric name for success outcomes.
    /// Default is "success".
    /// </summary>
    public string SuccessMetricName { get; set; } = "success";

    /// <summary>
    /// Gets or sets whether to batch writes for performance.
    /// Default is false.
    /// </summary>
    public bool EnableBatching { get; set; }

    /// <summary>
    /// Gets or sets the maximum batch size when batching is enabled.
    /// Default is 100.
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum time to wait before flushing a batch.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan BatchFlushInterval { get; set; } = TimeSpan.FromSeconds(5);
}
