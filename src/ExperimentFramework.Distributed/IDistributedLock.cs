namespace ExperimentFramework.Distributed;

/// <summary>
/// Represents an acquired distributed lock.
/// </summary>
/// <remarks>
/// Dispose this object to release the lock.
/// </remarks>
public interface IDistributedLockHandle : IAsyncDisposable
{
    /// <summary>
    /// Gets whether the lock is still held.
    /// </summary>
    bool IsAcquired { get; }

    /// <summary>
    /// Gets the unique identifier for this lock acquisition.
    /// </summary>
    string LockId { get; }

    /// <summary>
    /// Extends the lock duration.
    /// </summary>
    /// <param name="extension">The additional time to hold the lock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the lock was extended; false if it was lost.</returns>
    ValueTask<bool> ExtendAsync(TimeSpan extension, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides distributed locking for experiments.
/// </summary>
/// <remarks>
/// <para>
/// Use this to coordinate exclusive access to resources across multiple instances.
/// Common use cases include:
/// <list type="bullet">
/// <item><description>Ensuring only one instance runs experiment cleanup</description></item>
/// <item><description>Coordinating staged rollout updates</description></item>
/// <item><description>Preventing duplicate processing</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IDistributedLockProvider
{
    /// <summary>
    /// Attempts to acquire a distributed lock.
    /// </summary>
    /// <param name="lockName">The name of the lock.</param>
    /// <param name="expiration">How long to hold the lock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A lock handle if acquired; null if the lock is held by another instance.</returns>
    ValueTask<IDistributedLockHandle?> TryAcquireAsync(
        string lockName,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquires a distributed lock, waiting if necessary.
    /// </summary>
    /// <param name="lockName">The name of the lock.</param>
    /// <param name="expiration">How long to hold the lock.</param>
    /// <param name="timeout">Maximum time to wait for the lock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A lock handle if acquired; null if the timeout elapsed.</returns>
    ValueTask<IDistributedLockHandle?> AcquireAsync(
        string lockName,
        TimeSpan expiration,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
