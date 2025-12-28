using StackExchange.Redis;

namespace ExperimentFramework.Distributed.Redis;

/// <summary>
/// Redis implementation of distributed locking using RedLock algorithm principles.
/// </summary>
public sealed class RedisDistributedLockProvider : IDistributedLockProvider
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _keyPrefix;

    /// <summary>
    /// Creates a new Redis distributed lock provider.
    /// </summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    /// <param name="keyPrefix">Optional key prefix for lock keys.</param>
    public RedisDistributedLockProvider(IConnectionMultiplexer redis, string keyPrefix = "experiment:lock:")
    {
        _redis = redis;
        _keyPrefix = keyPrefix;
    }

    /// <inheritdoc />
    public async ValueTask<IDistributedLockHandle?> TryAcquireAsync(
        string lockName,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var lockId = Guid.NewGuid().ToString("N");
        var key = _keyPrefix + lockName;

        // Try to acquire the lock using SET NX
        var acquired = await db.StringSetAsync(
            key,
            lockId,
            expiration,
            When.NotExists);

        if (acquired)
        {
            return new RedisLockHandle(db, key, lockId);
        }

        return null;
    }

    /// <inheritdoc />
    public async ValueTask<IDistributedLockHandle?> AcquireAsync(
        string lockName,
        TimeSpan expiration,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        var retryDelay = TimeSpan.FromMilliseconds(50);
        var maxRetryDelay = TimeSpan.FromMilliseconds(200);

        while (DateTimeOffset.UtcNow < deadline)
        {
            var handle = await TryAcquireAsync(lockName, expiration, cancellationToken);
            if (handle != null)
                return handle;

            await Task.Delay(retryDelay, cancellationToken);

            // Exponential backoff with max
            retryDelay = TimeSpan.FromMilliseconds(
                Math.Min(retryDelay.TotalMilliseconds * 1.5, maxRetryDelay.TotalMilliseconds));
        }

        return null;
    }

    private sealed class RedisLockHandle(IDatabase db, string key, string lockId) : IDistributedLockHandle
    {
        private bool _released;

        public bool IsAcquired => !_released;
        public string LockId => lockId;

        public async ValueTask<bool> ExtendAsync(TimeSpan extension, CancellationToken cancellationToken = default)
        {
            if (_released) return false;

            // Use Lua script to atomically check and extend
            const string script = """
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('pexpire', KEYS[1], ARGV[2])
                else
                    return 0
                end
                """;

            var result = await db.ScriptEvaluateAsync(
                script,
                [key],
                [lockId, (long)extension.TotalMilliseconds]);

            return (int)result == 1;
        }

        public async ValueTask DisposeAsync()
        {
            if (_released) return;
            _released = true;

            // Use Lua script to atomically check and delete
            const string script = """
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end
                """;

            await db.ScriptEvaluateAsync(script, [key], [lockId]);
        }
    }
}
