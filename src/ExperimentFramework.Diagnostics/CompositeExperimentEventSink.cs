namespace ExperimentFramework.Diagnostics;

/// <summary>
/// Composite event sink that forwards events to multiple underlying sinks.
/// </summary>
/// <remarks>
/// Events are forwarded to sinks in registration order.
/// If a sink throws an exception, it is caught and other sinks continue to receive events.
/// Thread-safe if all underlying sinks are thread-safe.
/// </remarks>
public sealed class CompositeExperimentEventSink : IExperimentEventSink
{
    private readonly IExperimentEventSink[] _sinks;

    /// <summary>
    /// Initializes a new instance of <see cref="CompositeExperimentEventSink"/>.
    /// </summary>
    /// <param name="sinks">The sinks to forward events to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sinks"/> is null.</exception>
    public CompositeExperimentEventSink(params IExperimentEventSink[] sinks)
    {
        _sinks = sinks ?? throw new ArgumentNullException(nameof(sinks));
    }

    /// <summary>
    /// Initializes a new instance of <see cref="CompositeExperimentEventSink"/>.
    /// </summary>
    /// <param name="sinks">The sinks to forward events to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sinks"/> is null.</exception>
    public CompositeExperimentEventSink(IEnumerable<IExperimentEventSink> sinks)
    {
        if (sinks == null)
            throw new ArgumentNullException(nameof(sinks));

        _sinks = sinks.ToArray();
    }

    /// <summary>
    /// Gets the number of sinks in this composite.
    /// </summary>
    public int SinkCount => _sinks.Length;

    /// <inheritdoc/>
    public void OnEvent(in ExperimentEvent e)
    {
        foreach (var sink in _sinks)
        {
            try
            {
                sink.OnEvent(e);
            }
            catch
            {
                // Swallow exceptions from individual sinks to prevent one
                // failing sink from affecting others
                // In production, consider logging these failures
            }
        }
    }
}
