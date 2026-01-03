using ExperimentFramework.Governance.Policy;

namespace ExperimentFramework.Governance.Persistence.Models;

/// <summary>
/// Persisted representation of a policy evaluation result.
/// </summary>
public sealed class PersistedPolicyEvaluation
{
    /// <summary>
    /// Gets or sets the unique identifier for this evaluation.
    /// </summary>
    public required string EvaluationId { get; init; }

    /// <summary>
    /// Gets or sets the experiment name.
    /// </summary>
    public required string ExperimentName { get; init; }

    /// <summary>
    /// Gets or sets the policy name.
    /// </summary>
    public required string PolicyName { get; init; }

    /// <summary>
    /// Gets or sets whether the policy is satisfied.
    /// </summary>
    public required bool IsCompliant { get; init; }

    /// <summary>
    /// Gets or sets the reason for the evaluation result.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets or sets the severity of policy violation (if not compliant).
    /// </summary>
    public required PolicyViolationSeverity Severity { get; init; }

    /// <summary>
    /// Gets or sets the timestamp of the evaluation.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets or sets the current lifecycle state at evaluation time.
    /// </summary>
    public ExperimentLifecycleState? CurrentState { get; init; }

    /// <summary>
    /// Gets or sets the target lifecycle state (for transition policies).
    /// </summary>
    public ExperimentLifecycleState? TargetState { get; init; }

    /// <summary>
    /// Gets or sets additional metadata about the evaluation.
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
