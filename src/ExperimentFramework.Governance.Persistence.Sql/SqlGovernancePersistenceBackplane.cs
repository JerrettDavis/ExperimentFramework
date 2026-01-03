using System.Text.Json;
using ExperimentFramework.Governance.Persistence.Models;
using ExperimentFramework.Governance.Persistence.Sql.Entities;
using ExperimentFramework.Governance.Policy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Governance.Persistence.Sql;

/// <summary>
/// SQL-based implementation of governance persistence backplane using Entity Framework Core.
/// </summary>
public sealed class SqlGovernancePersistenceBackplane : IGovernancePersistenceBackplane
{
    private readonly GovernanceDbContext _dbContext;
    private readonly ILogger<SqlGovernancePersistenceBackplane> _logger;

    public SqlGovernancePersistenceBackplane(
        GovernanceDbContext dbContext,
        ILogger<SqlGovernancePersistenceBackplane> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ===== Experiment State Operations =====

    public async Task<PersistedExperimentState?> GetExperimentStateAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ExperimentStates
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.ExperimentName == experimentName &&
                     e.TenantId == (tenantId ?? string.Empty) &&
                     e.Environment == (environment ?? string.Empty),
                cancellationToken);

        return entity == null ? null : MapToPersistedState(entity);
    }

    public async Task<PersistenceResult<PersistedExperimentState>> SaveExperimentStateAsync(
        PersistedExperimentState state,
        string? expectedETag = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = state.TenantId ?? string.Empty;
        var environment = state.Environment ?? string.Empty;

        var newETag = Guid.NewGuid().ToString();

        try
        {
            var existing = await _dbContext.ExperimentStates
                .FirstOrDefaultAsync(
                    e => e.ExperimentName == state.ExperimentName &&
                         e.TenantId == tenantId &&
                         e.Environment == environment,
                    cancellationToken);

            if (existing == null)
            {
                // New entity
                if (expectedETag != null)
                {
                    return PersistenceResult<PersistedExperimentState>.Failure(
                        "Cannot update non-existent entity with expectedETag");
                }

                var newEntity = new ExperimentStateEntity
                {
                    ExperimentName = state.ExperimentName,
                    TenantId = tenantId,
                    Environment = environment,
                    CurrentState = (int)state.CurrentState,
                    ConfigurationVersion = state.ConfigurationVersion,
                    LastModified = state.LastModified,
                    LastModifiedBy = state.LastModifiedBy,
                    ETag = newETag,
                    MetadataJson = SerializeMetadata(state.Metadata)
                };

                _dbContext.ExperimentStates.Add(newEntity);
                await _dbContext.SaveChangesAsync(cancellationToken);

                return PersistenceResult<PersistedExperimentState>.Ok(
                    MapToPersistedState(newEntity), newETag);
            }
            else
            {
                // Update existing
                if (expectedETag != null && existing.ETag != expectedETag)
                {
                    _logger.LogWarning(
                        "Concurrency conflict for experiment {ExperimentName}. Expected ETag: {Expected}, Actual: {Actual}",
                        state.ExperimentName, expectedETag, existing.ETag);
                    return PersistenceResult<PersistedExperimentState>.Conflict(
                        $"ETag mismatch. Expected: {expectedETag}, Actual: {existing.ETag}");
                }

                existing.CurrentState = (int)state.CurrentState;
                existing.ConfigurationVersion = state.ConfigurationVersion;
                existing.LastModified = state.LastModified;
                existing.LastModifiedBy = state.LastModifiedBy;
                existing.ETag = newETag;
                existing.MetadataJson = SerializeMetadata(state.Metadata);

                await _dbContext.SaveChangesAsync(cancellationToken);

                return PersistenceResult<PersistedExperimentState>.Ok(
                    MapToPersistedState(existing), newETag);
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict saving experiment state for {ExperimentName}",
                state.ExperimentName);
            return PersistenceResult<PersistedExperimentState>.Conflict(
                "Concurrency conflict detected during save");
        }
    }

    // ===== State Transition History Operations =====

    public async Task AppendStateTransitionAsync(
        PersistedStateTransition transition,
        CancellationToken cancellationToken = default)
    {
        var entity = new StateTransitionEntity
        {
            TransitionId = transition.TransitionId,
            ExperimentName = transition.ExperimentName,
            FromState = (int)transition.FromState,
            ToState = (int)transition.ToState,
            Timestamp = transition.Timestamp,
            Actor = transition.Actor,
            Reason = transition.Reason,
            MetadataJson = SerializeMetadata(transition.Metadata),
            TenantId = transition.TenantId,
            Environment = transition.Environment
        };

        _dbContext.StateTransitions.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PersistedStateTransition>> GetStateTransitionHistoryAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.StateTransitions
            .AsNoTracking()
            .Where(e => e.ExperimentName == experimentName &&
                       (tenantId == null || e.TenantId == tenantId) &&
                       (environment == null || e.Environment == environment))
            .OrderBy(e => e.Timestamp)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToPersistedTransition).ToList();
    }

    // ===== Approval Record Operations =====

    public async Task AppendApprovalRecordAsync(
        PersistedApprovalRecord approval,
        CancellationToken cancellationToken = default)
    {
        var entity = new ApprovalRecordEntity
        {
            ApprovalId = approval.ApprovalId,
            ExperimentName = approval.ExperimentName,
            TransitionId = approval.TransitionId,
            FromState = approval.FromState.HasValue ? (int)approval.FromState.Value : null,
            ToState = (int)approval.ToState,
            IsApproved = approval.IsApproved,
            Approver = approval.Approver,
            Reason = approval.Reason,
            Timestamp = approval.Timestamp,
            GateName = approval.GateName,
            MetadataJson = SerializeMetadata(approval.Metadata),
            TenantId = approval.TenantId,
            Environment = approval.Environment
        };

        _dbContext.ApprovalRecords.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PersistedApprovalRecord>> GetApprovalRecordsAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.ApprovalRecords
            .AsNoTracking()
            .Where(e => e.ExperimentName == experimentName &&
                       (tenantId == null || e.TenantId == tenantId) &&
                       (environment == null || e.Environment == environment))
            .OrderBy(e => e.Timestamp)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToPersistedApproval).ToList();
    }

    public async Task<IReadOnlyList<PersistedApprovalRecord>> GetApprovalRecordsByTransitionAsync(
        string transitionId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.ApprovalRecords
            .AsNoTracking()
            .Where(e => e.TransitionId == transitionId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToPersistedApproval).ToList();
    }

    // ===== Configuration Version Operations =====

    public async Task AppendConfigurationVersionAsync(
        PersistedConfigurationVersion version,
        CancellationToken cancellationToken = default)
    {
        var entity = new ConfigurationVersionEntity
        {
            ExperimentName = version.ExperimentName,
            VersionNumber = version.VersionNumber,
            ConfigurationJson = version.ConfigurationJson,
            CreatedAt = version.CreatedAt,
            CreatedBy = version.CreatedBy,
            ChangeDescription = version.ChangeDescription,
            LifecycleState = version.LifecycleState.HasValue ? (int)version.LifecycleState.Value : null,
            ConfigurationHash = version.ConfigurationHash,
            IsRollback = version.IsRollback,
            RolledBackFrom = version.RolledBackFrom,
            MetadataJson = SerializeMetadata(version.Metadata),
            TenantId = version.TenantId,
            Environment = version.Environment
        };

        _dbContext.ConfigurationVersions.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PersistedConfigurationVersion?> GetConfigurationVersionAsync(
        string experimentName,
        int versionNumber,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ConfigurationVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.ExperimentName == experimentName &&
                     e.VersionNumber == versionNumber &&
                     (tenantId == null || e.TenantId == tenantId) &&
                     (environment == null || e.Environment == environment),
                cancellationToken);

        return entity == null ? null : MapToPersistedConfigVersion(entity);
    }

    public async Task<PersistedConfigurationVersion?> GetLatestConfigurationVersionAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ConfigurationVersions
            .AsNoTracking()
            .Where(e => e.ExperimentName == experimentName &&
                       (tenantId == null || e.TenantId == tenantId) &&
                       (environment == null || e.Environment == environment))
            .OrderByDescending(e => e.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        return entity == null ? null : MapToPersistedConfigVersion(entity);
    }

    public async Task<IReadOnlyList<PersistedConfigurationVersion>> GetAllConfigurationVersionsAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.ConfigurationVersions
            .AsNoTracking()
            .Where(e => e.ExperimentName == experimentName &&
                       (tenantId == null || e.TenantId == tenantId) &&
                       (environment == null || e.Environment == environment))
            .OrderBy(e => e.VersionNumber)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToPersistedConfigVersion).ToList();
    }

    // ===== Policy Evaluation History Operations =====

    public async Task AppendPolicyEvaluationAsync(
        PersistedPolicyEvaluation evaluation,
        CancellationToken cancellationToken = default)
    {
        var entity = new PolicyEvaluationEntity
        {
            EvaluationId = evaluation.EvaluationId,
            ExperimentName = evaluation.ExperimentName,
            PolicyName = evaluation.PolicyName,
            IsCompliant = evaluation.IsCompliant,
            Reason = evaluation.Reason,
            Severity = (int)evaluation.Severity,
            Timestamp = evaluation.Timestamp,
            CurrentState = evaluation.CurrentState.HasValue ? (int)evaluation.CurrentState.Value : null,
            TargetState = evaluation.TargetState.HasValue ? (int)evaluation.TargetState.Value : null,
            MetadataJson = SerializeMetadata(evaluation.Metadata),
            TenantId = evaluation.TenantId,
            Environment = evaluation.Environment
        };

        _dbContext.PolicyEvaluations.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PersistedPolicyEvaluation>> GetPolicyEvaluationsAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.PolicyEvaluations
            .AsNoTracking()
            .Where(e => e.ExperimentName == experimentName &&
                       (tenantId == null || e.TenantId == tenantId) &&
                       (environment == null || e.Environment == environment))
            .OrderBy(e => e.Timestamp)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToPersistedPolicyEvaluation).ToList();
    }

    public async Task<PersistedPolicyEvaluation?> GetLatestPolicyEvaluationAsync(
        string experimentName,
        string policyName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.PolicyEvaluations
            .AsNoTracking()
            .Where(e => e.ExperimentName == experimentName &&
                       e.PolicyName == policyName &&
                       (tenantId == null || e.TenantId == tenantId) &&
                       (environment == null || e.Environment == environment))
            .OrderByDescending(e => e.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        return entity == null ? null : MapToPersistedPolicyEvaluation(entity);
    }

    // ===== Helper Methods =====

    private static string? SerializeMetadata(IReadOnlyDictionary<string, object>? metadata)
    {
        return metadata == null ? null : JsonSerializer.Serialize(metadata);
    }

    private static IReadOnlyDictionary<string, object>? DeserializeMetadata(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        catch
        {
            return null;
        }
    }

    private static PersistedExperimentState MapToPersistedState(ExperimentStateEntity entity)
    {
        return new PersistedExperimentState
        {
            ExperimentName = entity.ExperimentName,
            CurrentState = (ExperimentLifecycleState)entity.CurrentState,
            ConfigurationVersion = entity.ConfigurationVersion,
            LastModified = entity.LastModified,
            LastModifiedBy = entity.LastModifiedBy,
            ETag = entity.ETag,
            Metadata = DeserializeMetadata(entity.MetadataJson),
            TenantId = string.IsNullOrEmpty(entity.TenantId) ? null : entity.TenantId,
            Environment = string.IsNullOrEmpty(entity.Environment) ? null : entity.Environment
        };
    }

    private static PersistedStateTransition MapToPersistedTransition(StateTransitionEntity entity)
    {
        return new PersistedStateTransition
        {
            TransitionId = entity.TransitionId,
            ExperimentName = entity.ExperimentName,
            FromState = (ExperimentLifecycleState)entity.FromState,
            ToState = (ExperimentLifecycleState)entity.ToState,
            Timestamp = entity.Timestamp,
            Actor = entity.Actor,
            Reason = entity.Reason,
            Metadata = DeserializeMetadata(entity.MetadataJson),
            TenantId = entity.TenantId,
            Environment = entity.Environment
        };
    }

    private static PersistedApprovalRecord MapToPersistedApproval(ApprovalRecordEntity entity)
    {
        return new PersistedApprovalRecord
        {
            ApprovalId = entity.ApprovalId,
            ExperimentName = entity.ExperimentName,
            TransitionId = entity.TransitionId,
            FromState = entity.FromState.HasValue ? (ExperimentLifecycleState)entity.FromState.Value : null,
            ToState = (ExperimentLifecycleState)entity.ToState,
            IsApproved = entity.IsApproved,
            Approver = entity.Approver,
            Reason = entity.Reason,
            Timestamp = entity.Timestamp,
            GateName = entity.GateName,
            Metadata = DeserializeMetadata(entity.MetadataJson),
            TenantId = entity.TenantId,
            Environment = entity.Environment
        };
    }

    private static PersistedConfigurationVersion MapToPersistedConfigVersion(ConfigurationVersionEntity entity)
    {
        return new PersistedConfigurationVersion
        {
            ExperimentName = entity.ExperimentName,
            VersionNumber = entity.VersionNumber,
            ConfigurationJson = entity.ConfigurationJson,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            ChangeDescription = entity.ChangeDescription,
            LifecycleState = entity.LifecycleState.HasValue ? (ExperimentLifecycleState)entity.LifecycleState.Value : null,
            ConfigurationHash = entity.ConfigurationHash,
            IsRollback = entity.IsRollback,
            RolledBackFrom = entity.RolledBackFrom,
            Metadata = DeserializeMetadata(entity.MetadataJson),
            TenantId = entity.TenantId,
            Environment = entity.Environment
        };
    }

    private static PersistedPolicyEvaluation MapToPersistedPolicyEvaluation(PolicyEvaluationEntity entity)
    {
        return new PersistedPolicyEvaluation
        {
            EvaluationId = entity.EvaluationId,
            ExperimentName = entity.ExperimentName,
            PolicyName = entity.PolicyName,
            IsCompliant = entity.IsCompliant,
            Reason = entity.Reason,
            Severity = (PolicyViolationSeverity)entity.Severity,
            Timestamp = entity.Timestamp,
            CurrentState = entity.CurrentState.HasValue ? (ExperimentLifecycleState)entity.CurrentState.Value : null,
            TargetState = entity.TargetState.HasValue ? (ExperimentLifecycleState)entity.TargetState.Value : null,
            Metadata = DeserializeMetadata(entity.MetadataJson),
            TenantId = entity.TenantId,
            Environment = entity.Environment
        };
    }
}
