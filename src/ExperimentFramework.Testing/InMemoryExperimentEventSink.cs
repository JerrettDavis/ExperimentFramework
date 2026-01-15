using System.Collections.Concurrent;

namespace ExperimentFramework.Testing;

/// <summary>
/// In-memory event sink for capturing experiment execution traces during testing.
/// Thread-safe and suitable for concurrent test scenarios.
/// </summary>
public sealed class InMemoryExperimentEventSink
{
    private readonly ConcurrentBag<ExperimentTraceEvent> _events = new();

    /// <summary>
    /// Gets all recorded events in the order they were added.
    /// </summary>
    public IReadOnlyList<ExperimentTraceEvent> Events => _events.ToList();

    /// <summary>
    /// Records a new experiment trace event.
    /// </summary>
    /// <param name="event">The event to record.</param>
    public void RecordEvent(ExperimentTraceEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        _events.Add(@event);
    }

    /// <summary>
    /// Clears all recorded events.
    /// </summary>
    public void Clear()
    {
        _events.Clear();
    }

    /// <summary>
    /// Gets the count of recorded events.
    /// </summary>
    public int Count => _events.Count;
}
