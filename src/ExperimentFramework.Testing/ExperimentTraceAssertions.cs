namespace ExperimentFramework.Testing;

/// <summary>
/// Provides assertion helpers for experiment trace events.
/// Framework-agnostic and can be used with any testing library.
/// </summary>
public sealed class ExperimentTraceAssertions
{
    private readonly InMemoryExperimentEventSink _sink;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExperimentTraceAssertions"/> class.
    /// </summary>
    /// <param name="sink">The event sink to assert against.</param>
    public ExperimentTraceAssertions(InMemoryExperimentEventSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        _sink = sink;
    }

    /// <summary>
    /// Expects that a specific trial key was routed for the given service type.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <param name="expectedTrialKey">The expected trial key.</param>
    /// <returns>True if the assertion passes; otherwise, false.</returns>
    public bool ExpectRouted<TService>(string expectedTrialKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(expectedTrialKey);

        var serviceType = typeof(TService);
        var matchingEvents = _sink.Events
            .Where(e => e.ServiceType == serviceType && e.SelectedTrialKey == expectedTrialKey)
            .ToList();

        return matchingEvents.Count > 0;
    }

    /// <summary>
    /// Expects that a fallback occurred for the given service type.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <returns>True if the assertion passes; otherwise, false.</returns>
    public bool ExpectFallback<TService>()
    {
        var serviceType = typeof(TService);
        var fallbackEvents = _sink.Events
            .Where(e => e.ServiceType == serviceType && e.IsFallback)
            .ToList();

        return fallbackEvents.Count > 0;
    }

    /// <summary>
    /// Expects that a specific method was called on the given service type.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <param name="methodName">The expected method name.</param>
    /// <returns>True if the assertion passes; otherwise, false.</returns>
    public bool ExpectCall<TService>(string methodName)
    {
        ArgumentException.ThrowIfNullOrEmpty(methodName);

        var serviceType = typeof(TService);
        var matchingEvents = _sink.Events
            .Where(e => e.ServiceType == serviceType && e.MethodName == methodName)
            .ToList();

        return matchingEvents.Count > 0;
    }

    /// <summary>
    /// Expects that an exception occurred for the given service type.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <returns>True if the assertion passes; otherwise, false.</returns>
    public bool ExpectException<TService>()
    {
        var serviceType = typeof(TService);
        var exceptionEvents = _sink.Events
            .Where(e => e.ServiceType == serviceType && e.Exception != null)
            .ToList();

        return exceptionEvents.Count > 0;
    }

    /// <summary>
    /// Gets all events for a specific service type.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <returns>All events for the specified service type.</returns>
    public IReadOnlyList<ExperimentTraceEvent> GetEventsFor<TService>()
    {
        var serviceType = typeof(TService);
        return _sink.Events
            .Where(e => e.ServiceType == serviceType)
            .ToList();
    }

    /// <summary>
    /// Gets the first event for a specific service type.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <returns>The first event for the specified service type, or null if none exists.</returns>
    public ExperimentTraceEvent? GetFirstEventFor<TService>()
    {
        var serviceType = typeof(TService);
        return _sink.Events
            .FirstOrDefault(e => e.ServiceType == serviceType);
    }

    /// <summary>
    /// Clears all recorded events.
    /// </summary>
    public void Clear()
    {
        _sink.Clear();
    }
}
