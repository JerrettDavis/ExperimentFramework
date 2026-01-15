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
                // Note: Proxy mode information is not available in InvocationContext
                // This could be enhanced in a future version by extending the context
                ProxyMode = "Unknown",
                StartTime = startTime,
                EndTime = endTime,
                Duration = duration,
                // Note: Fallback detection would require additional context from the framework
                // This could be enhanced by adding fallback metadata to InvocationContext
                IsFallback = false,
                Exception = exception
            };

            _sink.RecordEvent(traceEvent);
        }
    }
}
