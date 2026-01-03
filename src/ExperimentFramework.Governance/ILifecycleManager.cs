namespace ExperimentFramework.Governance;

/// <summary>
/// Manages experiment lifecycle state transitions.
/// </summary>
public interface ILifecycleManager
{
    /// <summary>
    /// Gets the current lifecycle state of an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <returns>The current lifecycle state, or null if not found.</returns>
    ExperimentLifecycleState? GetState(string experimentName);

    /// <summary>
    /// Gets the lifecycle history of an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <returns>The list of state transitions in chronological order.</returns>
    IReadOnlyList<StateTransition> GetHistory(string experimentName);

    /// <summary>
    /// Transitions an experiment to a new lifecycle state.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="toState">The target lifecycle state.</param>
    /// <param name="actor">The actor performing the transition.</param>
    /// <param name="reason">The reason for the transition.</param>
    /// <param name="metadata">Optional metadata about the transition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the transition is successful.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the transition is not valid.</exception>
    Task TransitionAsync(
        string experimentName,
        ExperimentLifecycleState toState,
        string? actor = null,
        string? reason = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a state transition is allowed.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="toState">The target lifecycle state.</param>
    /// <returns>True if the transition is valid, false otherwise.</returns>
    bool CanTransition(string experimentName, ExperimentLifecycleState toState);

    /// <summary>
    /// Gets the allowed target states from the current state of an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <returns>The list of allowed target states.</returns>
    IReadOnlyList<ExperimentLifecycleState> GetAllowedTransitions(string experimentName);
}
