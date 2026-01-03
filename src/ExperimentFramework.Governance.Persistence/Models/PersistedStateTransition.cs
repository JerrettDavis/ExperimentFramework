namespace ExperimentFramework.Governance.Persistence.Models;

/// <summary>
/// Persisted representation of a lifecycle transition event.
/// </summary>
public sealed class PersistedStateTransition
{
    /// <summary>
    /// Gets or sets the unique identifier for this transition.
    /// </summary>
    public required string TransitionId { get; init; }

    /// <summary>
    /// Gets or sets the experiment name.
    /// </summary>
    public required string ExperimentName { get; init; }

    /// <summary>
    /// Gets or sets the source state.
    /// </summary>
    public required ExperimentLifecycleState FromState { get; init; }

    /// <summary>
    /// Gets or sets the target state.
    /// </summary>
    public required ExperimentLifecycleState ToState { get; init; }

    /// <summary>
    /// Gets or sets the timestamp of the transition.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets or sets the actor who triggered the transition.
    /// </summary>
    public string? Actor { get; init; }

    /// <summary>
    /// Gets or sets the reason for the transition.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets or sets additional metadata about the transition.
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
