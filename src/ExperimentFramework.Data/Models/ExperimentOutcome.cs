namespace ExperimentFramework.Data.Models;

/// <summary>
/// Represents a recorded experiment outcome for a specific subject.
/// </summary>
/// <remarks>
/// <para>
/// Each outcome captures:
/// <list type="bullet">
/// <item><description>Which experiment and trial the subject was assigned to</description></item>
/// <item><description>The metric being measured and its value</description></item>
/// <item><description>When the outcome was recorded</description></item>
/// <item><description>Optional metadata for additional context</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ExperimentOutcome
{
    /// <summary>
    /// Gets the unique identifier for this outcome record.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the name of the experiment this outcome belongs to.
    /// </summary>
    public required string ExperimentName { get; init; }

    /// <summary>
    /// Gets the trial key the subject was assigned to (e.g., "control", "variant-a").
    /// </summary>
    public required string TrialKey { get; init; }

    /// <summary>
    /// Gets the unique identifier for the subject (user, session, etc.).
    /// </summary>
    public required string SubjectId { get; init; }

    /// <summary>
    /// Gets the type of outcome recorded.
    /// </summary>
    public required OutcomeType OutcomeType { get; init; }

    /// <summary>
    /// Gets the name of the metric/endpoint being measured.
    /// </summary>
    public required string MetricName { get; init; }

    /// <summary>
    /// Gets the outcome value.
    /// </summary>
    /// <remarks>
    /// Interpretation depends on <see cref="OutcomeType"/>:
    /// <list type="bullet">
    /// <item><description>Binary: 1.0 for success, 0.0 for failure</description></item>
    /// <item><description>Continuous: The actual measurement value</description></item>
    /// <item><description>Count: The count as a double</description></item>
    /// <item><description>Duration: The duration in seconds</description></item>
    /// </list>
    /// </remarks>
    public required double Value { get; init; }

    /// <summary>
    /// Gets the timestamp when the outcome was recorded.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets optional metadata associated with this outcome.
    /// </summary>
    /// <remarks>
    /// Use metadata to store additional context such as:
    /// <list type="bullet">
    /// <item><description>User segments or cohorts</description></item>
    /// <item><description>Device or platform information</description></item>
    /// <item><description>Geographic region</description></item>
    /// <item><description>Any covariates for analysis</description></item>
    /// </list>
    /// </remarks>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <inheritdoc />
    public override string ToString() =>
        $"Outcome[{ExperimentName}/{TrialKey}] {MetricName}={Value} ({OutcomeType}) for {SubjectId}";
}
