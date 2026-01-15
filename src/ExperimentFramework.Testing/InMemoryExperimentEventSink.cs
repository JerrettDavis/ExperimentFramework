using System.Collections.Concurrent;

namespace ExperimentFramework.Testing;

/// <summary>
/// In-memory event sink for capturing experiment execution traces during testing.
/// Thread-safe and suitable for concurrent test scenarios.
/// </summary>
public sealed class InMemoryExperimentEventSink
{
    private readonly ConcurrentBag<ExperimentTraceEvent> _events = new();
    private IReadOnlyList<ExperimentTraceEvent>? _cachedSnapshot;

    /// <summary>
    /// Gets all recorded events in the order they were added.
    /// </summary>
    /// <remarks>
    /// The returned list is a snapshot of the recorded events at the time of the
    /// first access after a modification. Subsequent accesses reuse the same
    /// snapshot until new events are recorded or the sink is cleared.
    /// </remarks>
    public IReadOnlyList<ExperimentTraceEvent> Events
    {
        get
        {
            var snapshot = _cachedSnapshot;
            if (snapshot is not null)
            {
                return snapshot;
            }

            snapshot = _events.ToList();
            _cachedSnapshot = snapshot;
            return snapshot;
        }
    }

    /// <summary>
    /// Records a new experiment trace event.
    /// </summary>
    /// <param name="event">The event to record.</param>
    public void RecordEvent(ExperimentTraceEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        _events.Add(@event);
        _cachedSnapshot = null;
    }

    /// <summary>
    /// Clears all recorded events.
    /// </summary>
    public void Clear()
    {
        _events.Clear();
        _cachedSnapshot = null;
    }

    /// <summary>
    /// Gets the count of recorded events.
    /// </summary>
    public int Count => _events.Count;
}
