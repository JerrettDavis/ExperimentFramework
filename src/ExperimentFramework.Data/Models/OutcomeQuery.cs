namespace ExperimentFramework.Data.Models;

/// <summary>
/// Represents query parameters for retrieving experiment outcomes.
/// </summary>
/// <remarks>
/// All filter properties are optional. When null, the filter is not applied.
/// Multiple filters are combined with AND logic.
/// </remarks>
public sealed class OutcomeQuery
{
    /// <summary>
    /// Gets or sets the experiment name to filter by.
    /// </summary>
    public string? ExperimentName { get; init; }

    /// <summary>
    /// Gets or sets the trial key to filter by.
    /// </summary>
    public string? TrialKey { get; init; }

    /// <summary>
    /// Gets or sets the metric name to filter by.
    /// </summary>
    public string? MetricName { get; init; }

    /// <summary>
    /// Gets or sets the subject ID to filter by.
    /// </summary>
    public string? SubjectId { get; init; }

    /// <summary>
    /// Gets or sets the outcome type to filter by.
    /// </summary>
    public OutcomeType? OutcomeType { get; init; }

    /// <summary>
    /// Gets or sets the start of the time range to filter by (inclusive).
    /// </summary>
    public DateTimeOffset? FromTimestamp { get; init; }

    /// <summary>
    /// Gets or sets the end of the time range to filter by (exclusive).
    /// </summary>
    public DateTimeOffset? ToTimestamp { get; init; }

    /// <summary>
    /// Gets or sets the maximum number of results to return.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// Gets or sets the number of results to skip (for pagination).
    /// </summary>
    public int? Offset { get; init; }

    /// <summary>
    /// Gets or sets whether to order results by timestamp descending (newest first).
    /// Default is false (oldest first).
    /// </summary>
    public bool OrderByTimestampDescending { get; init; }

    /// <summary>
    /// Creates a query for all outcomes in an experiment.
    /// </summary>
    public static OutcomeQuery ForExperiment(string experimentName) =>
        new() { ExperimentName = experimentName };

    /// <summary>
    /// Creates a query for all outcomes for a specific metric in an experiment.
    /// </summary>
    public static OutcomeQuery ForMetric(string experimentName, string metricName) =>
        new() { ExperimentName = experimentName, MetricName = metricName };

    /// <summary>
    /// Creates a query for all outcomes for a specific subject.
    /// </summary>
    public static OutcomeQuery ForSubject(string subjectId) =>
        new() { SubjectId = subjectId };
}
