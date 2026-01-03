namespace ExperimentFramework.Governance.Persistence.Models;

/// <summary>
/// Persisted representation of a configuration version.
/// </summary>
public sealed class PersistedConfigurationVersion
{
    /// <summary>
    /// Gets or sets the experiment name.
    /// </summary>
    public required string ExperimentName { get; init; }

    /// <summary>
    /// Gets or sets the version number (monotonically increasing).
    /// </summary>
    public required int VersionNumber { get; init; }

    /// <summary>
    /// Gets or sets the configuration as a serialized JSON string.
    /// </summary>
    public required string ConfigurationJson { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when this version was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets the actor who created this version.
    /// </summary>
    public string? CreatedBy { get; init; }

    /// <summary>
    /// Gets or sets the change description.
    /// </summary>
    public string? ChangeDescription { get; init; }

    /// <summary>
    /// Gets or sets the lifecycle state at the time of versioning.
    /// </summary>
    public ExperimentLifecycleState? LifecycleState { get; init; }

    /// <summary>
    /// Gets or sets the configuration hash for integrity verification.
    /// </summary>
    public required string ConfigurationHash { get; init; }

    /// <summary>
    /// Gets or sets whether this is a rollback to a previous version.
    /// </summary>
    public bool IsRollback { get; init; }

    /// <summary>
    /// Gets or sets the version this was rolled back from (if IsRollback is true).
    /// </summary>
    public int? RolledBackFrom { get; init; }

    /// <summary>
    /// Gets or sets additional metadata about this version.
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
