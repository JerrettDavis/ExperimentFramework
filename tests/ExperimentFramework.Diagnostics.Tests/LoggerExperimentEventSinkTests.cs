using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExperimentFramework.Diagnostics.Tests;

public class LoggerExperimentEventSinkTests
{
    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LoggerExperimentEventSink(null!));
    }

    [Fact]
    public void OnEvent_TrialStarted_LogsDebugMessage()
    {
        // Arrange
        var logger = new TestLogger();
        var sink = new LoggerExperimentEventSink(logger);
        var evt = CreateEvent(ExperimentEventKind.TrialStarted);

        // Act
        sink.OnEvent(evt);

        // Assert
        Assert.Single(logger.Logs);
        var log = logger.Logs[0];
        Assert.Equal(LogLevel.Debug, log.LogLevel);
        Assert.Equal(1001, log.EventId.Id);
        Assert.Contains("Trial started", log.Message);
    }

    [Fact]
    public void OnEvent_TrialEndedSuccess_LogsInformation()
    {
        // Arrange
        var logger = new TestLogger();
        var sink = new LoggerExperimentEventSink(logger);
        var evt = CreateEvent(ExperimentEventKind.TrialEnded, success: true, duration: TimeSpan.FromMilliseconds(150));

        // Act
        sink.OnEvent(evt);

        // Assert
        Assert.Single(logger.Logs);
        var log = logger.Logs[0];
        Assert.Equal(LogLevel.Information, log.LogLevel);
        Assert.Equal(1002, log.EventId.Id);
        Assert.Contains("Trial ended", log.Message);
        Assert.Contains("success", log.Message);
    }

    [Fact]
    public void OnEvent_TrialEndedFailure_LogsWarning()
    {
        // Arrange
        var logger = new TestLogger();
        var sink = new LoggerExperimentEventSink(logger);
        var evt = CreateEvent(ExperimentEventKind.TrialEnded, success: false, duration: TimeSpan.FromMilliseconds(50));

        // Act
        sink.OnEvent(evt);

        // Assert
        Assert.Single(logger.Logs);
        var log = logger.Logs[0];
        Assert.Equal(LogLevel.Warning, log.LogLevel);
        Assert.Contains("failure", log.Message);
    }

    [Fact]
    public void OnEvent_FallbackOccurred_LogsWarning()
    {
        // Arrange
        var logger = new TestLogger();
        var sink = new LoggerExperimentEventSink(logger);
        var evt = CreateEvent(ExperimentEventKind.FallbackOccurred, fallbackKey: "fallback-trial");

        // Act
        sink.OnEvent(evt);

        // Assert
        Assert.Single(logger.Logs);
        var log = logger.Logs[0];
        Assert.Equal(LogLevel.Warning, log.LogLevel);
        Assert.Equal(1004, log.EventId.Id);
        Assert.Contains("Fallback occurred", log.Message);
    }

    [Fact]
    public void OnEvent_ExceptionThrown_LogsError()
    {
        // Arrange
        var logger = new TestLogger();
        var sink = new LoggerExperimentEventSink(logger);
        var exception = new InvalidOperationException("Test error");
        var evt = CreateEvent(ExperimentEventKind.ExceptionThrown, exception: exception);

        // Act
        sink.OnEvent(evt);

        // Assert
        Assert.Single(logger.Logs);
        var log = logger.Logs[0];
        Assert.Equal(LogLevel.Error, log.LogLevel);
        Assert.Equal(1005, log.EventId.Id);
        Assert.Contains("Exception thrown", log.Message);
        Assert.Same(exception, log.Exception);
    }

    [Fact]
    public void OnEvent_IncludesStructuredData()
    {
        // Arrange
        var logger = new TestLogger();
        var sink = new LoggerExperimentEventSink(logger);
        var evt = CreateEvent(
            ExperimentEventKind.TrialStarted,
            selectorName: "my-selector",
            context: new Dictionary<string, object?> { ["customKey"] = "customValue" });

        // Act
        sink.OnEvent(evt);

        // Assert
        Assert.Single(logger.Logs);
        var log = logger.Logs[0];
        Assert.NotNull(log.State);
        Assert.IsType<Dictionary<string, object?>>(log.State);
        var state = Assert.IsType<Dictionary<string, object?>>(log.State);
        Assert.Equal("TrialStarted", state["EventKind"]);
        Assert.Equal("my-selector", state["SelectorName"]);
        Assert.Equal("customValue", state["Context.customKey"]);
    }

    [Fact]
    public void OnEvent_SkipsLoggingWhenLevelNotEnabled()
    {
        // Arrange
        var logger = new TestLogger(enabledLevel: LogLevel.Information);
        var sink = new LoggerExperimentEventSink(logger);
        var evt = CreateEvent(ExperimentEventKind.TrialStarted); // Debug level

        // Act
        sink.OnEvent(evt);

        // Assert
        Assert.Empty(logger.Logs);
    }

    private static ExperimentEvent CreateEvent(
        ExperimentEventKind kind,
        bool? success = null,
        TimeSpan? duration = null,
        string? fallbackKey = null,
        Exception? exception = null,
        string? selectorName = null,
        Dictionary<string, object?>? context = null)
    {
        return new ExperimentEvent
        {
            Kind = kind,
            Timestamp = DateTimeOffset.UtcNow,
            ServiceType = typeof(LoggerExperimentEventSinkTests),
            MethodName = "TestMethod",
            TrialKey = "test-trial",
            Success = success,
            Duration = duration,
            FallbackKey = fallbackKey,
            Exception = exception,
            SelectorName = selectorName,
            Context = context
        };
    }

    private class TestLogger : ILogger
    {
        private readonly LogLevel _enabledLevel;
        public List<LogEntry> Logs { get; } = new();

        public TestLogger(LogLevel enabledLevel = LogLevel.Trace)
        {
            _enabledLevel = enabledLevel;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _enabledLevel;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            Logs.Add(new LogEntry
            {
                LogLevel = logLevel,
                EventId = eventId,
                State = state,
                Exception = exception,
                Message = formatter(state, exception)
            });
        }

        public class LogEntry
        {
            public LogLevel LogLevel { get; init; }
            public EventId EventId { get; init; }
            public object? State { get; init; }
            public Exception? Exception { get; init; }
            public string Message { get; init; } = string.Empty;
        }
    }
}
