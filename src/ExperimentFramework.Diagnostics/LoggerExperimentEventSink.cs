using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Diagnostics;

/// <summary>
/// Event sink that writes events to an ILogger with structured logging.
/// </summary>
/// <remarks>
/// Uses event IDs for filtering and structured log properties for analysis.
/// Thread-safe through ILogger's thread-safety guarantees.
/// </remarks>
public sealed class LoggerExperimentEventSink : IExperimentEventSink
{
    private readonly ILogger _logger;

    // Event IDs for different event kinds
    private static readonly EventId TrialStartedEventId = new(1001, "TrialStarted");
    private static readonly EventId TrialEndedEventId = new(1002, "TrialEnded");
    private static readonly EventId RouteSelectedEventId = new(1003, "RouteSelected");
    private static readonly EventId FallbackOccurredEventId = new(1004, "FallbackOccurred");
    private static readonly EventId ExceptionThrownEventId = new(1005, "ExceptionThrown");
    private static readonly EventId MethodInvokedEventId = new(1006, "MethodInvoked");
    private static readonly EventId MethodCompletedEventId = new(1007, "MethodCompleted");

    /// <summary>
    /// Initializes a new instance of <see cref="LoggerExperimentEventSink"/>.
    /// </summary>
    /// <param name="logger">The logger to write events to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public LoggerExperimentEventSink(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public void OnEvent(in ExperimentEvent e)
    {
        var (eventId, logLevel, message) = GetEventMetadata(e);

        if (!_logger.IsEnabled(logLevel))
            return;

        // Build structured log state
        var state = new Dictionary<string, object?>
        {
            ["EventKind"] = e.Kind.ToString(),
            ["ServiceType"] = e.ServiceType.Name,
            ["MethodName"] = e.MethodName,
            ["TrialKey"] = e.TrialKey,
            ["Timestamp"] = e.Timestamp
        };

        if (e.SelectorName != null)
            state["SelectorName"] = e.SelectorName;

        if (e.FallbackKey != null)
            state["FallbackKey"] = e.FallbackKey;

        if (e.Duration.HasValue)
            state["DurationMs"] = e.Duration.Value.TotalMilliseconds;

        if (e.Success.HasValue)
            state["Success"] = e.Success.Value;

        if (e.Context != null)
        {
            foreach (var kvp in e.Context)
            {
                state[$"Context.{kvp.Key}"] = kvp.Value;
            }
        }

        // Log with exception if present
        if (e.Exception != null)
        {
            _logger.Log(logLevel, eventId, state, e.Exception, (s, ex) => message);
        }
        else
        {
            _logger.Log(logLevel, eventId, state, null, (s, ex) => message);
        }
    }

    private static (EventId EventId, LogLevel LogLevel, string Message) GetEventMetadata(in ExperimentEvent e)
    {
        return e.Kind switch
        {
            ExperimentEventKind.TrialStarted => (
                TrialStartedEventId,
                LogLevel.Debug,
                $"Trial started: {e.ServiceType.Name}.{e.MethodName} -> {e.TrialKey}"
            ),
            ExperimentEventKind.TrialEnded => (
                TrialEndedEventId,
                e.Success == true ? LogLevel.Information : LogLevel.Warning,
                $"Trial ended: {e.ServiceType.Name}.{e.MethodName} -> {e.TrialKey} ({(e.Success == true ? "success" : "failure")}, {e.Duration?.TotalMilliseconds:F2}ms)"
            ),
            ExperimentEventKind.RouteSelected => (
                RouteSelectedEventId,
                LogLevel.Debug,
                $"Route selected: {e.ServiceType.Name}.{e.MethodName} -> {e.TrialKey} (selector: {e.SelectorName})"
            ),
            ExperimentEventKind.FallbackOccurred => (
                FallbackOccurredEventId,
                LogLevel.Warning,
                $"Fallback occurred: {e.ServiceType.Name}.{e.MethodName} from {e.TrialKey} to {e.FallbackKey}"
            ),
            ExperimentEventKind.ExceptionThrown => (
                ExceptionThrownEventId,
                LogLevel.Error,
                $"Exception thrown: {e.ServiceType.Name}.{e.MethodName} in {e.TrialKey}: {e.Exception?.Message}"
            ),
            ExperimentEventKind.MethodInvoked => (
                MethodInvokedEventId,
                LogLevel.Trace,
                $"Method invoked: {e.ServiceType.Name}.{e.MethodName} -> {e.TrialKey}"
            ),
            ExperimentEventKind.MethodCompleted => (
                MethodCompletedEventId,
                LogLevel.Trace,
                $"Method completed: {e.ServiceType.Name}.{e.MethodName} -> {e.TrialKey} ({e.Duration?.TotalMilliseconds:F2}ms)"
            ),
            _ => (
                new EventId(1000, "UnknownEvent"),
                LogLevel.Information,
                $"Unknown event: {e.Kind}"
            )
        };
    }
}
