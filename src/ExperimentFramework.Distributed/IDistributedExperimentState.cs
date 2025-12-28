namespace ExperimentFramework.Distributed;

/// <summary>
/// Provides distributed state management for experiments.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to share experiment state across multiple application instances.
/// This is essential for consistent behavior in load-balanced or horizontally-scaled environments.
/// </para>
/// </remarks>
public interface IDistributedExperimentState
{
    /// <summary>
    /// Gets a value from distributed state.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="key">The state key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The value if found; otherwise default.</returns>
    ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in distributed state.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="key">The state key.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="expiration">Optional expiration time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from distributed state.
    /// </summary>
    /// <param name="key">The state key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically increments a counter.
    /// </summary>
    /// <param name="key">The counter key.</param>
    /// <param name="delta">The amount to increment by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new counter value.</returns>
    ValueTask<long> IncrementAsync(string key, long delta = 1, CancellationToken cancellationToken = default);
}
