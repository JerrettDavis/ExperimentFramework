namespace ExperimentFramework.Data.Models;

/// <summary>
/// Defines the type of outcome being recorded for statistical analysis.
/// </summary>
public enum OutcomeType
{
    /// <summary>
    /// Binary outcome representing success/failure (e.g., converted/not converted).
    /// Value is 1.0 for success, 0.0 for failure.
    /// </summary>
    Binary,

    /// <summary>
    /// Continuous outcome representing a measurement (e.g., revenue, time on page).
    /// Value is the actual measurement.
    /// </summary>
    Continuous,

    /// <summary>
    /// Count outcome representing discrete occurrences (e.g., number of clicks, page views).
    /// Value is the count as a double.
    /// </summary>
    Count,

    /// <summary>
    /// Duration outcome representing time spans (e.g., session length, time to complete).
    /// Value is the duration in seconds.
    /// </summary>
    Duration
}
