using ExperimentFramework.Metrics.Exporters;

namespace ExperimentFramework.Metrics.Exporters.Tests;

/// <summary>
/// Unit tests for <see cref="OpenTelemetryExperimentMetrics"/>.
/// </summary>
public sealed class OpenTelemetryExperimentMetricsTests
{
    // -----------------------------------------------------------------------
    // Construction
    // -----------------------------------------------------------------------

    [Fact]
    public void Ctor_default_parameters_creates_instance()
    {
        var m = new OpenTelemetryExperimentMetrics();
        Assert.NotNull(m);
        m.Dispose();
    }

    [Fact]
    public void Ctor_custom_meter_name_creates_instance()
    {
        var m = new OpenTelemetryExperimentMetrics("MyApp.Experiments", "2.0.0");
        Assert.NotNull(m);
        m.Dispose();
    }

    [Fact]
    public void Ctor_null_version_does_not_throw()
    {
        var m = new OpenTelemetryExperimentMetrics("TestMeter", null);
        Assert.NotNull(m);
        m.Dispose();
    }

    // -----------------------------------------------------------------------
    // IncrementCounter — smoke tests (OTel has no synchronous read-back API)
    // -----------------------------------------------------------------------

    [Fact]
    public void IncrementCounter_does_not_throw_with_default_value()
    {
        var m = new OpenTelemetryExperimentMetrics();
        m.IncrementCounter("experiment_hits");
        m.Dispose();
    }

    [Fact]
    public void IncrementCounter_does_not_throw_with_explicit_value()
    {
        var m = new OpenTelemetryExperimentMetrics();
        m.IncrementCounter("experiment_hits", 5);
        m.Dispose();
    }

    [Fact]
    public void IncrementCounter_does_not_throw_with_tags()
    {
        var m = new OpenTelemetryExperimentMetrics();
        m.IncrementCounter("experiment_hits", 1,
            new KeyValuePair<string, object>("experiment", "checkout-flow"),
            new KeyValuePair<string, object>("variant", "control"));
        m.Dispose();
    }

    // -----------------------------------------------------------------------
    // RecordHistogram
    // -----------------------------------------------------------------------

    [Fact]
    public void RecordHistogram_does_not_throw()
    {
        var m = new OpenTelemetryExperimentMetrics();
        m.RecordHistogram("response_time_ms", 42.5);
        m.Dispose();
    }

    [Fact]
    public void RecordHistogram_does_not_throw_with_tags()
    {
        var m = new OpenTelemetryExperimentMetrics();
        m.RecordHistogram("response_time_ms", 99.9,
            new KeyValuePair<string, object>("service", "api"));
        m.Dispose();
    }

    // -----------------------------------------------------------------------
    // SetGauge (falls back to histogram internally)
    // -----------------------------------------------------------------------

    [Fact]
    public void SetGauge_does_not_throw()
    {
        var m = new OpenTelemetryExperimentMetrics();
        m.SetGauge("active_connections", 17.0);
        m.Dispose();
    }

    // -----------------------------------------------------------------------
    // RecordSummary (falls back to histogram internally)
    // -----------------------------------------------------------------------

    [Fact]
    public void RecordSummary_does_not_throw()
    {
        var m = new OpenTelemetryExperimentMetrics();
        m.RecordSummary("payload_bytes", 1024.0);
        m.Dispose();
    }

    // -----------------------------------------------------------------------
    // Dispose
    // -----------------------------------------------------------------------

    [Fact]
    public void Dispose_can_be_called_multiple_times_without_throwing()
    {
        var m = new OpenTelemetryExperimentMetrics();
        m.Dispose();
        // Second dispose must not throw (Meter.Dispose is documented idempotent)
        m.Dispose();
    }

    [Fact]
    public void Multiple_instances_with_different_meter_names_coexist()
    {
        var m1 = new OpenTelemetryExperimentMetrics("MeterA", "1.0");
        var m2 = new OpenTelemetryExperimentMetrics("MeterB", "1.0");

        // Should not interfere with each other
        m1.IncrementCounter("counter", 1);
        m2.IncrementCounter("counter", 1);

        m1.Dispose();
        m2.Dispose();
    }
}
