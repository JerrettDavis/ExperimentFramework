namespace ExperimentFramework.DataPlane.Abstractions;

/// <summary>
/// Pluggable backplane for publishing experimentation-related data.
/// </summary>
/// <remarks>
/// <para>
/// The data backplane provides a consistent contract for emitting structured
/// experimentation events while allowing consumers to integrate with their
/// existing observability, analytics, and data pipelines.
/// </para>
/// <para>
/// Implementations may buffer events, batch writes, or publish synchronously.
/// Backplane failures should be bounded and configurable to prevent blocking
/// the request path.
/// </para>
/// </remarks>
public interface IDataBackplane
{
    /// <summary>
    /// Publishes an event envelope to the backplane.
    /// </summary>
    /// <param name="envelope">The event envelope to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method should not throw exceptions. Failures should be logged
    /// and handled according to the configured failure mode.
    /// </remarks>
    ValueTask PublishAsync(DataPlaneEnvelope envelope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes any buffered events to the underlying storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask FlushAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the health of the backplane.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing the health status.</returns>
    ValueTask<BackplaneHealth> HealthAsync(CancellationToken cancellationToken = default);
}
