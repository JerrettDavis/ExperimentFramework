using System.Diagnostics;

namespace ExperimentFramework.Telemetry;

/// <summary>
/// OpenTelemetry-based telemetry implementation using <see cref="System.Diagnostics.Activity"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses the BCL's <see cref="ActivitySource"/> to emit distributed tracing spans.
/// No external OpenTelemetry package is required - the BCL types are sufficient.
/// </para>
/// <para>
/// To collect these activities, configure an OpenTelemetry SDK with an ActivityListener or use
/// OpenTelemetry .NET's automatic instrumentation.
/// </para>
/// <para>
/// Activity source name: <c>"ExperimentFramework"</c>
/// </para>
/// </remarks>
public sealed class OpenTelemetryExperimentTelemetry : IExperimentTelemetry
{
    private static readonly ActivitySource ActivitySource = new("ExperimentFramework", "1.0.0");

    /// <inheritdoc/>
    public IExperimentTelemetryScope StartInvocation(
        Type serviceType,
        string methodName,
        string selectorName,
        string trialKey,
        IReadOnlyList<string> candidateKeys)
    {
        var activity = ActivitySource.StartActivity($"Experiment {serviceType.Name}.{methodName}");

        if (activity is not null)
        {
            activity.SetTag("experiment.service", serviceType.Name);
            activity.SetTag("experiment.method", methodName);
            activity.SetTag("experiment.selector", selectorName);
            activity.SetTag("experiment.trial.selected", trialKey);
            activity.SetTag("experiment.trial.candidates", string.Join(",", candidateKeys));
        }

        return new OpenTelemetryScope(activity);
    }

    /// <summary>
    /// OpenTelemetry telemetry scope backed by an <see cref="Activity"/>.
    /// </summary>
    private sealed class OpenTelemetryScope(Activity? activity) : IExperimentTelemetryScope
    {
        public void RecordSuccess()
        {
            activity?.SetTag("experiment.outcome", "success");
        }

        public void RecordFailure(Exception exception)
        {
            activity?.SetTag("experiment.outcome", "failure");
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        }

        public void RecordFallback(string fallbackKey)
        {
            activity?.SetTag("experiment.fallback", fallbackKey);
        }

        public void RecordVariant(string variantName, string variantSource)
        {
            activity?.SetTag("experiment.variant", variantName);
            activity?.SetTag("experiment.variant.source", variantSource);
        }

        public void Dispose() => activity?.Dispose();
    }
}
