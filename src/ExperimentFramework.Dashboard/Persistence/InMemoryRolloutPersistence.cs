using System.Collections.Concurrent;
using ExperimentFramework.Dashboard.Abstractions;

namespace ExperimentFramework.Dashboard.Persistence;

/// <summary>
/// In-memory implementation of rollout persistence.
/// </summary>
/// <remarks>
/// This implementation stores rollout configurations in memory.
/// Data will be lost on application restart. Use a database-backed implementation for production.
/// </remarks>
public sealed class InMemoryRolloutPersistence : IRolloutPersistenceBackplane
{
    private readonly ConcurrentDictionary<string, RolloutConfiguration> _rollouts = new();

    /// <inheritdoc />
    public Task<RolloutConfiguration?> GetRolloutConfigAsync(
        string experimentName,
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(experimentName, tenantId);
        _rollouts.TryGetValue(key, out var config);
        return Task.FromResult(config);
    }

    /// <inheritdoc />
    public Task SaveRolloutConfigAsync(
        RolloutConfiguration config,
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(config.ExperimentName, tenantId);
        config.TenantId = tenantId;
        config.LastModified = DateTimeOffset.UtcNow;
        _rollouts[key] = config;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteRolloutConfigAsync(
        string experimentName,
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(experimentName, tenantId);
        _rollouts.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RolloutConfiguration>> GetActiveRolloutsAsync(
        string? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var activeRollouts = _rollouts.Values
            .Where(r => r.Enabled &&
                       r.Status == RolloutStatus.InProgress &&
                       (tenantId == null || r.TenantId == tenantId))
            .ToList();

        return Task.FromResult<IReadOnlyList<RolloutConfiguration>>(activeRollouts);
    }

    private static string GetKey(string experimentName, string? tenantId)
    {
        return tenantId == null ? experimentName : $"{tenantId}:{experimentName}";
    }
}
