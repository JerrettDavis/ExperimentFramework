using System.Collections.Concurrent;
using ExperimentFramework.Governance.Persistence.Models;

namespace ExperimentFramework.Governance.Persistence;

/// <summary>
/// In-memory implementation of governance persistence backplane for testing and development.
/// </summary>
/// <remarks>
/// This implementation provides all persistence operations using thread-safe in-memory storage.
/// Data is lost on application restart. Use SQL or Redis backplanes for production scenarios.
/// </remarks>
public sealed class InMemoryGovernancePersistenceBackplane : IGovernancePersistenceBackplane
{
    private readonly ConcurrentDictionary<string, PersistedExperimentState> _experimentStates = new();
    private readonly ConcurrentDictionary<string, List<PersistedStateTransition>> _stateTransitions = new();
    private readonly ConcurrentDictionary<string, List<PersistedApprovalRecord>> _approvalRecords = new();
    private readonly ConcurrentDictionary<string, List<PersistedConfigurationVersion>> _configVersions = new();
    private readonly ConcurrentDictionary<string, List<PersistedPolicyEvaluation>> _policyEvaluations = new();
    private readonly ConcurrentDictionary<string, List<PersistedApprovalRecord>> _approvalsByTransition = new();

    // ===== Experiment State Operations =====

    public Task<PersistedExperimentState?> GetExperimentStateAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(experimentName, tenantId, environment);
        _experimentStates.TryGetValue(key, out var state);
        return Task.FromResult(state);
    }

    public Task<PersistenceResult<PersistedExperimentState>> SaveExperimentStateAsync(
        PersistedExperimentState state,
        string? expectedETag = null,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(state.ExperimentName, state.TenantId, state.Environment);

        // Generate new ETag
        var newETag = Guid.NewGuid().ToString();
        var updatedState = new PersistedExperimentState
        {
            ExperimentName = state.ExperimentName,
            CurrentState = state.CurrentState,
            ConfigurationVersion = state.ConfigurationVersion,
            LastModified = state.LastModified,
            LastModifiedBy = state.LastModifiedBy,
            ETag = newETag,
            Metadata = state.Metadata,
            TenantId = state.TenantId,
            Environment = state.Environment
        };

        if (expectedETag == null)
        {
            // New entity - add only if not exists
            if (_experimentStates.TryAdd(key, updatedState))
            {
                return Task.FromResult(PersistenceResult<PersistedExperimentState>.Ok(updatedState, newETag));
            }
            return Task.FromResult(PersistenceResult<PersistedExperimentState>.Conflict("Entity already exists"));
        }
        else
        {
            // Update with optimistic concurrency check
            var result = _experimentStates.AddOrUpdate(
                key,
                _ => updatedState, // Should not happen with expectedETag
                (_, existing) =>
                {
                    if (existing.ETag != expectedETag)
                    {
                        // Conflict detected - return existing
                        return existing;
                    }
                    return updatedState;
                });

            if (result.ETag == newETag)
            {
                return Task.FromResult(PersistenceResult<PersistedExperimentState>.Ok(updatedState, newETag));
            }
            return Task.FromResult(PersistenceResult<PersistedExperimentState>.Conflict(
                $"ETag mismatch. Expected: {expectedETag}, Actual: {result.ETag}"));
        }
    }

    // ===== State Transition History Operations =====

    public Task AppendStateTransitionAsync(
        PersistedStateTransition transition,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(transition.ExperimentName, transition.TenantId, transition.Environment);
        _stateTransitions.AddOrUpdate(
            key,
            _ => new List<PersistedStateTransition> { transition },
            (_, list) =>
            {
                lock (list)
                {
                    list.Add(transition);
                }
                return list;
            });
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PersistedStateTransition>> GetStateTransitionHistoryAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(experimentName, tenantId, environment);
        if (_stateTransitions.TryGetValue(key, out var transitions))
        {
            lock (transitions)
            {
                return Task.FromResult<IReadOnlyList<PersistedStateTransition>>(
                    transitions.OrderBy(t => t.Timestamp).ToList());
            }
        }
        return Task.FromResult<IReadOnlyList<PersistedStateTransition>>(Array.Empty<PersistedStateTransition>());
    }

    // ===== Approval Record Operations =====

    public Task AppendApprovalRecordAsync(
        PersistedApprovalRecord approval,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(approval.ExperimentName, approval.TenantId, approval.Environment);
        _approvalRecords.AddOrUpdate(
            key,
            _ => new List<PersistedApprovalRecord> { approval },
            (_, list) =>
            {
                lock (list)
                {
                    list.Add(approval);
                }
                return list;
            });

        // Also index by transition ID
        _approvalsByTransition.AddOrUpdate(
            approval.TransitionId,
            _ => new List<PersistedApprovalRecord> { approval },
            (_, list) =>
            {
                lock (list)
                {
                    list.Add(approval);
                }
                return list;
            });

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PersistedApprovalRecord>> GetApprovalRecordsAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(experimentName, tenantId, environment);
        if (_approvalRecords.TryGetValue(key, out var approvals))
        {
            lock (approvals)
            {
                return Task.FromResult<IReadOnlyList<PersistedApprovalRecord>>(
                    approvals.OrderBy(a => a.Timestamp).ToList());
            }
        }
        return Task.FromResult<IReadOnlyList<PersistedApprovalRecord>>(Array.Empty<PersistedApprovalRecord>());
    }

    public Task<IReadOnlyList<PersistedApprovalRecord>> GetApprovalRecordsByTransitionAsync(
        string transitionId,
        CancellationToken cancellationToken = default)
    {
        if (_approvalsByTransition.TryGetValue(transitionId, out var approvals))
        {
            lock (approvals)
            {
                return Task.FromResult<IReadOnlyList<PersistedApprovalRecord>>(
                    approvals.OrderBy(a => a.Timestamp).ToList());
            }
        }
        return Task.FromResult<IReadOnlyList<PersistedApprovalRecord>>(Array.Empty<PersistedApprovalRecord>());
    }

    // ===== Configuration Version Operations =====

    public Task AppendConfigurationVersionAsync(
        PersistedConfigurationVersion version,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(version.ExperimentName, version.TenantId, version.Environment);
        _configVersions.AddOrUpdate(
            key,
            _ => new List<PersistedConfigurationVersion> { version },
            (_, list) =>
            {
                lock (list)
                {
                    list.Add(version);
                }
                return list;
            });
        return Task.CompletedTask;
    }

    public Task<PersistedConfigurationVersion?> GetConfigurationVersionAsync(
        string experimentName,
        int versionNumber,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(experimentName, tenantId, environment);
        if (_configVersions.TryGetValue(key, out var versions))
        {
            lock (versions)
            {
                return Task.FromResult(versions.FirstOrDefault(v => v.VersionNumber == versionNumber));
            }
        }
        return Task.FromResult<PersistedConfigurationVersion?>(null);
    }

    public Task<PersistedConfigurationVersion?> GetLatestConfigurationVersionAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(experimentName, tenantId, environment);
        if (_configVersions.TryGetValue(key, out var versions))
        {
            lock (versions)
            {
                return Task.FromResult(versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault());
            }
        }
        return Task.FromResult<PersistedConfigurationVersion?>(null);
    }

    public Task<IReadOnlyList<PersistedConfigurationVersion>> GetAllConfigurationVersionsAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(experimentName, tenantId, environment);
        if (_configVersions.TryGetValue(key, out var versions))
        {
            lock (versions)
            {
                return Task.FromResult<IReadOnlyList<PersistedConfigurationVersion>>(
                    versions.OrderBy(v => v.VersionNumber).ToList());
            }
        }
        return Task.FromResult<IReadOnlyList<PersistedConfigurationVersion>>(Array.Empty<PersistedConfigurationVersion>());
    }

    // ===== Policy Evaluation History Operations =====

    public Task AppendPolicyEvaluationAsync(
        PersistedPolicyEvaluation evaluation,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(evaluation.ExperimentName, evaluation.TenantId, evaluation.Environment);
        _policyEvaluations.AddOrUpdate(
            key,
            _ => new List<PersistedPolicyEvaluation> { evaluation },
            (_, list) =>
            {
                lock (list)
                {
                    list.Add(evaluation);
                }
                return list;
            });
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PersistedPolicyEvaluation>> GetPolicyEvaluationsAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(experimentName, tenantId, environment);
        if (_policyEvaluations.TryGetValue(key, out var evaluations))
        {
            lock (evaluations)
            {
                return Task.FromResult<IReadOnlyList<PersistedPolicyEvaluation>>(
                    evaluations.OrderBy(e => e.Timestamp).ToList());
            }
        }
        return Task.FromResult<IReadOnlyList<PersistedPolicyEvaluation>>(Array.Empty<PersistedPolicyEvaluation>());
    }

    public Task<PersistedPolicyEvaluation?> GetLatestPolicyEvaluationAsync(
        string experimentName,
        string policyName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(experimentName, tenantId, environment);
        if (_policyEvaluations.TryGetValue(key, out var evaluations))
        {
            lock (evaluations)
            {
                return Task.FromResult(evaluations
                    .Where(e => e.PolicyName == policyName)
                    .OrderByDescending(e => e.Timestamp)
                    .FirstOrDefault());
            }
        }
        return Task.FromResult<PersistedPolicyEvaluation?>(null);
    }

    // ===== Helper Methods =====

    private static string BuildKey(string experimentName, string? tenantId, string? environment)
    {
        var parts = new List<string> { experimentName };
        if (!string.IsNullOrEmpty(tenantId))
            parts.Add(tenantId);
        if (!string.IsNullOrEmpty(environment))
            parts.Add(environment);
        return string.Join("::", parts);
    }
}
