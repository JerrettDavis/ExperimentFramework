using System.Diagnostics.Metrics;

namespace ExperimentFramework.Metrics.Exporters;

/// <summary>
/// OpenTelemetry-compatible metrics exporter using System.Diagnostics.Metrics.
/// Integrates seamlessly with OpenTelemetry SDK and other observability platforms.
/// </summary>
public sealed class OpenTelemetryExperimentMetrics : IExperimentMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _counter;
    private readonly Histogram<double> _histogram;

    /// <summary>
    /// Creates a new OpenTelemetry metrics exporter.
    /// </summary>
    /// <param name="meterName">The meter name for the metrics. Defaults to "ExperimentFramework".</param>
    /// <param name="version">The version of the metrics schema. Defaults to "1.0.0".</param>
    public OpenTelemetryExperimentMetrics(string meterName = "ExperimentFramework", string? version = "1.0.0")
    {
        _meter = new Meter(meterName, version);
        _counter = _meter.CreateCounter<long>("experiment_counter", description: "Experiment counter metrics");
        _histogram = _meter.CreateHistogram<double>("experiment_histogram", description: "Experiment histogram metrics");
    }

    /// <inheritdoc/>
    public void IncrementCounter(string name, long value = 1, params KeyValuePair<string, object>[] tags)
    {
        _counter.Add(value, tags!);
    }

    /// <inheritdoc/>
    public void RecordHistogram(string name, double value, params KeyValuePair<string, object>[] tags)
    {
        _histogram.Record(value, tags!);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Gauge functionality is not fully supported in this implementation.
    /// OpenTelemetry gauges require observable callbacks that capture point-in-time values.
    /// This records values into a histogram as a fallback, which accumulates values
    /// rather than replacing them. For true gauge behavior, register an ObservableGauge directly on the Meter.
    /// </remarks>
    public void SetGauge(string name, double value, params KeyValuePair<string, object>[] tags)
    {
        _histogram.Record(value, tags!);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Summaries are represented as histograms in OpenTelemetry.
    /// </remarks>
    public void RecordSummary(string name, double value, params KeyValuePair<string, object>[] tags)
    {
        _histogram.Record(value, tags!);
    }

    /// <summary>
    /// Disposes the underlying Meter.
    /// </summary>
    public void Dispose()
    {
        _meter.Dispose();
    }
}
