using ExperimentFramework.Audit;
using Microsoft.Extensions.Logging;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Audit;

[Feature("LoggingAuditSink writes audit events to the logging infrastructure")]
public sealed class LoggingAuditSinkTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Records audit event to logger")]
    [Fact]
    public async Task Records_event_to_logger()
    {
        var logger = new TestLogger<LoggingAuditSink>();
        var sink = new LoggingAuditSink(logger);

        var auditEvent = new AuditEvent
        {
            EventId = "test-001",
            Timestamp = DateTimeOffset.UtcNow,
            EventType = AuditEventType.VariantSelected,
            ExperimentName = "test-experiment",
            ServiceType = "ITestService",
            SelectedTrialKey = "variant-b",
            Actor = "test-user"
        };
        await sink.RecordAsync(auditEvent);

        Assert.Single(logger.LogEntries);
        Assert.Equal(LogLevel.Information, logger.LogEntries[0].LogLevel);
        Assert.Contains("VariantSelected", logger.LogEntries[0].Message);
        Assert.Contains("test-experiment", logger.LogEntries[0].Message);
    }

    [Scenario("Uses configured log level")]
    [Fact]
    public async Task Uses_configured_log_level()
    {
        var logger = new TestLogger<LoggingAuditSink>();
        var sink = new LoggingAuditSink(logger, LogLevel.Warning);

        var auditEvent = new AuditEvent
        {
            EventId = "test-002",
            Timestamp = DateTimeOffset.UtcNow,
            EventType = AuditEventType.Error
        };
        await sink.RecordAsync(auditEvent);

        Assert.Equal(LogLevel.Warning, logger.LogEntries[0].LogLevel);
    }

    [Scenario("Handles null optional properties")]
    [Fact]
    public async Task Handles_null_properties()
    {
        var logger = new TestLogger<LoggingAuditSink>();
        var sink = new LoggingAuditSink(logger);

        var auditEvent = new AuditEvent
        {
            EventId = "test-003",
            Timestamp = DateTimeOffset.UtcNow,
            EventType = AuditEventType.ExperimentCreated
        };
        await sink.RecordAsync(auditEvent);

        Assert.Single(logger.LogEntries);
        Assert.True(logger.LogEntries[0].Message.Contains("(none)") ||
                    logger.LogEntries[0].Message.Contains("(system)"));
    }

    [Scenario("Serializes details as JSON")]
    [Fact]
    public async Task Serializes_details_as_json()
    {
        var logger = new TestLogger<LoggingAuditSink>();
        var sink = new LoggingAuditSink(logger);

        var auditEvent = new AuditEvent
        {
            EventId = "test-004",
            Timestamp = DateTimeOffset.UtcNow,
            EventType = AuditEventType.ExperimentModified,
            Details = new Dictionary<string, object>
            {
                ["change"] = "rollout",
                ["oldValue"] = 10,
                ["newValue"] = 50
            }
        };
        await sink.RecordAsync(auditEvent);

        Assert.Contains("change", logger.LogEntries[0].Message);
        Assert.Contains("rollout", logger.LogEntries[0].Message);
    }

    [Scenario("RecordAsync completes synchronously")]
    [Fact]
    public Task RecordAsync_completes_synchronously()
        => Given("a logging audit sink", () =>
            {
                var logger = new TestLogger<LoggingAuditSink>();
                return new LoggingAuditSink(logger);
            })
            .When("recording an event", sink =>
            {
                var auditEvent = new AuditEvent
                {
                    EventId = "test-005",
                    Timestamp = DateTimeOffset.UtcNow,
                    EventType = AuditEventType.FallbackTriggered
                };
                var task = sink.RecordAsync(auditEvent);
                return task.IsCompleted;
            })
            .Then("task is already completed", isCompleted => isCompleted)
            .AssertPassed();

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> LogEntries { get; } = [];

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            LogEntries.Add(new LogEntry
            {
                LogLevel = logLevel,
                Message = formatter(state, exception)
            });
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }

    private sealed class LogEntry
    {
        public LogLevel LogLevel { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}
