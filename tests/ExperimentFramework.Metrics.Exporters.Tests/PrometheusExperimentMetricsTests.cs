using ExperimentFramework.Metrics.Exporters;

namespace ExperimentFramework.Metrics.Exporters.Tests;

/// <summary>
/// Unit tests for <see cref="PrometheusExperimentMetrics"/>.
/// </summary>
public sealed class PrometheusExperimentMetricsTests
{
    // -----------------------------------------------------------------------
    // Counter
    // -----------------------------------------------------------------------

    [Fact]
    public void IncrementCounter_single_call_appears_in_output()
    {
        var m = new PrometheusExperimentMetrics();
        m.IncrementCounter("requests_total", 1);

        var output = m.GeneratePrometheusOutput();

        Assert.Contains("requests_total", output);
    }

    [Fact]
    public void IncrementCounter_multiple_calls_accumulate()
    {
        var m = new PrometheusExperimentMetrics();
        m.IncrementCounter("requests_total", 3);
        m.IncrementCounter("requests_total", 7);

        var output = m.GeneratePrometheusOutput();

        // Sum should be 10
        Assert.Contains("10", output);
    }

    [Fact]
    public void IncrementCounter_output_contains_TYPE_counter_declaration()
    {
        var m = new PrometheusExperimentMetrics();
        m.IncrementCounter("hits_total", 1);

        var output = m.GeneratePrometheusOutput();

        Assert.Contains("# TYPE hits_total counter", output);
    }

    [Fact]
    public void IncrementCounter_with_tags_renders_label_set()
    {
        var m = new PrometheusExperimentMetrics();
        m.IncrementCounter("requests_total", 5,
            new KeyValuePair<string, object>("method", "GET"),
            new KeyValuePair<string, object>("status", "200"));

        var output = m.GeneratePrometheusOutput();

        Assert.Contains("method=\"GET\"", output);
        Assert.Contains("status=\"200\"", output);
    }

    [Fact]
    public void IncrementCounter_different_tag_sets_are_tracked_independently()
    {
        var m = new PrometheusExperimentMetrics();
        m.IncrementCounter("req", 2, new KeyValuePair<string, object>("env", "prod"));
        m.IncrementCounter("req", 3, new KeyValuePair<string, object>("env", "staging"));

        var output = m.GeneratePrometheusOutput();

        Assert.Contains("\"prod\"", output);
        Assert.Contains("\"staging\"", output);
    }

    // -----------------------------------------------------------------------
    // Gauge
    // -----------------------------------------------------------------------

    [Fact]
    public void SetGauge_output_contains_TYPE_gauge_declaration()
    {
        var m = new PrometheusExperimentMetrics();
        m.SetGauge("active_users", 42.0);

        var output = m.GeneratePrometheusOutput();

        Assert.Contains("# TYPE active_users gauge", output);
    }

    [Fact]
    public void SetGauge_last_value_wins()
    {
        var m = new PrometheusExperimentMetrics();
        m.SetGauge("queue_depth", 10.0);
        m.SetGauge("queue_depth", 99.0);

        var output = m.GeneratePrometheusOutput();

        Assert.Contains("99", output);
        Assert.DoesNotContain("10 ", output); // original value should be replaced
    }

    // -----------------------------------------------------------------------
    // Histogram
    // -----------------------------------------------------------------------

    [Fact]
    public void RecordHistogram_output_contains_TYPE_histogram_declaration()
    {
        var m = new PrometheusExperimentMetrics();
        m.RecordHistogram("latency_seconds", 0.5);

        var output = m.GeneratePrometheusOutput();

        Assert.Contains("# TYPE latency_seconds histogram", output);
    }

    [Fact]
    public void RecordHistogram_emits_sum_and_count_lines()
    {
        var m = new PrometheusExperimentMetrics();
        m.RecordHistogram("latency_seconds", 1.0);
        m.RecordHistogram("latency_seconds", 2.0);

        var output = m.GeneratePrometheusOutput();

        Assert.Contains("latency_seconds_sum", output);
        Assert.Contains("latency_seconds_count", output);
        Assert.Contains("2", output); // count = 2
    }

    [Fact]
    public void RecordHistogram_sum_accumulates_values()
    {
        var m = new PrometheusExperimentMetrics();
        m.RecordHistogram("resp_size", 100.0);
        m.RecordHistogram("resp_size", 200.0);

        var output = m.GeneratePrometheusOutput();

        Assert.Contains("300", output); // sum = 300
    }

    // -----------------------------------------------------------------------
    // Summary
    // -----------------------------------------------------------------------

    [Fact]
    public void RecordSummary_output_contains_TYPE_summary_declaration()
    {
        var m = new PrometheusExperimentMetrics();
        m.RecordSummary("processing_time", 0.25);

        var output = m.GeneratePrometheusOutput();

        Assert.Contains("# TYPE processing_time summary", output);
    }

    [Fact]
    public void RecordSummary_emits_sum_and_count_lines()
    {
        var m = new PrometheusExperimentMetrics();
        m.RecordSummary("processing_time", 1.0);
        m.RecordSummary("processing_time", 3.0);

        var output = m.GeneratePrometheusOutput();

        Assert.Contains("processing_time_sum", output);
        Assert.Contains("processing_time_count", output);
    }

    // -----------------------------------------------------------------------
    // Label escaping (Prometheus text format rules)
    // -----------------------------------------------------------------------

    [Fact]
    public void Tags_newline_in_value_is_escaped_to_backslash_n()
    {
        var m = new PrometheusExperimentMetrics();
        m.IncrementCounter("evt", 1, new KeyValuePair<string, object>("msg", "line1\nline2"));

        var output = m.GeneratePrometheusOutput();

        Assert.Contains("\\n", output);
        Assert.DoesNotContain("\n}", output); // raw newline inside label braces must not appear
    }

    [Fact]
    public void Tags_backslash_in_value_is_escaped()
    {
        var m = new PrometheusExperimentMetrics();
        m.IncrementCounter("evt", 1, new KeyValuePair<string, object>("path", @"C:\temp\file"));

        var output = m.GeneratePrometheusOutput();

        Assert.Contains("\\\\", output);
    }

    [Fact]
    public void Tags_double_quote_in_value_is_escaped()
    {
        var m = new PrometheusExperimentMetrics();
        m.IncrementCounter("evt", 1, new KeyValuePair<string, object>("q", "say \"hello\""));

        var output = m.GeneratePrometheusOutput();

        Assert.Contains("\\\"", output);
    }

    // -----------------------------------------------------------------------
    // Clear
    // -----------------------------------------------------------------------

    [Fact]
    public void Clear_removes_all_counters()
    {
        var m = new PrometheusExperimentMetrics();
        m.IncrementCounter("c", 5);
        m.Clear();

        var output = m.GeneratePrometheusOutput();

        Assert.DoesNotContain("c", output);
    }

    [Fact]
    public void Clear_removes_gauges_histograms_and_summaries()
    {
        var m = new PrometheusExperimentMetrics();
        m.SetGauge("g", 1.0);
        m.RecordHistogram("h", 1.0);
        m.RecordSummary("s", 1.0);
        m.Clear();

        var output = m.GeneratePrometheusOutput();

        Assert.Equal(string.Empty, output.Trim());
    }

    // -----------------------------------------------------------------------
    // Empty state
    // -----------------------------------------------------------------------

    [Fact]
    public void GeneratePrometheusOutput_with_no_metrics_returns_empty_or_whitespace()
    {
        var m = new PrometheusExperimentMetrics();
        var output = m.GeneratePrometheusOutput();
        Assert.True(string.IsNullOrWhiteSpace(output));
    }
}
