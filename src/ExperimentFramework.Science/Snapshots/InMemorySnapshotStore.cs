using System.Collections.Concurrent;
using ExperimentFramework.Science.Models.Snapshots;

namespace ExperimentFramework.Science.Snapshots;

/// <summary>
/// Thread-safe in-memory implementation of snapshot storage.
/// </summary>
/// <remarks>
/// Suitable for testing, development, and short-lived experiments.
/// For production use with durability requirements, implement a persistent store.
/// </remarks>
public sealed class InMemorySnapshotStore : ISnapshotStore
{
    private readonly ConcurrentDictionary<string, ExperimentSnapshot> _snapshots = new();
    private readonly ConcurrentDictionary<string, List<string>> _experimentIndex = new();
    private readonly object _indexLock = new();

    /// <inheritdoc />
    public ValueTask SaveAsync(ExperimentSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        _snapshots[snapshot.Id] = snapshot;

        lock (_indexLock)
        {
            if (!_experimentIndex.TryGetValue(snapshot.ExperimentName, out var list))
            {
                list = [];
                _experimentIndex[snapshot.ExperimentName] = list;
            }

            if (!list.Contains(snapshot.Id))
            {
                list.Add(snapshot.Id);
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<ExperimentSnapshot?> GetAsync(string snapshotId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotId);

        _snapshots.TryGetValue(snapshotId, out var snapshot);
        return ValueTask.FromResult(snapshot);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<ExperimentSnapshot>> ListAsync(
        string experimentName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(experimentName);

        if (!_experimentIndex.TryGetValue(experimentName, out var ids))
        {
            return ValueTask.FromResult<IReadOnlyList<ExperimentSnapshot>>(Array.Empty<ExperimentSnapshot>());
        }

        var snapshots = ids
            .Where(id => _snapshots.ContainsKey(id))
            .Select(id => _snapshots[id])
            .OrderBy(s => s.Timestamp)
            .ToList();

        return ValueTask.FromResult<IReadOnlyList<ExperimentSnapshot>>(snapshots);
    }

    /// <inheritdoc />
    public ValueTask<ExperimentSnapshot?> GetLatestAsync(
        string experimentName,
        SnapshotType type,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(experimentName);

        if (!_experimentIndex.TryGetValue(experimentName, out var ids))
        {
            return ValueTask.FromResult<ExperimentSnapshot?>(null);
        }

        var latest = ids
            .Where(id => _snapshots.TryGetValue(id, out var s) && s.Type == type)
            .Select(id => _snapshots[id])
            .OrderByDescending(s => s.Timestamp)
            .FirstOrDefault();

        return ValueTask.FromResult(latest);
    }

    /// <inheritdoc />
    public ValueTask<bool> DeleteAsync(string snapshotId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotId);

        if (!_snapshots.TryRemove(snapshotId, out var snapshot))
        {
            return ValueTask.FromResult(false);
        }

        lock (_indexLock)
        {
            if (_experimentIndex.TryGetValue(snapshot.ExperimentName, out var list))
            {
                list.Remove(snapshotId);
            }
        }

        return ValueTask.FromResult(true);
    }

    /// <inheritdoc />
    public ValueTask<int> DeleteAllAsync(string experimentName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(experimentName);

        if (!_experimentIndex.TryRemove(experimentName, out var ids))
        {
            return ValueTask.FromResult(0);
        }

        var count = ids.Count(id => _snapshots.TryRemove(id, out _));
        return ValueTask.FromResult(count);
    }

    /// <summary>
    /// Gets the total number of snapshots stored.
    /// </summary>
    public int Count => _snapshots.Count;

    /// <summary>
    /// Clears all snapshots.
    /// </summary>
    public void Clear()
    {
        _snapshots.Clear();
        _experimentIndex.Clear();
    }
}
