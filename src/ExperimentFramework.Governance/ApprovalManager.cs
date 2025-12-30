namespace ExperimentFramework.Governance;

/// <summary>
/// Manages approval gates for experiment lifecycle transitions.
/// </summary>
public interface IApprovalManager
{
    /// <summary>
    /// Registers an approval gate for a specific lifecycle transition.
    /// </summary>
    /// <param name="fromState">The source state (null for any state).</param>
    /// <param name="toState">The target state.</param>
    /// <param name="gate">The approval gate to register.</param>
    void RegisterGate(ExperimentLifecycleState? fromState, ExperimentLifecycleState toState, IApprovalGate gate);

    /// <summary>
    /// Evaluates all approval gates for a lifecycle transition.
    /// </summary>
    /// <param name="context">The approval context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of approval results from all applicable gates.</returns>
    Task<IReadOnlyList<ApprovalResult>> EvaluateAsync(ApprovalContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if all required approvals are satisfied for a transition.
    /// </summary>
    /// <param name="context">The approval context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if all approvals are granted, false otherwise.</returns>
    Task<bool> IsApprovedAsync(ApprovalContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of the approval manager.
/// </summary>
public class ApprovalManager : IApprovalManager
{
    private readonly List<(ExperimentLifecycleState? FromState, ExperimentLifecycleState ToState, IApprovalGate Gate)> _gates = new();

    /// <inheritdoc/>
    public void RegisterGate(ExperimentLifecycleState? fromState, ExperimentLifecycleState toState, IApprovalGate gate)
    {
        if (gate == null)
            throw new ArgumentNullException(nameof(gate));

        lock (_gates)
        {
            _gates.Add((fromState, toState, gate));
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ApprovalResult>> EvaluateAsync(ApprovalContext context, CancellationToken cancellationToken = default)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        List<(ExperimentLifecycleState? FromState, ExperimentLifecycleState ToState, IApprovalGate Gate)> applicableGates;
        lock (_gates)
        {
            applicableGates = _gates
                .Where(g => (g.FromState == null || g.FromState == context.CurrentState) && g.ToState == context.TargetState)
                .ToList();
        }

        var results = new List<ApprovalResult>();
        foreach (var (_, _, gate) in applicableGates)
        {
            var result = await gate.EvaluateAsync(context, cancellationToken);
            results.Add(result);
        }

        return results.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<bool> IsApprovedAsync(ApprovalContext context, CancellationToken cancellationToken = default)
    {
        var results = await EvaluateAsync(context, cancellationToken);
        return results.All(r => r.IsApproved);
    }
}
