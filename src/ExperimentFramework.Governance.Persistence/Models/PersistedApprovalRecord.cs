namespace ExperimentFramework.Governance.Persistence.Models;

/// <summary>
/// Persisted representation of an approval record.
/// </summary>
public sealed class PersistedApprovalRecord
{
    /// <summary>
    /// Gets or sets the unique identifier for this approval.
    /// </summary>
    public required string ApprovalId { get; init; }

    /// <summary>
    /// Gets or sets the experiment name.
    /// </summary>
    public required string ExperimentName { get; init; }

    /// <summary>
    /// Gets or sets the lifecycle transition being approved.
    /// </summary>
    public required string TransitionId { get; init; }

    /// <summary>
    /// Gets or sets the source state.
    /// </summary>
    public ExperimentLifecycleState? FromState { get; init; }

    /// <summary>
    /// Gets or sets the target state.
    /// </summary>
    public required ExperimentLifecycleState ToState { get; init; }

    /// <summary>
    /// Gets or sets whether the approval was granted.
    /// </summary>
    public required bool IsApproved { get; init; }

    /// <summary>
    /// Gets or sets the approver identity.
    /// </summary>
    public string? Approver { get; init; }

    /// <summary>
    /// Gets or sets the reason for the approval decision.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets or sets the timestamp of the approval decision.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets or sets the approval gate name that generated this record.
    /// </summary>
    public required string GateName { get; init; }

    /// <summary>
    /// Gets or sets additional metadata about the approval.
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
