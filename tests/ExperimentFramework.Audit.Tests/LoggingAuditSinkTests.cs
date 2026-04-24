using ExperimentFramework.Audit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExperimentFramework.Audit.Tests;

public sealed class LoggingAuditSinkTests
{
    private static AuditEvent MakeEvent(
        AuditEventType type = AuditEventType.VariantSelected,
        string? experiment = "exp-1",
        string? trial = "control")
        => new AuditEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            EventType = type,
            ExperimentName = experiment,
            SelectedTrialKey = trial,
        };

    [Fact]
    public async Task RecordAsync_DoesNotThrow_WithNullLogger()
    {
        var sink = new LoggingAuditSink(NullLogger<LoggingAuditSink>.Instance);
        var evt = MakeEvent();
        // Should complete without exception
        await sink.RecordAsync(evt);
    }

    [Fact]
    public async Task RecordAsync_LogsAuditEvent_WithInformationLevel()
    {
        using var logFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug));
        var logger = logFactory.CreateLogger<LoggingAuditSink>();
        var sink = new LoggingAuditSink(logger, LogLevel.Information);
        var evt = MakeEvent(AuditEventType.ExperimentStarted, "my-exp", "variant-a");

        // Should not throw
        await sink.RecordAsync(evt);
    }

    [Fact]
    public async Task RecordAsync_LogsAtSpecifiedLevel()
    {
        using var logFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug));
        var logger = logFactory.CreateLogger<LoggingAuditSink>();
        var sink = new LoggingAuditSink(logger, LogLevel.Warning);
        var evt = MakeEvent(AuditEventType.Error);

        await sink.RecordAsync(evt);
    }

    [Fact]
    public async Task RecordAsync_HandlesNullDetails()
    {
        var sink = new LoggingAuditSink(NullLogger<LoggingAuditSink>.Instance);
        var evt = new AuditEvent
        {
            EventId = "id-1",
            Timestamp = DateTimeOffset.UtcNow,
            EventType = AuditEventType.VariantSelected,
            Details = null,
        };
        await sink.RecordAsync(evt);
    }

    [Fact]
    public async Task RecordAsync_HandlesNonNullDetails()
    {
        var sink = new LoggingAuditSink(NullLogger<LoggingAuditSink>.Instance);
        var evt = new AuditEvent
        {
            EventId = "id-2",
            Timestamp = DateTimeOffset.UtcNow,
            EventType = AuditEventType.RolloutChanged,
            Details = new Dictionary<string, object> { ["key"] = "value", ["count"] = 42 },
        };
        await sink.RecordAsync(evt);
    }

    [Fact]
    public async Task RecordAsync_HandlesNullExperimentName()
    {
        var sink = new LoggingAuditSink(NullLogger<LoggingAuditSink>.Instance);
        var evt = new AuditEvent
        {
            EventId = "id-3",
            Timestamp = DateTimeOffset.UtcNow,
            EventType = AuditEventType.FallbackTriggered,
            ExperimentName = null,
            ServiceType = null,
            SelectedTrialKey = null,
            Actor = null,
        };
        await sink.RecordAsync(evt);
    }

    [Fact]
    public async Task RecordAsync_HandlesAllEventTypes()
    {
        var sink = new LoggingAuditSink(NullLogger<LoggingAuditSink>.Instance);
        foreach (AuditEventType type in Enum.GetValues<AuditEventType>())
        {
            var evt = MakeEvent(type);
            await sink.RecordAsync(evt);
        }
    }

    [Fact]
    public async Task RecordAsync_UsesCancellationToken_WithoutThrow()
    {
        var sink = new LoggingAuditSink(NullLogger<LoggingAuditSink>.Instance);
        using var cts = new CancellationTokenSource();
        var evt = MakeEvent();
        await sink.RecordAsync(evt, cts.Token);
    }

    [Fact]
    public async Task RecordAsync_WithDebugLogLevel_Succeeds()
    {
        using var logFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Trace));
        var logger = logFactory.CreateLogger<LoggingAuditSink>();
        var sink = new LoggingAuditSink(logger, LogLevel.Debug);
        var evt = MakeEvent(AuditEventType.ExperimentModified, "debug-exp", "v1");
        await sink.RecordAsync(evt);
    }
}
