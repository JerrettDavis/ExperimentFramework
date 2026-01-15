using ExperimentFramework.Decorators;

namespace ExperimentFramework.Testing;

/// <summary>
/// Decorator that captures experiment execution traces to an in-memory sink.
/// </summary>
internal sealed class TraceCapturingDecorator : IExperimentDecorator
{
    private readonly InMemoryExperimentEventSink _sink;

    public TraceCapturingDecorator(InMemoryExperimentEventSink sink)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    }

    public async ValueTask<object?> InvokeAsync(InvocationContext context, Func<ValueTask<object?>> next)
    {
        var startTime = DateTimeOffset.UtcNow;
        Exception? exception = null;
        object? result = null;

        try
        {
            result = await next();
            return result;
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            var endTime = DateTimeOffset.UtcNow;
            var duration = endTime - startTime;

            var traceEvent = new ExperimentTraceEvent
            {
                ServiceType = context.ServiceType,
                MethodName = context.MethodName,
                SelectedTrialKey = context.TrialKey,
                ProxyMode = "Test", // Could be enhanced to detect actual proxy mode
                StartTime = startTime,
                EndTime = endTime,
                Duration = duration,
                IsFallback = false, // Could be enhanced to detect fallback
                Exception = exception
            };

            _sink.RecordEvent(traceEvent);
        }
    }
}
