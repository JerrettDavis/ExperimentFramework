using ExperimentFramework.Governance.Persistence.Models;

namespace ExperimentFramework.Governance.Persistence;

/// <summary>
/// Defines a durable persistence backplane for experiment governance state.
/// </summary>
/// <remarks>
/// Implementations must provide:
/// - Strong consistency and conflict detection
/// - Immutable history (append-only state transitions, approvals, evaluations)
/// - Safe rollback and replay capabilities
/// - Optional multi-tenant and multi-environment scoping
/// </remarks>
public interface IGovernancePersistenceBackplane
{
    // ===== Experiment State Operations =====

    /// <summary>
    /// Gets the current state of an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="environment">Optional environment identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current experiment state, or null if not found.</returns>
    Task<PersistedExperimentState?> GetExperimentStateAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or updates the current state of an experiment with optimistic concurrency control.
    /// </summary>
    /// <param name="state">The experiment state to save.</param>
    /// <param name="expectedETag">The expected ETag for optimistic concurrency (null for new entities).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or conflict.</returns>
    Task<PersistenceResult<PersistedExperimentState>> SaveExperimentStateAsync(
        PersistedExperimentState state,
        string? expectedETag = null,
        CancellationToken cancellationToken = default);

    // ===== State Transition History Operations (Immutable/Append-Only) =====

    /// <summary>
    /// Appends a state transition to the history.
    /// </summary>
    /// <param name="transition">The state transition to record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the transition is persisted.</returns>
    Task AppendStateTransitionAsync(
        PersistedStateTransition transition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the lifecycle history of an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="environment">Optional environment identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of state transitions in chronological order.</returns>
    Task<IReadOnlyList<PersistedStateTransition>> GetStateTransitionHistoryAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default);

    // ===== Approval Record Operations (Immutable/Append-Only) =====

    /// <summary>
    /// Appends an approval record to the history.
    /// </summary>
    /// <param name="approval">The approval record to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the approval is persisted.</returns>
    Task AppendApprovalRecordAsync(
        PersistedApprovalRecord approval,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all approval records for an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="environment">Optional environment identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of approval records in chronological order.</returns>
    Task<IReadOnlyList<PersistedApprovalRecord>> GetApprovalRecordsAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets approval records for a specific transition.
    /// </summary>
    /// <param name="transitionId">The transition identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of approval records for the transition.</returns>
    Task<IReadOnlyList<PersistedApprovalRecord>> GetApprovalRecordsByTransitionAsync(
        string transitionId,
        CancellationToken cancellationToken = default);

    // ===== Configuration Version Operations (Immutable/Append-Only) =====

    /// <summary>
    /// Appends a configuration version to the history.
    /// </summary>
    /// <param name="version">The configuration version to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the version is persisted.</returns>
    Task AppendConfigurationVersionAsync(
        PersistedConfigurationVersion version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific configuration version.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="versionNumber">The version number.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="environment">Optional environment identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The configuration version, or null if not found.</returns>
    Task<PersistedConfigurationVersion?> GetConfigurationVersionAsync(
        string experimentName,
        int versionNumber,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest configuration version.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="environment">Optional environment identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest configuration version, or null if none exist.</returns>
    Task<PersistedConfigurationVersion?> GetLatestConfigurationVersionAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configuration versions for an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="environment">Optional environment identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of configuration versions in chronological order.</returns>
    Task<IReadOnlyList<PersistedConfigurationVersion>> GetAllConfigurationVersionsAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default);

    // ===== Policy Evaluation History Operations (Immutable/Append-Only) =====

    /// <summary>
    /// Appends a policy evaluation result to the history.
    /// </summary>
    /// <param name="evaluation">The policy evaluation to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the evaluation is persisted.</returns>
    Task AppendPolicyEvaluationAsync(
        PersistedPolicyEvaluation evaluation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all policy evaluation results for an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="environment">Optional environment identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of policy evaluations in chronological order.</returns>
    Task<IReadOnlyList<PersistedPolicyEvaluation>> GetPolicyEvaluationsAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest policy evaluation result for a specific policy.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="policyName">The policy name.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="environment">Optional environment identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest policy evaluation, or null if none exist.</returns>
    Task<PersistedPolicyEvaluation?> GetLatestPolicyEvaluationAsync(
        string experimentName,
        string policyName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default);
}
