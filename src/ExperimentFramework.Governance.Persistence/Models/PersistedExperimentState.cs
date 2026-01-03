namespace ExperimentFramework.Governance.Persistence.Models;

/// <summary>
/// Persisted representation of experiment state.
/// </summary>
public sealed class PersistedExperimentState
{
    /// <summary>
    /// Gets or sets the experiment name (unique identifier).
    /// </summary>
    public required string ExperimentName { get; init; }

    /// <summary>
    /// Gets or sets the current lifecycle state.
    /// </summary>
    public required ExperimentLifecycleState CurrentState { get; init; }

    /// <summary>
    /// Gets or sets the configuration version.
    /// </summary>
    public int ConfigurationVersion { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the state was last modified.
    /// </summary>
    public required DateTimeOffset LastModified { get; init; }

    /// <summary>
    /// Gets or sets the last actor who modified the state.
    /// </summary>
    public string? LastModifiedBy { get; init; }

    /// <summary>
    /// Gets or sets the ETag for optimistic concurrency control.
    /// </summary>
    public required string ETag { get; init; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Gets or sets the tenant identifier (for multi-tenancy).
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// Gets or sets the environment identifier (dev, staging, prod).
    /// </summary>
    public string? Environment { get; init; }
}
