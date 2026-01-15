using System.Collections.Concurrent;

namespace ExperimentFramework.Diagnostics;

/// <summary>
/// In-memory event sink for testing and diagnostics.
/// </summary>
/// <remarks>
/// Supports both bounded (ring buffer) and unbounded storage modes.
/// Thread-safe for concurrent event capture.
/// </remarks>
public sealed class InMemoryExperimentEventSink : IExperimentEventSink
{
    private readonly ConcurrentQueue<ExperimentEvent> _events = new();
    private readonly int? _maxCapacity;
    private int _eventCount;

    /// <summary>
    /// Initializes a new instance of <see cref="InMemoryExperimentEventSink"/> with unbounded storage.
    /// </summary>
    public InMemoryExperimentEventSink()
    {
        _maxCapacity = null;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="InMemoryExperimentEventSink"/> with bounded storage.
    /// </summary>
    /// <param name="maxCapacity">The maximum number of events to retain. When exceeded, oldest events are discarded.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxCapacity"/> is less than 1.</exception>
    public InMemoryExperimentEventSink(int maxCapacity)
    {
        if (maxCapacity < 1)
            throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Max capacity must be at least 1.");

        _maxCapacity = maxCapacity;
    }

    /// <summary>
    /// Gets the current number of events stored.
    /// </summary>
    public int Count => _events.Count;

    /// <summary>
    /// Gets the maximum capacity (null for unbounded).
    /// </summary>
    public int? MaxCapacity => _maxCapacity;

    /// <summary>
    /// Gets all captured events as a read-only snapshot.
    /// </summary>
    public IReadOnlyList<ExperimentEvent> Events => _events.ToArray();

    /// <inheritdoc/>
    public void OnEvent(in ExperimentEvent e)
    {
        _events.Enqueue(e);
        Interlocked.Increment(ref _eventCount);

        // If bounded, enforce capacity by dequeuing oldest events
        if (_maxCapacity.HasValue)
        {
            while (_events.Count > _maxCapacity.Value)
            {
                _events.TryDequeue(out _);
            }
        }
    }

    /// <summary>
    /// Clears all captured events.
    /// </summary>
    public void Clear()
    {
        while (_events.TryDequeue(out _)) { }
        _eventCount = 0;
    }

    /// <summary>
    /// Gets events matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <returns>Matching events.</returns>
    public IReadOnlyList<ExperimentEvent> GetEvents(Func<ExperimentEvent, bool> predicate)
        => _events.Where(predicate).ToArray();

    /// <summary>
    /// Gets events of a specific kind.
    /// </summary>
    /// <param name="kind">The event kind to filter by.</param>
    /// <returns>Matching events.</returns>
    public IReadOnlyList<ExperimentEvent> GetEventsByKind(ExperimentEventKind kind)
        => GetEvents(e => e.Kind == kind);

    /// <summary>
    /// Gets the total number of events captured (including those discarded in bounded mode).
    /// </summary>
    public int TotalEventCount => _eventCount;
}
