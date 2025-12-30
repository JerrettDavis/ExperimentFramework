using ExperimentFramework.DataPlane.Abstractions;
using ExperimentFramework.DataPlane.Abstractions.Configuration;
using ExperimentFramework.DataPlane.Abstractions.Events;
using ExperimentFramework.Decorators;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExperimentFramework.DataPlane.Decorators;

/// <summary>
/// Decorator that emits exposure events to the data backplane.
/// </summary>
public sealed class ExposureLoggingDecorator : IExperimentDecorator
{
    private readonly IDataBackplane _backplane;
    private readonly IOptions<DataPlaneOptions> _options;
    private readonly ILogger<ExposureLoggingDecorator> _logger;
    private readonly ISubjectIdentityProvider? _identityProvider;
    private readonly Random _random = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ExposureLoggingDecorator"/> class.
    /// </summary>
    public ExposureLoggingDecorator(
        IDataBackplane backplane,
        IOptions<DataPlaneOptions> options,
        ILogger<ExposureLoggingDecorator> logger,
        ISubjectIdentityProvider? identityProvider = null)
    {
        _backplane = backplane ?? throw new ArgumentNullException(nameof(backplane));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _identityProvider = identityProvider;
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(InvocationContext ctx, Func<ValueTask<object?>> next)
    {
        var shouldLog = ShouldLogExposure();

        if (shouldLog)
        {
            await LogExposureAsync(ctx);
        }

        return await next();
    }

    private bool ShouldLogExposure()
    {
        var options = _options.Value;

        if (!options.EnableExposureEvents)
            return false;

        // Apply sampling
        if (options.SamplingRate < 1.0)
        {
            lock (_random)
            {
                return _random.NextDouble() <= options.SamplingRate;
            }
        }

        return true;
    }

    private async ValueTask LogExposureAsync(InvocationContext ctx)
    {
        try
        {
            var subjectId = TryGetSubjectId();
            if (subjectId == null)
            {
                _logger.LogDebug("No subject identity available for exposure logging");
                return;
            }

            var exposureEvent = new ExposureEvent
            {
                ExperimentName = ctx.ServiceType.Name,
                VariantKey = ctx.TrialKey,
                SubjectId = subjectId,
                SubjectType = _identityProvider?.SubjectType ?? "unknown",
                Timestamp = DateTimeOffset.UtcNow,
                SelectionReason = "trial-selection",
                AssignmentPolicy = AssignmentPolicy.BestEffort,
                IsRepeatExposure = false
            };

            var envelope = new DataPlaneEnvelope
            {
                EventId = Guid.NewGuid().ToString(),
                Timestamp = DateTimeOffset.UtcNow,
                EventType = DataPlaneEventType.Exposure,
                SchemaVersion = ExposureEvent.SchemaVersion,
                Payload = exposureEvent
            };

            await _backplane.PublishAsync(envelope);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log exposure event for {ServiceType}.{MethodName}",
                ctx.ServiceType.Name, ctx.MethodName);
        }
    }

    private string? TryGetSubjectId()
    {
        if (_identityProvider == null)
            return null;

        return _identityProvider.TryGetSubjectId(out var subjectId) ? subjectId : null;
    }
}
