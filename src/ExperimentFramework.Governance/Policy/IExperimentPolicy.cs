namespace ExperimentFramework.Governance.Policy;

/// <summary>
/// Represents the result of a policy evaluation.
/// </summary>
public sealed class PolicyEvaluationResult
{
    /// <summary>
    /// Gets or sets whether the policy is satisfied.
    /// </summary>
    public required bool IsCompliant { get; init; }

    /// <summary>
    /// Gets or sets the policy name.
    /// </summary>
    public required string PolicyName { get; init; }

    /// <summary>
    /// Gets or sets the reason for the evaluation result.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gets or sets the severity of policy violation (if not compliant).
    /// </summary>
    public PolicyViolationSeverity Severity { get; init; } = PolicyViolationSeverity.Warning;

    /// <summary>
    /// Gets or sets the timestamp of the evaluation.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata about the evaluation.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// The severity of a policy violation.
/// </summary>
public enum PolicyViolationSeverity
{
    /// <summary>
    /// Informational only, no action required.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning, action recommended but not required.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error, action should be taken but transition can proceed.
    /// </summary>
    Error = 2,

    /// <summary>
    /// Critical, transition must be blocked.
    /// </summary>
    Critical = 3
}

/// <summary>
/// Defines a policy that can evaluate experiment state and telemetry.
/// </summary>
public interface IExperimentPolicy
{
    /// <summary>
    /// Gets the policy name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the policy description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Evaluates the policy against the current context.
    /// </summary>
    /// <param name="context">The policy evaluation context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The evaluation result.</returns>
    Task<PolicyEvaluationResult> EvaluateAsync(PolicyContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Context information for policy evaluation.
/// </summary>
public sealed class PolicyContext
{
    /// <summary>
    /// Gets or sets the experiment name.
    /// </summary>
    public required string ExperimentName { get; init; }

    /// <summary>
    /// Gets or sets the current lifecycle state.
    /// </summary>
    public ExperimentLifecycleState? CurrentState { get; init; }

    /// <summary>
    /// Gets or sets the target lifecycle state (for transition policies).
    /// </summary>
    public ExperimentLifecycleState? TargetState { get; init; }

    /// <summary>
    /// Gets or sets telemetry data for the experiment.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Telemetry { get; init; }

    /// <summary>
    /// Gets or sets additional context metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
