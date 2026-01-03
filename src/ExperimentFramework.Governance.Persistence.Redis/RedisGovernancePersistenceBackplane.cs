using System.Text.Json;
using ExperimentFramework.Governance.Persistence.Models;
using ExperimentFramework.Governance.Policy;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ExperimentFramework.Governance.Persistence.Redis;

/// <summary>
/// Redis-based implementation of governance persistence backplane.
/// </summary>
/// <remarks>
/// Provides fast, distributed persistence for governance state with atomic operations.
/// Suitable for multi-instance coordination and low-latency reads.
/// </remarks>
public sealed class RedisGovernancePersistenceBackplane : IGovernancePersistenceBackplane
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisGovernancePersistenceBackplane> _logger;
    private readonly string _keyPrefix;

    public RedisGovernancePersistenceBackplane(
        IConnectionMultiplexer redis,
        ILogger<RedisGovernancePersistenceBackplane> logger,
        string keyPrefix = "governance:")
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyPrefix = keyPrefix;
    }

    private IDatabase Db => _redis.GetDatabase();

    // ===== Experiment State Operations =====

    public async Task<PersistedExperimentState?> GetExperimentStateAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var key = BuildStateKey(experimentName, tenantId, environment);
        var value = await Db.StringGetAsync(key);

        if (value.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<PersistedExperimentState>((string)value!);
    }

    public async Task<PersistenceResult<PersistedExperimentState>> SaveExperimentStateAsync(
        PersistedExperimentState state,
        string? expectedETag = null,
        CancellationToken cancellationToken = default)
    {
        var key = BuildStateKey(state.ExperimentName, state.TenantId, state.Environment);
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
        var json = JsonSerializer.Serialize(updatedState);

        if (expectedETag == null)
        {
            // New entity - use SET NX (set if not exists)
            var created = await Db.StringSetAsync(key, json, when: When.NotExists);
            if (created)
            {
                return PersistenceResult<PersistedExperimentState>.Ok(updatedState, newETag);
            }
            return PersistenceResult<PersistedExperimentState>.Conflict("Entity already exists");
        }
        else
        {
            // Optimistic concurrency using Redis transactions
            try
            {
                // Get current value to check ETag
                var currentJson = await Db.StringGetAsync(key);
                
                if (currentJson.IsNullOrEmpty)
                {
                    return PersistenceResult<PersistedExperimentState>.Failure("Entity not found");
                }

                var currentState = JsonSerializer.Deserialize<PersistedExperimentState>((string)currentJson!);
                
                if (currentState?.ETag != expectedETag)
                {
                    _logger.LogWarning(
                        "Concurrency conflict for experiment {ExperimentName}. Expected: {Expected}, Actual: {Actual}",
                        state.ExperimentName, expectedETag, currentState?.ETag ?? "null");
                    return PersistenceResult<PersistedExperimentState>.Conflict(
                        $"ETag mismatch. Expected: {expectedETag}, Actual: {currentState?.ETag ?? "null"}");
                }

                // ETag matches, update the value
                await Db.StringSetAsync(key, json);
                return PersistenceResult<PersistedExperimentState>.Ok(updatedState, newETag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving experiment state for {ExperimentName}", state.ExperimentName);
                return PersistenceResult<PersistedExperimentState>.Failure($"Redis error: {ex.Message}");
            }
        }
    }

    // ===== State Transition History Operations =====

    public async Task AppendStateTransitionAsync(
        PersistedStateTransition transition,
        CancellationToken cancellationToken = default)
    {
        var listKey = BuildTransitionsKey(transition.ExperimentName, transition.TenantId, transition.Environment);
        var json = JsonSerializer.Serialize(transition);
        await Db.ListRightPushAsync(listKey, json);
    }

    public async Task<IReadOnlyList<PersistedStateTransition>> GetStateTransitionHistoryAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var listKey = BuildTransitionsKey(experimentName, tenantId, environment);
        var values = await Db.ListRangeAsync(listKey);

        return values
            .Select(v => JsonSerializer.Deserialize<PersistedStateTransition>(v.ToString()))
            .Where(t => t != null)
            .Cast<PersistedStateTransition>()
            .ToList();
    }

    // ===== Approval Record Operations =====

    public async Task AppendApprovalRecordAsync(
        PersistedApprovalRecord approval,
        CancellationToken cancellationToken = default)
    {
        var listKey = BuildApprovalsKey(approval.ExperimentName, approval.TenantId, approval.Environment);
        var transitionKey = BuildApprovalsByTransitionKey(approval.TransitionId);
        var json = JsonSerializer.Serialize(approval);

        var batch = Db.CreateBatch();
        var task1 = batch.ListRightPushAsync(listKey, json);
        var task2 = batch.ListRightPushAsync(transitionKey, json);
        batch.Execute();

        await Task.WhenAll(task1, task2);
    }

    public async Task<IReadOnlyList<PersistedApprovalRecord>> GetApprovalRecordsAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var listKey = BuildApprovalsKey(experimentName, tenantId, environment);
        var values = await Db.ListRangeAsync(listKey);

        return values
            .Select(v => JsonSerializer.Deserialize<PersistedApprovalRecord>(v.ToString()))
            .Where(a => a != null)
            .Cast<PersistedApprovalRecord>()
            .ToList();
    }

    public async Task<IReadOnlyList<PersistedApprovalRecord>> GetApprovalRecordsByTransitionAsync(
        string transitionId,
        CancellationToken cancellationToken = default)
    {
        var listKey = BuildApprovalsByTransitionKey(transitionId);
        var values = await Db.ListRangeAsync(listKey);

        return values
            .Select(v => JsonSerializer.Deserialize<PersistedApprovalRecord>(v.ToString()))
            .Where(a => a != null)
            .Cast<PersistedApprovalRecord>()
            .ToList();
    }

    // ===== Configuration Version Operations =====

    public async Task AppendConfigurationVersionAsync(
        PersistedConfigurationVersion version,
        CancellationToken cancellationToken = default)
    {
        var hashKey = BuildVersionsHashKey(version.ExperimentName, version.TenantId, version.Environment);
        var fieldName = $"v{version.VersionNumber}";
        var json = JsonSerializer.Serialize(version);

        await Db.HashSetAsync(hashKey, fieldName, json);
    }

    public async Task<PersistedConfigurationVersion?> GetConfigurationVersionAsync(
        string experimentName,
        int versionNumber,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var hashKey = BuildVersionsHashKey(experimentName, tenantId, environment);
        var fieldName = $"v{versionNumber}";
        var value = await Db.HashGetAsync(hashKey, fieldName);

        if (value.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<PersistedConfigurationVersion>((string)value!);
    }

    public async Task<PersistedConfigurationVersion?> GetLatestConfigurationVersionAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var hashKey = BuildVersionsHashKey(experimentName, tenantId, environment);
        var entries = await Db.HashGetAllAsync(hashKey);

        if (entries.Length == 0)
            return null;

        var versions = entries
            .Select(e => JsonSerializer.Deserialize<PersistedConfigurationVersion>(e.Value.ToString()))
            .Where(v => v != null)
            .Cast<PersistedConfigurationVersion>()
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefault();

        return versions;
    }

    public async Task<IReadOnlyList<PersistedConfigurationVersion>> GetAllConfigurationVersionsAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var hashKey = BuildVersionsHashKey(experimentName, tenantId, environment);
        var entries = await Db.HashGetAllAsync(hashKey);

        return entries
            .Select(e => JsonSerializer.Deserialize<PersistedConfigurationVersion>(e.Value.ToString()))
            .Where(v => v != null)
            .Cast<PersistedConfigurationVersion>()
            .OrderBy(v => v.VersionNumber)
            .ToList();
    }

    // ===== Policy Evaluation History Operations =====

    public async Task AppendPolicyEvaluationAsync(
        PersistedPolicyEvaluation evaluation,
        CancellationToken cancellationToken = default)
    {
        var listKey = BuildPolicyEvaluationsKey(evaluation.ExperimentName, evaluation.TenantId, evaluation.Environment);
        var json = JsonSerializer.Serialize(evaluation);
        await Db.ListRightPushAsync(listKey, json);
    }

    public async Task<IReadOnlyList<PersistedPolicyEvaluation>> GetPolicyEvaluationsAsync(
        string experimentName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var listKey = BuildPolicyEvaluationsKey(experimentName, tenantId, environment);
        var values = await Db.ListRangeAsync(listKey);

        return values
            .Select(v => JsonSerializer.Deserialize<PersistedPolicyEvaluation>(v.ToString()))
            .Where(e => e != null)
            .Cast<PersistedPolicyEvaluation>()
            .ToList();
    }

    public async Task<PersistedPolicyEvaluation?> GetLatestPolicyEvaluationAsync(
        string experimentName,
        string policyName,
        string? tenantId = null,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var listKey = BuildPolicyEvaluationsKey(experimentName, tenantId, environment);
        var values = await Db.ListRangeAsync(listKey);

        return values
            .Select(v => JsonSerializer.Deserialize<PersistedPolicyEvaluation>(v.ToString()))
            .Where(e => e != null && e.PolicyName == policyName)
            .Cast<PersistedPolicyEvaluation>()
            .OrderByDescending(e => e.Timestamp)
            .FirstOrDefault();
    }

    // ===== Helper Methods =====

    private string BuildStateKey(string experimentName, string? tenantId, string? environment)
    {
        return $"{_keyPrefix}state:{BuildScopeKey(experimentName, tenantId, environment)}";
    }

    private string BuildTransitionsKey(string experimentName, string? tenantId, string? environment)
    {
        return $"{_keyPrefix}transitions:{BuildScopeKey(experimentName, tenantId, environment)}";
    }

    private string BuildApprovalsKey(string experimentName, string? tenantId, string? environment)
    {
        return $"{_keyPrefix}approvals:{BuildScopeKey(experimentName, tenantId, environment)}";
    }

    private string BuildApprovalsByTransitionKey(string transitionId)
    {
        return $"{_keyPrefix}approvals:transition:{transitionId}";
    }

    private string BuildVersionsHashKey(string experimentName, string? tenantId, string? environment)
    {
        return $"{_keyPrefix}versions:{BuildScopeKey(experimentName, tenantId, environment)}";
    }

    private string BuildPolicyEvaluationsKey(string experimentName, string? tenantId, string? environment)
    {
        return $"{_keyPrefix}policies:{BuildScopeKey(experimentName, tenantId, environment)}";
    }

    private static string BuildScopeKey(string experimentName, string? tenantId, string? environment)
    {
        var parts = new List<string> { experimentName };
        if (!string.IsNullOrEmpty(tenantId))
            parts.Add(tenantId);
        if (!string.IsNullOrEmpty(environment))
            parts.Add(environment);
        return string.Join(":", parts);
    }
}
