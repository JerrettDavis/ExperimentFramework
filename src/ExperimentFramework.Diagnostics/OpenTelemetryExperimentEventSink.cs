using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ExperimentFramework.Diagnostics;

/// <summary>
/// Event sink that emits OpenTelemetry activities and metrics.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="ActivitySource"/> for distributed tracing and <see cref="Meter"/> for metrics.
/// No external OpenTelemetry package is required - the BCL types are sufficient.
/// </para>
/// <para>
/// Activity source name: <c>"ExperimentFramework.Diagnostics"</c><br/>
/// Meter name: <c>"ExperimentFramework.Diagnostics"</c>
/// </para>
/// <para>
/// To collect these activities and metrics, configure an OpenTelemetry SDK with appropriate listeners.
/// </para>
/// </remarks>
public sealed class OpenTelemetryExperimentEventSink : IExperimentEventSink
{
    private static readonly ActivitySource ActivitySource = new("ExperimentFramework.Diagnostics", "1.0.0");
    private static readonly Meter Meter = new("ExperimentFramework.Diagnostics", "1.0.0");

    // Counters
    private static readonly Counter<long> TrialStartedCounter = Meter.CreateCounter<long>(
        "experiment.trial.started",
        description: "Number of trial invocations started");

    private static readonly Counter<long> TrialEndedCounter = Meter.CreateCounter<long>(
        "experiment.trial.ended",
        description: "Number of trial invocations ended");

    private static readonly Counter<long> RouteSelectedCounter = Meter.CreateCounter<long>(
        "experiment.route.selected",
        description: "Number of routes selected");

    private static readonly Counter<long> FallbackCounter = Meter.CreateCounter<long>(
        "experiment.fallback.occurred",
        description: "Number of fallback occurrences");

    private static readonly Counter<long> ExceptionCounter = Meter.CreateCounter<long>(
        "experiment.exception.thrown",
        description: "Number of exceptions thrown");

    // Histograms
    private static readonly Histogram<double> TrialDurationHistogram = Meter.CreateHistogram<double>(
        "experiment.trial.duration",
        unit: "ms",
        description: "Duration of trial invocations");

    /// <inheritdoc/>
    public void OnEvent(in ExperimentEvent e)
    {
        // Emit metrics based on event kind
        EmitMetrics(e);

        // Optionally emit activities for key events
        // (In practice, you might only want activities for certain events to reduce overhead)
        if (ShouldEmitActivity(e.Kind))
        {
            EmitActivity(e);
        }
    }

    private static void EmitMetrics(in ExperimentEvent e)
    {
        var tags = new TagList
        {
            { "service.type", e.ServiceType.Name },
            { "method.name", e.MethodName },
            { "trial.key", e.TrialKey }
        };

        if (e.SelectorName != null)
            tags.Add("selector.name", e.SelectorName);

        switch (e.Kind)
        {
            case ExperimentEventKind.TrialStarted:
                TrialStartedCounter.Add(1, tags);
                break;

            case ExperimentEventKind.TrialEnded:
                tags.Add("success", e.Success?.ToString() ?? "unknown");
                TrialEndedCounter.Add(1, tags);
                if (e.Duration.HasValue)
                {
                    TrialDurationHistogram.Record(e.Duration.Value.TotalMilliseconds, tags);
                }
                break;

            case ExperimentEventKind.RouteSelected:
                RouteSelectedCounter.Add(1, tags);
                break;

            case ExperimentEventKind.FallbackOccurred:
                if (e.FallbackKey != null)
                    tags.Add("fallback.key", e.FallbackKey);
                FallbackCounter.Add(1, tags);
                break;

            case ExperimentEventKind.ExceptionThrown:
                if (e.Exception != null)
                    tags.Add("exception.type", e.Exception.GetType().Name);
                ExceptionCounter.Add(1, tags);
                break;

            case ExperimentEventKind.MethodInvoked:
            case ExperimentEventKind.MethodCompleted:
                // These are very high frequency - consider not emitting metrics for them
                // or emitting only under sampling conditions
                break;
        }
    }

    private static void EmitActivity(in ExperimentEvent e)
    {
        // Create a short-lived activity for this event
        var activityName = $"Experiment.{e.Kind}";
        using var activity = ActivitySource.StartActivity(activityName, ActivityKind.Internal);

        if (activity == null)
            return;

        activity.SetTag("event.kind", e.Kind.ToString());
        activity.SetTag("service.type", e.ServiceType.Name);
        activity.SetTag("method.name", e.MethodName);
        activity.SetTag("trial.key", e.TrialKey);
        activity.SetTag("event.timestamp", e.Timestamp.ToString("o"));

        if (e.SelectorName != null)
            activity.SetTag("selector.name", e.SelectorName);

        if (e.FallbackKey != null)
            activity.SetTag("fallback.key", e.FallbackKey);

        if (e.Duration.HasValue)
            activity.SetTag("duration.ms", e.Duration.Value.TotalMilliseconds);

        if (e.Success.HasValue)
            activity.SetTag("success", e.Success.Value);

        if (e.Exception != null)
        {
            activity.SetStatus(ActivityStatusCode.Error, e.Exception.Message);
            activity.SetTag("exception.type", e.Exception.GetType().FullName);
            activity.SetTag("exception.message", e.Exception.Message);
        }

        if (e.Context != null)
        {
            foreach (var kvp in e.Context)
            {
                activity.SetTag($"context.{kvp.Key}", kvp.Value?.ToString());
            }
        }
    }

    private static bool ShouldEmitActivity(ExperimentEventKind kind)
    {
        // Only emit activities for significant events to reduce overhead
        return kind switch
        {
            ExperimentEventKind.FallbackOccurred => true,
            ExperimentEventKind.ExceptionThrown => true,
            // Don't emit activities for high-frequency events
            ExperimentEventKind.TrialStarted => false,
            ExperimentEventKind.TrialEnded => false,
            ExperimentEventKind.RouteSelected => false,
            ExperimentEventKind.MethodInvoked => false,
            ExperimentEventKind.MethodCompleted => false,
            _ => false
        };
    }
}
