namespace ExperimentFramework.Configuration.Models;

/// <summary>
/// Configuration for an experiment endpoint (metric being measured).
/// </summary>
public sealed class EndpointConfig
{
    /// <summary>
    /// Unique name for this endpoint.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Outcome type.
    /// Valid values: "binary", "continuous", "count", "duration".
    /// </summary>
    public required string OutcomeType { get; set; }

    /// <summary>
    /// Description of what this endpoint measures.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Unit of measurement (e.g., "milliseconds", "percent").
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// If true, higher values are considered better.
    /// Mutually exclusive with <see cref="LowerIsBetter"/>.
    /// </summary>
    public bool? HigherIsBetter { get; set; }

    /// <summary>
    /// If true, lower values are considered better.
    /// Mutually exclusive with <see cref="HigherIsBetter"/>.
    /// </summary>
    public bool? LowerIsBetter { get; set; }

    /// <summary>
    /// Minimum clinically important difference (MCID).
    /// </summary>
    public double? MinimumImportantDifference { get; set; }

    /// <summary>
    /// Expected baseline value for the control group.
    /// </summary>
    public double? ExpectedBaselineValue { get; set; }

    /// <summary>
    /// Expected variance in the data.
    /// </summary>
    public double? ExpectedVariance { get; set; }
}
