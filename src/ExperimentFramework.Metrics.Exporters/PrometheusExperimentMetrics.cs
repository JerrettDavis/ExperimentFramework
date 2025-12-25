using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace ExperimentFramework.Metrics.Exporters;

/// <summary>
/// Prometheus-compatible metrics exporter that stores metrics in memory
/// and exposes them in Prometheus text format.
/// </summary>
public sealed class PrometheusExperimentMetrics : IExperimentMetrics
{
    private readonly ConcurrentDictionary<string, CounterMetric> _counters = new();
    private readonly ConcurrentDictionary<string, GaugeMetric> _gauges = new();
    private readonly ConcurrentDictionary<string, HistogramMetric> _histograms = new();
    private readonly ConcurrentDictionary<string, SummaryMetric> _summaries = new();

    /// <inheritdoc/>
    public void IncrementCounter(string name, long value = 1, params KeyValuePair<string, object>[] tags)
    {
        var key = BuildKey(name, tags);
        _counters.AddOrUpdate(key,
            _ => new CounterMetric(name, tags, value),
            (_, existing) =>
            {
                existing.Add(value);
                return existing;
            });
    }

    /// <inheritdoc/>
    public void RecordHistogram(string name, double value, params KeyValuePair<string, object>[] tags)
    {
        var key = BuildKey(name, tags);
        _histograms.AddOrUpdate(key,
            _ => new HistogramMetric(name, tags, value),
            (_, existing) =>
            {
                existing.Record(value);
                return existing;
            });
    }

    /// <inheritdoc/>
    public void SetGauge(string name, double value, params KeyValuePair<string, object>[] tags)
    {
        var key = BuildKey(name, tags);
        _gauges.AddOrUpdate(key,
            _ => new GaugeMetric(name, tags, value),
            (_, existing) =>
            {
                existing.Set(value);
                return existing;
            });
    }

    /// <inheritdoc/>
    public void RecordSummary(string name, double value, params KeyValuePair<string, object>[] tags)
    {
        var key = BuildKey(name, tags);
        _summaries.AddOrUpdate(key,
            _ => new SummaryMetric(name, tags, value),
            (_, existing) =>
            {
                existing.Record(value);
                return existing;
            });
    }

    /// <summary>
    /// Generates Prometheus text format output for all collected metrics.
    /// </summary>
    public string GeneratePrometheusOutput()
    {
        var counterLines = _counters
            .SelectMany(kvp => new[]
            {
                $"# TYPE {kvp.Value.Name} counter",
                $"{kvp.Value.Name}{FormatTags(kvp.Value.Tags)} {kvp.Value.Value}"
            });

        var gaugeLines = _gauges
            .SelectMany(kvp => new[]
            {
                $"# TYPE {kvp.Value.Name} gauge",
                $"{kvp.Value.Name}{FormatTags(kvp.Value.Tags)} {kvp.Value.Value}"
            });

        var histogramLines = _histograms
            .SelectMany(kvp => new[]
            {
                $"# TYPE {kvp.Value.Name} histogram",
                $"{kvp.Value.Name}_sum{FormatTags(kvp.Value.Tags)} {kvp.Value.Sum.ToString(CultureInfo.InvariantCulture)}",
                $"{kvp.Value.Name}_count{FormatTags(kvp.Value.Tags)} {kvp.Value.Count}"
            });

        var summaryLines = _summaries
            .SelectMany(kvp => new[]
            {
                $"# TYPE {kvp.Value.Name} summary",
                $"{kvp.Value.Name}_sum{FormatTags(kvp.Value.Tags)} {kvp.Value.Sum.ToString(CultureInfo.InvariantCulture)}",
                $"{kvp.Value.Name}_count{FormatTags(kvp.Value.Tags)} {kvp.Value.Count}"
            });

        return string.Join(
            Environment.NewLine,
                   counterLines
                       .Concat(gaugeLines)
                       .Concat(histogramLines)
                       .Concat(summaryLines)) ;
    }


    /// <summary>
    /// Clears all collected metrics.
    /// </summary>
    public void Clear()
    {
        _counters.Clear();
        _gauges.Clear();
        _histograms.Clear();
        _summaries.Clear();
    }

    private static string BuildKey(string name, KeyValuePair<string, object>[] tags)
    {
        if (tags.Length == 0)
            return name;

        var sb = new StringBuilder(name);
        foreach (var tag in tags.OrderBy(t => t.Key))
        {
            sb.Append('|');
            sb.Append(tag.Key);
            sb.Append('=');
            sb.Append(tag.Value);
        }

        return sb.ToString();
    }

    private static string FormatTags(KeyValuePair<string, object>[] tags)
    {
        if (tags.Length == 0)
            return string.Empty;

        var sb = new StringBuilder("{");
        for (var i = 0; i < tags.Length; i++)
        {
            if (i > 0)
                sb.Append(',');
            sb.Append(tags[i].Key);
            sb.Append("=\"");
            sb.Append(EscapePrometheusValue(tags[i].Value?.ToString() ?? ""));
            sb.Append('"');
        }

        sb.Append('}');
        return sb.ToString();
    }

    private static string EscapePrometheusValue(string value)
        => value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n");

    private sealed class CounterMetric(string name, KeyValuePair<string, object>[] tags, long initialValue)
    {
        private long _value = initialValue;

        public string Name { get; } = name;
        public KeyValuePair<string, object>[] Tags { get; } = tags;
        public long Value => Interlocked.Read(ref _value);

        public void Add(long value)
        {
            Interlocked.Add(ref _value, value);
        }
    }

    private sealed class GaugeMetric(string name, KeyValuePair<string, object>[] tags, double initialValue)
    {
        private long _value = BitConverter.DoubleToInt64Bits(initialValue);

        public string Name { get; } = name;
        public KeyValuePair<string, object>[] Tags { get; } = tags;
        public double Value => BitConverter.Int64BitsToDouble(Interlocked.Read(ref _value));

        public void Set(double value)
        {
            Interlocked.Exchange(ref _value, BitConverter.DoubleToInt64Bits(value));
        }
    }

    private sealed class HistogramMetric(string name, KeyValuePair<string, object>[] tags, double initialValue)
    {
        private long _sum = BitConverter.DoubleToInt64Bits(initialValue);
        private long _count = 1;

        public string Name { get; } = name;
        public KeyValuePair<string, object>[] Tags { get; } = tags;
        public double Sum => BitConverter.Int64BitsToDouble(Interlocked.Read(ref _sum));
        public long Count => Interlocked.Read(ref _count);

        public void Record(double value)
        {
            // Update sum atomically
            long initialSum, newSum;
            do
            {
                initialSum = Interlocked.Read(ref _sum);
                var currentSum = BitConverter.Int64BitsToDouble(initialSum);
                newSum = BitConverter.DoubleToInt64Bits(currentSum + value);
            } while (Interlocked.CompareExchange(ref _sum, newSum, initialSum) != initialSum);

            Interlocked.Increment(ref _count);
        }
    }

    private sealed class SummaryMetric(string name, KeyValuePair<string, object>[] tags, double initialValue)
    {
        private long _sum = BitConverter.DoubleToInt64Bits(initialValue);
        private long _count = 1;

        public string Name { get; } = name;
        public KeyValuePair<string, object>[] Tags { get; } = tags;
        public double Sum => BitConverter.Int64BitsToDouble(Interlocked.Read(ref _sum));
        public long Count => Interlocked.Read(ref _count);

        public void Record(double value)
        {
            // Update sum atomically
            long initialSum, newSum;
            do
            {
                initialSum = Interlocked.Read(ref _sum);
                var currentSum = BitConverter.Int64BitsToDouble(initialSum);
                newSum = BitConverter.DoubleToInt64Bits(currentSum + value);
            } while (Interlocked.CompareExchange(ref _sum, newSum, initialSum) != initialSum);

            Interlocked.Increment(ref _count);
        }
    }
}