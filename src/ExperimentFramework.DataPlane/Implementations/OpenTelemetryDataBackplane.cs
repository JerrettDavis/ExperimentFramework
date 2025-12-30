using ExperimentFramework.DataPlane.Abstractions;
using ExperimentFramework.DataPlane.Abstractions.Events;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ExperimentFramework.DataPlane.Implementations;

/// <summary>
/// OpenTelemetry-compatible data backplane that emits events as Activities and Logs.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses the BCL's <see cref="ActivitySource"/> and <see cref="ILogger"/>
/// to emit data plane events as OpenTelemetry spans and logs.
/// No external OpenTelemetry package is required - the BCL types are sufficient.
/// </para>
/// <para>
/// To collect these activities and logs, configure an OpenTelemetry SDK with appropriate
/// exporters for your observability backend.
/// </para>
/// <para>
/// Activity source name: <c>"ExperimentFramework.DataPlane"</c>
/// </para>
/// </remarks>
public sealed class OpenTelemetryDataBackplane : IDataBackplane
{
    private static readonly ActivitySource ActivitySource = new("ExperimentFramework.DataPlane", "1.0.0");
    private readonly ILogger<OpenTelemetryDataBackplane> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenTelemetryDataBackplane"/> class.
    /// </summary>
    /// <param name="logger">The logger for emitting log-based events.</param>
    public OpenTelemetryDataBackplane(ILogger<OpenTelemetryDataBackplane> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ValueTask PublishAsync(DataPlaneEnvelope envelope, CancellationToken cancellationToken = default)
    {
        try
        {
            if (envelope == null)
            {
                _logger.LogWarning("Attempted to publish null envelope");
                return ValueTask.CompletedTask;
            }

            // Emit as Activity (Span) for distributed tracing
            EmitAsActivity(envelope);

            // Also emit as structured log for log-based backends
            EmitAsLog(envelope);

            return ValueTask.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventId} of type {EventType}",
                envelope?.EventId, envelope?.EventType);
            return ValueTask.CompletedTask;
        }
    }

    /// <inheritdoc />
    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        // Activities and logs are flushed by the OpenTelemetry SDK
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<BackplaneHealth> HealthAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(BackplaneHealth.Healthy(
            "OpenTelemetry backplane is operational"));
    }

    private void EmitAsActivity(DataPlaneEnvelope envelope)
    {
        var activityName = $"DataPlane.{envelope.EventType}";
        using var activity = ActivitySource.StartActivity(activityName, ActivityKind.Internal);

        if (activity == null)
            return;

        // Set common tags
        activity.SetTag("dataplane.event.id", envelope.EventId);
        activity.SetTag("dataplane.event.type", envelope.EventType.ToString());
        activity.SetTag("dataplane.schema.version", envelope.SchemaVersion);
        activity.SetTag("dataplane.timestamp", envelope.Timestamp.ToString("o"));

        if (envelope.CorrelationId != null)
            activity.SetTag("dataplane.correlation.id", envelope.CorrelationId);

        // Set event-specific tags based on payload type
        switch (envelope.Payload)
        {
            case ExposureEvent exposure:
                EmitExposureActivityTags(activity, exposure);
                break;
            case AssignmentEvent assignment:
                EmitAssignmentActivityTags(activity, assignment);
                break;
            case AnalysisSignalEvent signal:
                EmitAnalysisSignalActivityTags(activity, signal);
                break;
        }

        // Add metadata as tags
        if (envelope.Metadata != null)
        {
            foreach (var kvp in envelope.Metadata)
            {
                activity.SetTag($"dataplane.metadata.{kvp.Key}", kvp.Value?.ToString());
            }
        }
    }

    private void EmitExposureActivityTags(Activity activity, ExposureEvent exposure)
    {
        activity.SetTag("experiment.name", exposure.ExperimentName);
        activity.SetTag("experiment.variant", exposure.VariantKey);
        activity.SetTag("experiment.subject.id", exposure.SubjectId);
        activity.SetTag("experiment.subject.type", exposure.SubjectType);
        activity.SetTag("experiment.selection.reason", exposure.SelectionReason);
        activity.SetTag("experiment.assignment.policy", exposure.AssignmentPolicy.ToString());
        activity.SetTag("experiment.repeat.exposure", exposure.IsRepeatExposure);

        if (exposure.TenantId != null)
            activity.SetTag("experiment.tenant.id", exposure.TenantId);

        if (exposure.RulePath != null)
            activity.SetTag("experiment.rule.path", exposure.RulePath);
    }

    private void EmitAssignmentActivityTags(Activity activity, AssignmentEvent assignment)
    {
        activity.SetTag("experiment.name", assignment.ExperimentName);
        activity.SetTag("experiment.subject.id", assignment.SubjectId);
        activity.SetTag("experiment.variant.previous", assignment.PreviousVariantKey);
        activity.SetTag("experiment.variant.new", assignment.NewVariantKey);
        activity.SetTag("experiment.change.reason", assignment.ChangeReason);
        activity.SetTag("experiment.assignment.policy", assignment.AssignmentPolicy.ToString());
    }

    private void EmitAnalysisSignalActivityTags(Activity activity, AnalysisSignalEvent signal)
    {
        activity.SetTag("experiment.name", signal.ExperimentName);
        activity.SetTag("analysis.signal.type", signal.SignalType.ToString());
        activity.SetTag("analysis.signal.severity", signal.Severity.ToString());
        activity.SetTag("analysis.signal.message", signal.Message);

        if (signal.Data != null)
        {
            foreach (var kvp in signal.Data)
            {
                activity.SetTag($"analysis.data.{kvp.Key}", kvp.Value?.ToString());
            }
        }
    }

    private void EmitAsLog(DataPlaneEnvelope envelope)
    {
        var logLevel = envelope.EventType switch
        {
            DataPlaneEventType.Error => LogLevel.Error,
            DataPlaneEventType.AnalysisSignal => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(logLevel,
            "DataPlane Event: {EventType} | Experiment: {ExperimentName} | EventId: {EventId}",
            envelope.EventType,
            GetExperimentName(envelope.Payload),
            envelope.EventId);
    }

    private static string? GetExperimentName(object payload)
    {
        return payload switch
        {
            ExposureEvent exposure => exposure.ExperimentName,
            AssignmentEvent assignment => assignment.ExperimentName,
            AnalysisSignalEvent signal => signal.ExperimentName,
            _ => null
        };
    }
}
