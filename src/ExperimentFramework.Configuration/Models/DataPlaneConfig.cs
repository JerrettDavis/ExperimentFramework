namespace ExperimentFramework.Configuration.Models;

/// <summary>
/// Configuration for the data plane in the DSL.
/// </summary>
public sealed class DataPlaneConfig
{
    /// <summary>
    /// Gets or sets the backplane configuration.
    /// </summary>
    public DataPlaneBackplaneConfig? Backplane { get; set; }

    /// <summary>
    /// Gets or sets whether exposure events are enabled.
    /// </summary>
    public bool? EnableExposureEvents { get; set; }

    /// <summary>
    /// Gets or sets whether assignment events are enabled.
    /// </summary>
    public bool? EnableAssignmentEvents { get; set; }

    /// <summary>
    /// Gets or sets whether outcome events are enabled.
    /// </summary>
    public bool? EnableOutcomeEvents { get; set; }

    /// <summary>
    /// Gets or sets whether analysis signal events are enabled.
    /// </summary>
    public bool? EnableAnalysisSignals { get; set; }

    /// <summary>
    /// Gets or sets whether error events are enabled.
    /// </summary>
    public bool? EnableErrorEvents { get; set; }

    /// <summary>
    /// Gets or sets the sampling rate (0.0 to 1.0).
    /// </summary>
    public double? SamplingRate { get; set; }

    /// <summary>
    /// Gets or sets the batch size for buffering events.
    /// </summary>
    public int? BatchSize { get; set; }

    /// <summary>
    /// Gets or sets the flush interval in milliseconds.
    /// </summary>
    public int? FlushIntervalMs { get; set; }
}
