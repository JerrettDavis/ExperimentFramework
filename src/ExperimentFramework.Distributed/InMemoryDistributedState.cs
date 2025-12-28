using System.Collections.Concurrent;

namespace ExperimentFramework.Distributed;

/// <summary>
/// In-memory implementation of distributed state for single-instance deployments.
/// </summary>
/// <remarks>
/// This implementation is useful for development and single-instance deployments.
/// For multi-instance deployments, use a proper distributed implementation like Redis.
/// </remarks>
public sealed class InMemoryDistributedState : IDistributedExperimentState
{
    private readonly ConcurrentDictionary<string, (object Value, DateTimeOffset? Expiration)> _state = new();
    private readonly ConcurrentDictionary<string, long> _counters = new();

    /// <inheritdoc />
    public ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        CleanupExpired();

        if (_state.TryGetValue(key, out var entry))
        {
            if (!entry.Expiration.HasValue || entry.Expiration > DateTimeOffset.UtcNow)
            {
                return ValueTask.FromResult((T?)entry.Value);
            }

            _state.TryRemove(key, out _);
        }

        return ValueTask.FromResult(default(T?));
    }

    /// <inheritdoc />
    public ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var expirationTime = expiration.HasValue
            ? DateTimeOffset.UtcNow + expiration.Value
            : (DateTimeOffset?)null;

        _state[key] = (value!, expirationTime);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _state.TryRemove(key, out _);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<long> IncrementAsync(string key, long delta = 1, CancellationToken cancellationToken = default)
    {
        var newValue = _counters.AddOrUpdate(key, delta, (_, current) => current + delta);
        return ValueTask.FromResult(newValue);
    }

    private void CleanupExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var kvp in _state)
        {
            if (kvp.Value.Expiration.HasValue && kvp.Value.Expiration <= now)
            {
                _state.TryRemove(kvp.Key, out _);
            }
        }
    }
}
