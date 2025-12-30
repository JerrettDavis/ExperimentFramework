namespace ExperimentFramework.DataPlane.Abstractions.Events;

/// <summary>
/// Represents an assignment change event: when a subject's variant assignment changed.
/// </summary>
/// <remarks>
/// Assignment changes indicate that consistency guarantees were violated.
/// This may happen due to identifier changes, experiment config changes,
/// or hash collisions.
/// </remarks>
public sealed class AssignmentEvent
{
    /// <summary>
    /// Schema version for this event type.
    /// </summary>
    public const string SchemaVersion = "1.0.0";

    /// <summary>
    /// Gets or sets the experiment name.
    /// </summary>
    public required string ExperimentName { get; init; }

    /// <summary>
    /// Gets or sets the subject identifier.
    /// </summary>
    public required string SubjectId { get; init; }

    /// <summary>
    /// Gets or sets the previous variant key.
    /// </summary>
    public required string PreviousVariantKey { get; init; }

    /// <summary>
    /// Gets or sets the new variant key.
    /// </summary>
    public required string NewVariantKey { get; init; }

    /// <summary>
    /// Gets or sets the timestamp of the assignment change.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets or sets the reason for the assignment change.
    /// </summary>
    public required string ChangeReason { get; init; }

    /// <summary>
    /// Gets or sets the assignment policy at the time of change.
    /// </summary>
    public AssignmentPolicy AssignmentPolicy { get; init; }

    /// <summary>
    /// Gets or sets additional context.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Attributes { get; init; }
}
