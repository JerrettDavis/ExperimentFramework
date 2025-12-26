using ExperimentFramework.Science.Models.Hypothesis;
using ExperimentFramework.Science.Reporting;

namespace ExperimentFramework.Science.Models.Snapshots;

/// <summary>
/// Represents a point-in-time snapshot of an experiment's state.
/// </summary>
/// <remarks>
/// <para>
/// Snapshots are essential for:
/// <list type="bullet">
/// <item><description>Reproducibility - Capturing exact state for replication</description></item>
/// <item><description>Auditing - Tracking what was known at each analysis point</description></item>
/// <item><description>Sequential analysis - Recording interim looks at the data</description></item>
/// <item><description>Pre-registration - Recording hypothesis before data collection</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ExperimentSnapshot
{
    /// <summary>
    /// Gets the unique identifier for this snapshot.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the experiment name.
    /// </summary>
    public required string ExperimentName { get; init; }

    /// <summary>
    /// Gets when this snapshot was taken.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the type of snapshot.
    /// </summary>
    public required SnapshotType Type { get; init; }

    /// <summary>
    /// Gets an optional description of this snapshot.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the hypothesis definition at the time of snapshot.
    /// </summary>
    public HypothesisDefinition? Hypothesis { get; init; }

    /// <summary>
    /// Gets the experiment configuration at the time of snapshot.
    /// </summary>
    public ExperimentConfiguration? Configuration { get; init; }

    /// <summary>
    /// Gets sample sizes at the time of snapshot.
    /// </summary>
    public IReadOnlyDictionary<string, int>? SampleSizes { get; init; }

    /// <summary>
    /// Gets the analysis report if this is an analysis snapshot.
    /// </summary>
    public ExperimentReport? Report { get; init; }

    /// <summary>
    /// Gets the environment info at the time of snapshot.
    /// </summary>
    public EnvironmentInfo? Environment { get; init; }

    /// <summary>
    /// Gets a hash of the data at the time of snapshot for integrity verification.
    /// </summary>
    public string? DataHash { get; init; }

    /// <summary>
    /// Gets the version of the experiment framework.
    /// </summary>
    public string? FrameworkVersion { get; init; }

    /// <summary>
    /// Gets any notes or comments for this snapshot.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Gets tags for categorizing snapshots.
    /// </summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>
    /// Gets additional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// The type of experiment snapshot.
/// </summary>
public enum SnapshotType
{
    /// <summary>
    /// Pre-registration snapshot taken before data collection.
    /// </summary>
    PreRegistration,

    /// <summary>
    /// Configuration snapshot when experiment is set up.
    /// </summary>
    Configuration,

    /// <summary>
    /// Interim analysis during the experiment.
    /// </summary>
    InterimAnalysis,

    /// <summary>
    /// Final analysis at experiment completion.
    /// </summary>
    FinalAnalysis,

    /// <summary>
    /// Snapshot taken when experiment is stopped early.
    /// </summary>
    EarlyStopping,

    /// <summary>
    /// Ad-hoc snapshot for debugging or auditing.
    /// </summary>
    AdHoc
}

/// <summary>
/// Experiment configuration details.
/// </summary>
public sealed class ExperimentConfiguration
{
    /// <summary>
    /// Gets the experiment conditions/variants.
    /// </summary>
    public IReadOnlyList<string>? Conditions { get; init; }

    /// <summary>
    /// Gets the traffic allocation per condition.
    /// </summary>
    public IReadOnlyDictionary<string, double>? TrafficAllocation { get; init; }

    /// <summary>
    /// Gets the selection mode configuration.
    /// </summary>
    public string? SelectionMode { get; init; }

    /// <summary>
    /// Gets the primary metric name.
    /// </summary>
    public string? PrimaryMetric { get; init; }

    /// <summary>
    /// Gets guardrail metrics.
    /// </summary>
    public IReadOnlyList<string>? GuardrailMetrics { get; init; }

    /// <summary>
    /// Gets the minimum sample size configured.
    /// </summary>
    public int? MinimumSampleSize { get; init; }

    /// <summary>
    /// Gets the maximum duration configured.
    /// </summary>
    public TimeSpan? MaximumDuration { get; init; }

    /// <summary>
    /// Gets additional configuration settings.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Settings { get; init; }
}
