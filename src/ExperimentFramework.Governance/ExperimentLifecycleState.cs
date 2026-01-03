namespace ExperimentFramework.Governance;

/// <summary>
/// Represents the lifecycle state of an experiment.
/// </summary>
/// <remarks>
/// Experiments progress through well-defined states to support governance and change management.
/// State transitions are validated and auditable.
/// </remarks>
public enum ExperimentLifecycleState
{
    /// <summary>
    /// Experiment is being drafted and not yet ready for review.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Experiment has been submitted for approval.
    /// </summary>
    PendingApproval = 1,

    /// <summary>
    /// Experiment has been approved and is ready to start.
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Experiment is actively running with stable traffic allocation.
    /// </summary>
    Running = 3,

    /// <summary>
    /// Experiment is being ramped (increasing traffic allocation).
    /// </summary>
    Ramping = 4,

    /// <summary>
    /// Experiment has been temporarily paused.
    /// </summary>
    Paused = 5,

    /// <summary>
    /// Experiment was rolled back due to issues.
    /// </summary>
    RolledBack = 6,

    /// <summary>
    /// Experiment has been completed and archived.
    /// </summary>
    Archived = 7,

    /// <summary>
    /// Experiment was rejected during approval process.
    /// </summary>
    Rejected = 8
}

/// <summary>
/// Represents a transition between lifecycle states.
/// </summary>
public sealed class StateTransition
{
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
}
