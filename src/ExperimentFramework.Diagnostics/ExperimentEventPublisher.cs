namespace ExperimentFramework.Diagnostics;

/// <summary>
/// Publishes experiment events to registered sinks.
/// </summary>
/// <remarks>
/// This class provides a bridge between the core framework and diagnostic event sinks.
/// It resolves sinks from DI and publishes events with minimal overhead.
/// </remarks>
public sealed class ExperimentEventPublisher
{
    private readonly IExperimentEventSink? _sink;

    /// <summary>
    /// Initializes a new instance of <see cref="ExperimentEventPublisher"/>.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving sinks.</param>
    public ExperimentEventPublisher(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));

        _sink = serviceProvider.GetExperimentEventSinks();
    }

    /// <summary>
    /// Publishes an event to all registered sinks.
    /// </summary>
    /// <param name="event">The event to publish.</param>
    public void Publish(in ExperimentEvent @event)
    {
        _sink?.OnEvent(@event);
    }

    /// <summary>
    /// Gets whether any sinks are registered.
    /// </summary>
    public bool HasSinks => _sink != null;

    /// <summary>
    /// Creates a TrialStarted event.
    /// </summary>
    public static ExperimentEvent CreateTrialStartedEvent(
        Type serviceType,
        string methodName,
        string trialKey,
        string? selectorName = null,
        IReadOnlyDictionary<string, object?>? context = null)
    {
        return new ExperimentEvent
        {
            Kind = ExperimentEventKind.TrialStarted,
            Timestamp = DateTimeOffset.UtcNow,
            ServiceType = serviceType,
            MethodName = methodName,
            TrialKey = trialKey,
            SelectorName = selectorName,
            Context = context
        };
    }

    /// <summary>
    /// Creates a TrialEnded event.
    /// </summary>
    public static ExperimentEvent CreateTrialEndedEvent(
        Type serviceType,
        string methodName,
        string trialKey,
        bool success,
        TimeSpan duration,
        string? selectorName = null,
        IReadOnlyDictionary<string, object?>? context = null)
    {
        return new ExperimentEvent
        {
            Kind = ExperimentEventKind.TrialEnded,
            Timestamp = DateTimeOffset.UtcNow,
            ServiceType = serviceType,
            MethodName = methodName,
            TrialKey = trialKey,
            Success = success,
            Duration = duration,
            SelectorName = selectorName,
            Context = context
        };
    }

    /// <summary>
    /// Creates a RouteSelected event.
    /// </summary>
    public static ExperimentEvent CreateRouteSelectedEvent(
        Type serviceType,
        string methodName,
        string trialKey,
        string? selectorName = null,
        IReadOnlyDictionary<string, object?>? context = null)
    {
        return new ExperimentEvent
        {
            Kind = ExperimentEventKind.RouteSelected,
            Timestamp = DateTimeOffset.UtcNow,
            ServiceType = serviceType,
            MethodName = methodName,
            TrialKey = trialKey,
            SelectorName = selectorName,
            Context = context
        };
    }

    /// <summary>
    /// Creates a FallbackOccurred event.
    /// </summary>
    public static ExperimentEvent CreateFallbackOccurredEvent(
        Type serviceType,
        string methodName,
        string trialKey,
        string fallbackKey,
        Exception exception,
        string? selectorName = null,
        IReadOnlyDictionary<string, object?>? context = null)
    {
        return new ExperimentEvent
        {
            Kind = ExperimentEventKind.FallbackOccurred,
            Timestamp = DateTimeOffset.UtcNow,
            ServiceType = serviceType,
            MethodName = methodName,
            TrialKey = trialKey,
            FallbackKey = fallbackKey,
            Exception = exception,
            SelectorName = selectorName,
            Context = context
        };
    }

    /// <summary>
    /// Creates an ExceptionThrown event.
    /// </summary>
    public static ExperimentEvent CreateExceptionThrownEvent(
        Type serviceType,
        string methodName,
        string trialKey,
        Exception exception,
        string? selectorName = null,
        IReadOnlyDictionary<string, object?>? context = null)
    {
        return new ExperimentEvent
        {
            Kind = ExperimentEventKind.ExceptionThrown,
            Timestamp = DateTimeOffset.UtcNow,
            ServiceType = serviceType,
            MethodName = methodName,
            TrialKey = trialKey,
            Exception = exception,
            SelectorName = selectorName,
            Context = context
        };
    }

    /// <summary>
    /// Creates a MethodInvoked event.
    /// </summary>
    public static ExperimentEvent CreateMethodInvokedEvent(
        Type serviceType,
        string methodName,
        string trialKey,
        string? selectorName = null,
        IReadOnlyDictionary<string, object?>? context = null)
    {
        return new ExperimentEvent
        {
            Kind = ExperimentEventKind.MethodInvoked,
            Timestamp = DateTimeOffset.UtcNow,
            ServiceType = serviceType,
            MethodName = methodName,
            TrialKey = trialKey,
            SelectorName = selectorName,
            Context = context
        };
    }

    /// <summary>
    /// Creates a MethodCompleted event.
    /// </summary>
    public static ExperimentEvent CreateMethodCompletedEvent(
        Type serviceType,
        string methodName,
        string trialKey,
        TimeSpan duration,
        bool success,
        string? selectorName = null,
        IReadOnlyDictionary<string, object?>? context = null)
    {
        return new ExperimentEvent
        {
            Kind = ExperimentEventKind.MethodCompleted,
            Timestamp = DateTimeOffset.UtcNow,
            ServiceType = serviceType,
            MethodName = methodName,
            TrialKey = trialKey,
            Duration = duration,
            Success = success,
            SelectorName = selectorName,
            Context = context
        };
    }
}
