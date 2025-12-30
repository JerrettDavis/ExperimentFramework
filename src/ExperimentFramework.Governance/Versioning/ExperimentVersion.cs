namespace ExperimentFramework.Governance.Versioning;

/// <summary>
/// Represents an immutable version of an experiment configuration.
/// </summary>
public sealed class ExperimentVersion
{
    /// <summary>
    /// Gets or sets the version number (monotonically increasing).
    /// </summary>
    public required int VersionNumber { get; init; }

    /// <summary>
    /// Gets or sets the experiment name.
    /// </summary>
    public required string ExperimentName { get; init; }

    /// <summary>
    /// Gets or sets the configuration as a serialized object.
    /// </summary>
    public required object Configuration { get; init; }

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
    /// Gets or sets metadata about this version.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Gets or sets the lifecycle state at the time of versioning.
    /// </summary>
    public ExperimentLifecycleState? LifecycleState { get; init; }
}

/// <summary>
/// Represents a difference between two experiment versions.
/// </summary>
public sealed class VersionDiff
{
    /// <summary>
    /// Gets or sets the source version number.
    /// </summary>
    public required int FromVersion { get; init; }

    /// <summary>
    /// Gets or sets the target version number.
    /// </summary>
    public required int ToVersion { get; init; }

    /// <summary>
    /// Gets or sets the list of changes.
    /// </summary>
    public required IReadOnlyList<ConfigurationChange> Changes { get; init; }
}

/// <summary>
/// Represents a single configuration change.
/// </summary>
public sealed class ConfigurationChange
{
    /// <summary>
    /// Gets or sets the type of change.
    /// </summary>
    public required ChangeType Type { get; init; }

    /// <summary>
    /// Gets or sets the path to the changed property.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets or sets the old value (null for additions).
    /// </summary>
    public object? OldValue { get; init; }

    /// <summary>
    /// Gets or sets the new value (null for deletions).
    /// </summary>
    public object? NewValue { get; init; }
}

/// <summary>
/// The type of configuration change.
/// </summary>
public enum ChangeType
{
    /// <summary>
    /// A property was added.
    /// </summary>
    Added,

    /// <summary>
    /// A property was modified.
    /// </summary>
    Modified,

    /// <summary>
    /// A property was removed.
    /// </summary>
    Removed
}
