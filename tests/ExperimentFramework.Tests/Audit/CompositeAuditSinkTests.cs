using ExperimentFramework.Audit;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Audit;

[Feature("CompositeAuditSink aggregates multiple sinks")]
public sealed class CompositeAuditSinkTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Composite sink calls all child sinks")]
    [Fact]
    public async Task Composite_calls_all_sinks()
    {
        var sink1 = new TestAuditSink();
        var sink2 = new TestAuditSink();
        var sink3 = new TestAuditSink();

        var composite = new CompositeAuditSink([sink1, sink2, sink3]);

        var evt = new AuditEvent
        {
            EventId = Guid.NewGuid().ToString(),
            ExperimentName = "test",
            EventType = AuditEventType.ExperimentStarted,
            Timestamp = DateTimeOffset.UtcNow
        };

        await composite.RecordAsync(evt);

        Assert.Single(sink1.Events);
        Assert.Single(sink2.Events);
        Assert.Single(sink3.Events);
    }

    [Scenario("Composite sink with empty sinks does not throw")]
    [Fact]
    public async Task Composite_with_empty_sinks()
    {
        var composite = new CompositeAuditSink([]);

        var evt = new AuditEvent
        {
            EventId = Guid.NewGuid().ToString(),
            ExperimentName = "test",
            EventType = AuditEventType.ExperimentStarted,
            Timestamp = DateTimeOffset.UtcNow
        };

        await composite.RecordAsync(evt);
        // Should not throw
    }

    [Scenario("Composite sink with single sink forwards events")]
    [Fact]
    public async Task Composite_with_single_sink()
    {
        var sink = new TestAuditSink();
        var composite = new CompositeAuditSink([sink]);

        var evt = new AuditEvent
        {
            EventId = Guid.NewGuid().ToString(),
            ExperimentName = "test-experiment",
            EventType = AuditEventType.VariantSelected,
            Timestamp = DateTimeOffset.UtcNow
        };

        await composite.RecordAsync(evt);

        Assert.Single(sink.Events);
        Assert.Equal("test-experiment", sink.Events[0].ExperimentName);
    }

    [Scenario("Composite sink respects cancellation token")]
    [Fact]
    public async Task Composite_respects_cancellation()
    {
        var sink = new TestAuditSink();
        var composite = new CompositeAuditSink([sink]);

        var evt = new AuditEvent
        {
            EventId = Guid.NewGuid().ToString(),
            ExperimentName = "test",
            EventType = AuditEventType.ExperimentStarted,
            Timestamp = DateTimeOffset.UtcNow
        };

        using var cts = new CancellationTokenSource();
        await composite.RecordAsync(evt, cts.Token);

        Assert.Single(sink.Events);
    }

    [Scenario("Composite sink records multiple events")]
    [Fact]
    public async Task Composite_records_multiple_events()
    {
        var sink = new TestAuditSink();
        var composite = new CompositeAuditSink([sink]);

        for (int i = 0; i < 5; i++)
        {
            await composite.RecordAsync(new AuditEvent
            {
                EventId = Guid.NewGuid().ToString(),
                ExperimentName = $"experiment-{i}",
                EventType = AuditEventType.ExperimentStarted,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        Assert.Equal(5, sink.Events.Count);
    }

    private sealed class TestAuditSink : IAuditSink
    {
        public List<AuditEvent> Events { get; } = [];

        public ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return ValueTask.CompletedTask;
        }
    }
}
