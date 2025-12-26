using ExperimentFramework.Science.Models.Snapshots;

namespace ExperimentFramework.Science.Snapshots;

/// <summary>
/// Defines the contract for storing and retrieving experiment snapshots.
/// </summary>
public interface ISnapshotStore
{
    /// <summary>
    /// Saves a snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask SaveAsync(ExperimentSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a snapshot by ID.
    /// </summary>
    /// <param name="snapshotId">The snapshot ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The snapshot if found, null otherwise.</returns>
    ValueTask<ExperimentSnapshot?> GetAsync(string snapshotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all snapshots for an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All snapshots for the experiment, ordered by timestamp.</returns>
    ValueTask<IReadOnlyList<ExperimentSnapshot>> ListAsync(string experimentName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest snapshot of a specific type for an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="type">The snapshot type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest snapshot of the type if found, null otherwise.</returns>
    ValueTask<ExperimentSnapshot?> GetLatestAsync(
        string experimentName,
        SnapshotType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a snapshot.
    /// </summary>
    /// <param name="snapshotId">The snapshot ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    ValueTask<bool> DeleteAsync(string snapshotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all snapshots for an experiment.
    /// </summary>
    /// <param name="experimentName">The experiment name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of snapshots deleted.</returns>
    ValueTask<int> DeleteAllAsync(string experimentName, CancellationToken cancellationToken = default);
}
