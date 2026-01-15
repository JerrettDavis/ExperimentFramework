using System.Diagnostics;

namespace ExperimentFramework.Diagnostics.Tests;

public class OpenTelemetryExperimentEventSinkTests
{
    [Fact]
    public void OnEvent_TrialStarted_EmitsMetric()
    {
        // Arrange
        var listener = CreateMeterListener();
        var sink = new OpenTelemetryExperimentEventSink();
        var evt = CreateEvent(ExperimentEventKind.TrialStarted);

        // Act
        sink.OnEvent(evt);

        // Assert
        Assert.Contains(listener.Measurements, m => m.InstrumentName == "experiment.trial.started");
    }

    [Fact]
    public void OnEvent_TrialEnded_EmitsMetricsWithDuration()
    {
        // Arrange
        var listener = CreateMeterListener();
        var sink = new OpenTelemetryExperimentEventSink();
        var evt = CreateEvent(ExperimentEventKind.TrialEnded, success: true, duration: TimeSpan.FromMilliseconds(100));

        // Act
        sink.OnEvent(evt);

        // Assert
        Assert.Contains(listener.Measurements, m => m.InstrumentName == "experiment.trial.ended");
        Assert.Contains(listener.Measurements, m => m.InstrumentName == "experiment.trial.duration");
    }

    [Fact]
    public void OnEvent_FallbackOccurred_EmitsMetricAndActivity()
    {
        // Arrange
        var meterListener = CreateMeterListener();
        var activityListener = CreateActivityListener();
        var sink = new OpenTelemetryExperimentEventSink();
        var evt = CreateEvent(ExperimentEventKind.FallbackOccurred, fallbackKey: "fallback-trial");

        // Act
        sink.OnEvent(evt);

        // Assert
        Assert.Contains(meterListener.Measurements, m => m.InstrumentName == "experiment.fallback.occurred");
        Assert.Contains(activityListener.Activities, a => a.DisplayName == "Experiment.FallbackOccurred");
    }

    [Fact]
    public void OnEvent_ExceptionThrown_EmitsMetricAndActivity()
    {
        // Arrange
        var meterListener = CreateMeterListener();
        var activityListener = CreateActivityListener();
        var sink = new OpenTelemetryExperimentEventSink();
        var exception = new InvalidOperationException("Test error");
        var evt = CreateEvent(ExperimentEventKind.ExceptionThrown, exception: exception);

        // Act
        sink.OnEvent(evt);

        // Assert
        Assert.Contains(meterListener.Measurements, m => m.InstrumentName == "experiment.exception.thrown");
        Assert.Contains(activityListener.Activities, a => a.DisplayName == "Experiment.ExceptionThrown");
        var activity = activityListener.Activities.First(a => a.DisplayName == "Experiment.ExceptionThrown");
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
    }

    [Fact]
    public void OnEvent_IncludesTagsInMetrics()
    {
        // Arrange
        var listener = CreateMeterListener();
        var sink = new OpenTelemetryExperimentEventSink();
        var evt = CreateEvent(
            ExperimentEventKind.RouteSelected,
            selectorName: "my-selector",
            trialKey: "trial-1");

        // Act
        sink.OnEvent(evt);

        // Assert
        var measurement = listener.Measurements.First(m => m.InstrumentName == "experiment.route.selected");
        Assert.NotNull(measurement.Tags);
        Assert.Contains(measurement.Tags, t => t.Key == "service.type");
        Assert.Contains(measurement.Tags, t => t.Key == "trial.key" && t.Value?.ToString() == "trial-1");
        Assert.Contains(measurement.Tags, t => t.Key == "selector.name" && t.Value?.ToString() == "my-selector");
    }

    [Fact]
    public void OnEvent_DoesNotEmitActivityForHighFrequencyEvents()
    {
        // Arrange
        var activityListener = CreateActivityListener();
        var sink = new OpenTelemetryExperimentEventSink();

        // Act
        sink.OnEvent(CreateEvent(ExperimentEventKind.TrialStarted));
        sink.OnEvent(CreateEvent(ExperimentEventKind.TrialEnded));
        sink.OnEvent(CreateEvent(ExperimentEventKind.RouteSelected));
        sink.OnEvent(CreateEvent(ExperimentEventKind.MethodInvoked));
        sink.OnEvent(CreateEvent(ExperimentEventKind.MethodCompleted));

        // Assert - only low frequency events should create activities
        Assert.Empty(activityListener.Activities);
    }

    private static ExperimentEvent CreateEvent(
        ExperimentEventKind kind,
        bool? success = null,
        TimeSpan? duration = null,
        string? fallbackKey = null,
        Exception? exception = null,
        string? selectorName = "test-selector",
        string trialKey = "test-trial")
    {
        return new ExperimentEvent
        {
            Kind = kind,
            Timestamp = DateTimeOffset.UtcNow,
            ServiceType = typeof(OpenTelemetryExperimentEventSinkTests),
            MethodName = "TestMethod",
            TrialKey = trialKey,
            Success = success,
            Duration = duration,
            FallbackKey = fallbackKey,
            Exception = exception,
            SelectorName = selectorName
        };
    }

    private static TestMeterListener CreateMeterListener()
    {
        var listener = new TestMeterListener();
        listener.Start();
        return listener;
    }

    private static TestActivityListener CreateActivityListener()
    {
        var listener = new TestActivityListener();
        ActivitySource.AddActivityListener(listener.GetListener());
        return listener;
    }

    private class TestMeterListener : IDisposable
    {
        private System.Diagnostics.Metrics.MeterListener? _listener;
        public List<Measurement> Measurements { get; } = new();

        public void Start()
        {
            _listener = new System.Diagnostics.Metrics.MeterListener();
            
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "ExperimentFramework.Diagnostics")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };

            _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
            {
                Measurements.Add(new Measurement
                {
                    InstrumentName = instrument.Name,
                    Value = measurement,
                    Tags = tags.ToArray()
                });
            });

            _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            {
                Measurements.Add(new Measurement
                {
                    InstrumentName = instrument.Name,
                    Value = measurement,
                    Tags = tags.ToArray()
                });
            });

            _listener.Start();
        }

        public void Dispose() => _listener?.Dispose();

        public class Measurement
        {
            public string InstrumentName { get; init; } = string.Empty;
            public object? Value { get; init; }
            public KeyValuePair<string, object?>[] Tags { get; init; } = Array.Empty<KeyValuePair<string, object?>>();
        }
    }

    private class TestActivityListener
    {
        private readonly ActivityListener _listener;
        public List<Activity> Activities { get; } = new();

        public TestActivityListener()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "ExperimentFramework.Diagnostics",
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = activity => Activities.Add(activity)
            };
        }

        public ActivityListener GetListener() => _listener;
    }
}
