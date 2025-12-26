using ExperimentFramework.Data.Models;

namespace ExperimentFramework.Science.Models.Hypothesis;

/// <summary>
/// Defines a measurable endpoint in an experiment.
/// </summary>
/// <remarks>
/// An endpoint is a specific outcome that is measured to evaluate the effect
/// of the treatment. Primary endpoints determine the main conclusion of the
/// experiment, while secondary endpoints provide additional insights.
/// </remarks>
public sealed class Endpoint
{
    /// <summary>
    /// Gets the name/identifier of this endpoint.
    /// </summary>
    /// <example>"conversion_rate", "response_time_ms", "user_satisfaction"</example>
    public required string Name { get; init; }

    /// <summary>
    /// Gets a human-readable description of this endpoint.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the type of outcome data for this endpoint.
    /// </summary>
    public required OutcomeType OutcomeType { get; init; }

    /// <summary>
    /// Gets the unit of measurement for this endpoint.
    /// </summary>
    /// <example>"milliseconds", "percentage", "count"</example>
    public string? Unit { get; init; }

    /// <summary>
    /// Gets whether a higher value is better for this endpoint.
    /// </summary>
    /// <remarks>
    /// True for metrics like conversion rate, satisfaction score.
    /// False for metrics like error rate, response time.
    /// </remarks>
    public bool HigherIsBetter { get; init; } = true;

    /// <summary>
    /// Gets the minimum clinically important difference (MCID) for this endpoint.
    /// </summary>
    /// <remarks>
    /// The smallest change in the endpoint that would be considered meaningful
    /// in practice, not just statistically significant.
    /// </remarks>
    public double? MinimumImportantDifference { get; init; }

    /// <summary>
    /// Gets the expected baseline value for this endpoint in the control group.
    /// </summary>
    /// <remarks>
    /// Used for power analysis and sample size calculations.
    /// </remarks>
    public double? ExpectedBaselineValue { get; init; }

    /// <summary>
    /// Gets the expected variance for this endpoint.
    /// </summary>
    /// <remarks>
    /// Used for power analysis and sample size calculations.
    /// </remarks>
    public double? ExpectedVariance { get; init; }

    /// <inheritdoc />
    public override string ToString() => Description != null
        ? $"{Name} ({Description})"
        : Name;
}
