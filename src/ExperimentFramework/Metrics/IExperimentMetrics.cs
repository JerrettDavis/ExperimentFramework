namespace ExperimentFramework.Metrics;

/// <summary>
/// Interface for recording experiment metrics.
/// </summary>
public interface IExperimentMetrics
{
    /// <summary>
    /// Increments a counter metric.
    /// </summary>
    void IncrementCounter(string name, long value = 1, params KeyValuePair<string, object>[] tags);

    /// <summary>
    /// Records a histogram value (for latency, duration, etc.).
    /// </summary>
    void RecordHistogram(string name, double value, params KeyValuePair<string, object>[] tags);

    /// <summary>
    /// Sets a gauge value (for current state, active experiments, etc.).
    /// </summary>
    void SetGauge(string name, double value, params KeyValuePair<string, object>[] tags);

    /// <summary>
    /// Records a summary value.
    /// </summary>
    void RecordSummary(string name, double value, params KeyValuePair<string, object>[] tags);
}

/// <summary>
/// No-op implementation of IExperimentMetrics for zero overhead when metrics are disabled.
/// </summary>
public sealed class NoopExperimentMetrics : IExperimentMetrics
{
    /// <summary>
    /// Gets the singleton instance of the no-op metrics implementation.
    /// </summary>
    public static readonly NoopExperimentMetrics Instance = new();

    private NoopExperimentMetrics() { }

    /// <inheritdoc />
    public void IncrementCounter(string name, long value = 1, params KeyValuePair<string, object>[] tags) { }

    /// <inheritdoc />
    public void RecordHistogram(string name, double value, params KeyValuePair<string, object>[] tags) { }

    /// <inheritdoc />
    public void SetGauge(string name, double value, params KeyValuePair<string, object>[] tags) { }

    /// <inheritdoc />
    public void RecordSummary(string name, double value, params KeyValuePair<string, object>[] tags) { }
}
