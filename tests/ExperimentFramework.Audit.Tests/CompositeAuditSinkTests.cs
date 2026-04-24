using ExperimentFramework.Audit;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExperimentFramework.Audit.Tests;

public sealed class CompositeAuditSinkTests
{
    private static AuditEvent MakeEvent(AuditEventType type = AuditEventType.VariantSelected)
        => new AuditEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            EventType = type,
            ExperimentName = "test-exp",
        };

    [Fact]
    public async Task RecordAsync_CallsAllSinks()
    {
        var sink1 = new RecordingAuditSink();
        var sink2 = new RecordingAuditSink();
        var composite = new CompositeAuditSink(new IAuditSink[] { sink1, sink2 });
        var evt = MakeEvent();

        await composite.RecordAsync(evt);

        Assert.Equal(1, sink1.RecordedCount);
        Assert.Equal(1, sink2.RecordedCount);
    }

    [Fact]
    public async Task RecordAsync_WithSingleSink_CallsIt()
    {
        var sink = new RecordingAuditSink();
        var composite = new CompositeAuditSink(new IAuditSink[] { sink });
        var evt = MakeEvent(AuditEventType.ExperimentCreated);

        await composite.RecordAsync(evt);

        Assert.Equal(1, sink.RecordedCount);
    }

    [Fact]
    public async Task RecordAsync_WithEmptySinks_Completes()
    {
        var composite = new CompositeAuditSink(Array.Empty<IAuditSink>());
        var evt = MakeEvent();

        // Should not throw
        await composite.RecordAsync(evt);
    }

    [Fact]
    public async Task RecordAsync_ForwardsCorrectEvent_ToSinks()
    {
        var sink1 = new RecordingAuditSink();
        var sink2 = new RecordingAuditSink();
        var composite = new CompositeAuditSink(new IAuditSink[] { sink1, sink2 });
        var evt = MakeEvent(AuditEventType.RolloutChanged);

        await composite.RecordAsync(evt);

        Assert.Same(evt, sink1.LastEvent);
        Assert.Same(evt, sink2.LastEvent);
    }

    [Fact]
    public async Task RecordAsync_ThreeSinks_AllCalled()
    {
        var sinks = Enumerable.Range(0, 3).Select(_ => new RecordingAuditSink()).ToArray();
        var composite = new CompositeAuditSink(sinks.Cast<IAuditSink>().ToArray());
        var evt = MakeEvent();

        await composite.RecordAsync(evt);

        foreach (var s in sinks)
            Assert.Equal(1, s.RecordedCount);
    }

    [Fact]
    public async Task RecordAsync_WithRealSinks_MixedTypes()
    {
        var logging = new LoggingAuditSink(NullLogger<LoggingAuditSink>.Instance);
        var recording = new RecordingAuditSink();
        var composite = new CompositeAuditSink(new IAuditSink[] { logging, recording });
        var evt = MakeEvent(AuditEventType.ExperimentStopped);

        await composite.RecordAsync(evt);

        Assert.Equal(1, recording.RecordedCount);
    }

    private sealed class RecordingAuditSink : IAuditSink
    {
        public int RecordedCount { get; private set; }
        public AuditEvent? LastEvent { get; private set; }

        public ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            RecordedCount++;
            LastEvent = auditEvent;
            return ValueTask.CompletedTask;
        }
    }
}
