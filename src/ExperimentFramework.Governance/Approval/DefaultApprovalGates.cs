namespace ExperimentFramework.Governance.Approval;

/// <summary>
/// An approval gate that always approves transitions automatically.
/// </summary>
public class AutomaticApprovalGate : IApprovalGate
{
    /// <inheritdoc/>
    public string Name => "Automatic";

    /// <inheritdoc/>
    public Task<ApprovalResult> EvaluateAsync(ApprovalContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ApprovalResult.Approved("system", "Automatic approval"));
    }
}

/// <summary>
/// An approval gate that requires manual approval (always returns pending).
/// </summary>
/// <remarks>
/// This gate should be used with an external approval system that can update the approval status.
/// </remarks>
public class ManualApprovalGate : IApprovalGate
{
    private readonly Dictionary<string, ApprovalResult> _approvals = new();

    /// <inheritdoc/>
    public string Name => "Manual";

    /// <inheritdoc/>
    public Task<ApprovalResult> EvaluateAsync(ApprovalContext context, CancellationToken cancellationToken = default)
    {
        var key = GetKey(context);
        lock (_approvals)
        {
            if (_approvals.TryGetValue(key, out var result))
            {
                return Task.FromResult(result);
            }
        }

        return Task.FromResult(ApprovalResult.Pending("Manual approval required"));
    }

    /// <summary>
    /// Records a manual approval decision.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="targetState">The target state.</param>
    /// <param name="result">The approval result.</param>
    public void RecordApproval(string experimentName, ExperimentLifecycleState targetState, ApprovalResult result)
    {
        var key = $"{experimentName}:{targetState}";
        lock (_approvals)
        {
            _approvals[key] = result;
        }
    }

    /// <summary>
    /// Clears approval for a specific experiment and state.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="targetState">The target state.</param>
    public void ClearApproval(string experimentName, ExperimentLifecycleState targetState)
    {
        var key = $"{experimentName}:{targetState}";
        lock (_approvals)
        {
            _approvals.Remove(key);
        }
    }

    private static string GetKey(ApprovalContext context) => $"{context.ExperimentName}:{context.TargetState}";
}

/// <summary>
/// An approval gate that checks if the actor has a specific role.
/// </summary>
public class RoleBasedApprovalGate : IApprovalGate
{
    private readonly HashSet<string> _allowedRoles;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleBasedApprovalGate"/> class.
    /// </summary>
    /// <param name="allowedRoles">The roles that are allowed to approve.</param>
    public RoleBasedApprovalGate(params string[] allowedRoles)
    {
        _allowedRoles = new HashSet<string>(allowedRoles, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public string Name => "RoleBased";

    /// <inheritdoc/>
    public Task<ApprovalResult> EvaluateAsync(ApprovalContext context, CancellationToken cancellationToken = default)
    {
        // Check if actor metadata contains a role claim
        if (context.Metadata != null &&
            context.Metadata.TryGetValue("actorRole", out var roleObj) &&
            roleObj is string role &&
            _allowedRoles.Contains(role))
        {
            return Task.FromResult(ApprovalResult.Approved(context.Actor, $"Approved by role: {role}"));
        }

        return Task.FromResult(ApprovalResult.Rejected(context.Actor, "Insufficient role privileges"));
    }
}
