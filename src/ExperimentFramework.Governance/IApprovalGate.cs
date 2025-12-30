namespace ExperimentFramework.Governance;

/// <summary>
/// Represents the result of an approval gate evaluation.
/// </summary>
public sealed class ApprovalResult
{
    /// <summary>
    /// Gets or sets whether the approval was granted.
    /// </summary>
    public required bool IsApproved { get; init; }

    /// <summary>
    /// Gets or sets the reason for the approval decision.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets or sets the approver identity.
    /// </summary>
    public string? Approver { get; init; }

    /// <summary>
    /// Gets or sets the timestamp of the approval decision.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata about the approval.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates an approved result.
    /// </summary>
    public static ApprovalResult Approved(string? approver = null, string? reason = null) =>
        new() { IsApproved = true, Approver = approver, Reason = reason };

    /// <summary>
    /// Creates a rejected result.
    /// </summary>
    public static ApprovalResult Rejected(string? approver = null, string? reason = null) =>
        new() { IsApproved = false, Approver = approver, Reason = reason };

    /// <summary>
    /// Creates a pending result (neither approved nor rejected).
    /// </summary>
    public static ApprovalResult Pending(string? reason = null) =>
        new() { IsApproved = false, Reason = reason ?? "Approval pending" };
}

/// <summary>
/// Defines an approval gate that must be satisfied before a lifecycle transition.
/// </summary>
public interface IApprovalGate
{
    /// <summary>
    /// Gets the name of the approval gate.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Evaluates whether approval is granted for a lifecycle transition.
    /// </summary>
    /// <param name="context">The approval context containing experiment and transition information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The approval result.</returns>
    Task<ApprovalResult> EvaluateAsync(ApprovalContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Context information for approval gate evaluation.
/// </summary>
public sealed class ApprovalContext
{
    /// <summary>
    /// Gets or sets the experiment name.
    /// </summary>
    public required string ExperimentName { get; init; }

    /// <summary>
    /// Gets or sets the current lifecycle state.
    /// </summary>
    public required ExperimentLifecycleState CurrentState { get; init; }

    /// <summary>
    /// Gets or sets the target lifecycle state.
    /// </summary>
    public required ExperimentLifecycleState TargetState { get; init; }

    /// <summary>
    /// Gets or sets the actor requesting the transition.
    /// </summary>
    public string? Actor { get; init; }

    /// <summary>
    /// Gets or sets the reason for the transition.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets or sets additional metadata about the transition request.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
