using System.Collections.Concurrent;

namespace ExperimentFramework.Distributed;

/// <summary>
/// In-memory implementation of distributed locking for single-instance deployments.
/// </summary>
public sealed class InMemoryDistributedLockProvider : IDistributedLockProvider
{
    private readonly ConcurrentDictionary<string, LockEntry> _locks = new();

    /// <inheritdoc />
    public ValueTask<IDistributedLockHandle?> TryAcquireAsync(
        string lockName,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        var lockId = Guid.NewGuid().ToString("N");
        var expiresAt = DateTimeOffset.UtcNow + expiration;
        var entry = new LockEntry(lockId, expiresAt);

        if (_locks.TryAdd(lockName, entry))
        {
            return ValueTask.FromResult<IDistributedLockHandle?>(
                new InMemoryLockHandle(this, lockName, lockId));
        }

        // Check if existing lock has expired
        if (_locks.TryGetValue(lockName, out var existing) && existing.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            if (_locks.TryUpdate(lockName, entry, existing))
            {
                return ValueTask.FromResult<IDistributedLockHandle?>(
                    new InMemoryLockHandle(this, lockName, lockId));
            }
        }

        return ValueTask.FromResult<IDistributedLockHandle?>(null);
    }

    /// <inheritdoc />
    public async ValueTask<IDistributedLockHandle?> AcquireAsync(
        string lockName,
        TimeSpan expiration,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            var handle = await TryAcquireAsync(lockName, expiration, cancellationToken);
            if (handle != null)
                return handle;

            await Task.Delay(50, cancellationToken);
        }

        return null;
    }

    internal bool TryRelease(string lockName, string lockId)
    {
        if (_locks.TryGetValue(lockName, out var entry) && entry.LockId == lockId)
        {
            return _locks.TryRemove(lockName, out _);
        }
        return false;
    }

    internal bool TryExtend(string lockName, string lockId, TimeSpan extension)
    {
        if (_locks.TryGetValue(lockName, out var existing) && existing.LockId == lockId)
        {
            var newEntry = new LockEntry(lockId, DateTimeOffset.UtcNow + extension);
            return _locks.TryUpdate(lockName, newEntry, existing);
        }
        return false;
    }

    private sealed record LockEntry(string LockId, DateTimeOffset ExpiresAt);

    private sealed class InMemoryLockHandle(
        InMemoryDistributedLockProvider provider,
        string lockName,
        string lockId) : IDistributedLockHandle
    {
        private bool _released;

        public bool IsAcquired => !_released;
        public string LockId => lockId;

        public ValueTask<bool> ExtendAsync(TimeSpan extension, CancellationToken cancellationToken = default)
        {
            if (_released) return ValueTask.FromResult(false);
            return ValueTask.FromResult(provider.TryExtend(lockName, lockId, extension));
        }

        public ValueTask DisposeAsync()
        {
            if (!_released)
            {
                _released = true;
                provider.TryRelease(lockName, lockId);
            }
            return ValueTask.CompletedTask;
        }
    }
}
