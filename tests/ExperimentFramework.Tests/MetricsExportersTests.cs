using ExperimentFramework.Metrics.Exporters;

namespace ExperimentFramework.Tests;

/// <summary>
/// Tests for the Metrics.Exporters package including Prometheus and OpenTelemetry exporters.
/// </summary>
public sealed class MetricsExportersTests
{
    #region PrometheusExperimentMetrics Tests

    [Fact]
    public void PrometheusExperimentMetrics_IncrementCounter_tracks_values()
    {
        var metrics = new PrometheusExperimentMetrics();

        metrics.IncrementCounter("test_counter", 1);
        metrics.IncrementCounter("test_counter", 2);

        var output = metrics.GeneratePrometheusOutput();

        Assert.Contains("test_counter", output);
        Assert.Contains("3", output); // 1 + 2
    }

    [Fact]
    public void PrometheusExperimentMetrics_IncrementCounter_with_tags()
    {
        var metrics = new PrometheusExperimentMetrics();
        var tags = new[] { new KeyValuePair<string, object>("service", "test") };

        metrics.IncrementCounter("test_counter", 1, tags);

        var output = metrics.GeneratePrometheusOutput();

        Assert.Contains("test_counter", output);
        Assert.Contains("service=\"test\"", output);
    }

    [Fact]
    public void PrometheusExperimentMetrics_RecordHistogram_tracks_values()
    {
        var metrics = new PrometheusExperimentMetrics();

        metrics.RecordHistogram("test_histogram", 1.5);
        metrics.RecordHistogram("test_histogram", 2.5);

        var output = metrics.GeneratePrometheusOutput();

        Assert.Contains("test_histogram", output);
        Assert.Contains("histogram", output);
        Assert.Contains("_sum", output);
        Assert.Contains("_count", output);
    }

    [Fact]
    public void PrometheusExperimentMetrics_SetGauge_tracks_values()
    {
        var metrics = new PrometheusExperimentMetrics();

        metrics.SetGauge("test_gauge", 5.0);
        metrics.SetGauge("test_gauge", 10.0);

        var output = metrics.GeneratePrometheusOutput();

        Assert.Contains("test_gauge", output);
        Assert.Contains("gauge", output);
        Assert.Contains("10", output); // Last value set
    }

    [Fact]
    public void PrometheusExperimentMetrics_RecordSummary_tracks_values()
    {
        var metrics = new PrometheusExperimentMetrics();

        metrics.RecordSummary("test_summary", 1.0);
        metrics.RecordSummary("test_summary", 2.0);

        var output = metrics.GeneratePrometheusOutput();

        Assert.Contains("test_summary", output);
        Assert.Contains("summary", output);
    }

    [Fact]
    public void PrometheusExperimentMetrics_Clear_removes_all_metrics()
    {
        var metrics = new PrometheusExperimentMetrics();

        metrics.IncrementCounter("test_counter", 1);
        metrics.Clear();

        var output = metrics.GeneratePrometheusOutput();

        Assert.DoesNotContain("test_counter", output);
    }

    [Fact]
    public void PrometheusExperimentMetrics_escapes_special_characters()
    {
        var metrics = new PrometheusExperimentMetrics();
        var tags = new[] { new KeyValuePair<string, object>("message", "hello\nworld") };

        metrics.IncrementCounter("test_counter", 1, tags);

        var output = metrics.GeneratePrometheusOutput();

        Assert.Contains("\\n", output); // Newline escaped
    }

    #endregion

    #region OpenTelemetryExperimentMetrics Tests

    [Fact]
    public void OpenTelemetryExperimentMetrics_can_be_created()
    {
        var metrics = new OpenTelemetryExperimentMetrics();

        Assert.NotNull(metrics);

        metrics.Dispose();
    }

    [Fact]
    public void OpenTelemetryExperimentMetrics_with_custom_meter_name()
    {
        var metrics = new OpenTelemetryExperimentMetrics("CustomMeter", "2.0.0");

        Assert.NotNull(metrics);

        metrics.Dispose();
    }

    [Fact]
    public void OpenTelemetryExperimentMetrics_IncrementCounter_does_not_throw()
    {
        var metrics = new OpenTelemetryExperimentMetrics();

        // Should not throw
        metrics.IncrementCounter("test_counter", 1);
        metrics.IncrementCounter("test_counter", 5, new KeyValuePair<string, object>("tag", "value"));

        metrics.Dispose();
    }

    [Fact]
    public void OpenTelemetryExperimentMetrics_RecordHistogram_does_not_throw()
    {
        var metrics = new OpenTelemetryExperimentMetrics();

        // Should not throw
        metrics.RecordHistogram("test_histogram", 1.5);

        metrics.Dispose();
    }

    [Fact]
    public void OpenTelemetryExperimentMetrics_SetGauge_does_not_throw()
    {
        var metrics = new OpenTelemetryExperimentMetrics();

        // Should not throw (records to histogram as fallback)
        metrics.SetGauge("test_gauge", 5.0);

        metrics.Dispose();
    }

    [Fact]
    public void OpenTelemetryExperimentMetrics_RecordSummary_does_not_throw()
    {
        var metrics = new OpenTelemetryExperimentMetrics();

        // Should not throw (records to histogram)
        metrics.RecordSummary("test_summary", 1.0);

        metrics.Dispose();
    }

    #endregion
}
