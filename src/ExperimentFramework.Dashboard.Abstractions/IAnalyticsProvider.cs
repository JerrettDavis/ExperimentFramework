namespace ExperimentFramework.Dashboard.Abstractions;

/// <summary>
/// Provides access to analytics data from the data backplane.
/// </summary>
public interface IAnalyticsProvider
{
    /// <summary>
    /// Gets assignment events for an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="tenantId">The tenant identifier, or null for the default tenant.</param>
    /// <param name="start">The start date/time filter.</param>
    /// <param name="end">The end date/time filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of assignment events.</returns>
    Task<IEnumerable<AssignmentEvent>> GetAssignmentsAsync(
        string experimentName,
        string? tenantId = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets exposure events for an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="tenantId">The tenant identifier, or null for the default tenant.</param>
    /// <param name="start">The start date/time filter.</param>
    /// <param name="end">The end date/time filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of exposure events.</returns>
    Task<IEnumerable<ExposureEvent>> GetExposuresAsync(
        string experimentName,
        string? tenantId = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets analysis signal events for an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="tenantId">The tenant identifier, or null for the default tenant.</param>
    /// <param name="start">The start date/time filter.</param>
    /// <param name="end">The end date/time filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of analysis signal events.</returns>
    Task<IEnumerable<AnalysisSignalEvent>> GetAnalysisSignalsAsync(
        string experimentName,
        string? tenantId = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an assignment event.
/// </summary>
public sealed class AssignmentEvent
{
    /// <summary>
    /// Gets or sets the experiment name.
    /// </summary>
    public required string ExperimentName { get; init; }

    /// <summary>
    /// Gets or sets the subject identifier (user/session).
    /// </summary>
    public required string SubjectId { get; init; }

    /// <summary>
    /// Gets or sets the assigned trial key.
    /// </summary>
    public required string TrialKey { get; init; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Represents an exposure event.
/// </summary>
public sealed class ExposureEvent
{
    /// <summary>
    /// Gets or sets the experiment name.
    /// </summary>
    public required string ExperimentName { get; init; }

    /// <summary>
    /// Gets or sets the subject identifier (user/session).
    /// </summary>
    public required string SubjectId { get; init; }

    /// <summary>
    /// Gets or sets the trial key.
    /// </summary>
    public required string TrialKey { get; init; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Represents an analysis signal event (metric observation).
/// </summary>
public sealed class AnalysisSignalEvent
{
    /// <summary>
    /// Gets or sets the experiment name.
    /// </summary>
    public required string ExperimentName { get; init; }

    /// <summary>
    /// Gets or sets the subject identifier (user/session).
    /// </summary>
    public required string SubjectId { get; init; }

    /// <summary>
    /// Gets or sets the trial key.
    /// </summary>
    public required string TrialKey { get; init; }

    /// <summary>
    /// Gets or sets the metric name.
    /// </summary>
    public required string MetricName { get; init; }

    /// <summary>
    /// Gets or sets the metric value.
    /// </summary>
    public double Value { get; init; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
