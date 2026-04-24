using ExperimentFramework.Audit;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Audit;

[Feature("Audit system – event ordering, filtering, serialization, and aggregation")]
public sealed class AuditOrderingTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private static AuditEvent MakeEvent(AuditEventType type, string name, DateTimeOffset? timestamp = null)
        => new()
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = timestamp ?? DateTimeOffset.UtcNow,
            EventType = type,
            ExperimentName = name
        };

    // ── ordering ──────────────────────────────────────────────────────────────

    [Scenario("Events are recorded in insertion order")]
    [Fact]
    public async Task Events_are_recorded_in_insertion_order()
    {
        var sink = new OrderedAuditSink();
        var types = new[]
        {
            AuditEventType.ExperimentCreated,
            AuditEventType.ExperimentStarted,
            AuditEventType.VariantSelected,
            AuditEventType.RolloutChanged,
            AuditEventType.ExperimentStopped
        };

        foreach (var type in types)
            await sink.RecordAsync(MakeEvent(type, "order-exp"));

        Assert.Equal(types, sink.Events.Select(e => e.EventType));
    }

    [Scenario("Composite sink preserves insertion order across multiple child sinks")]
    [Fact]
    public async Task Composite_preserves_order()
    {
        var sink1 = new OrderedAuditSink();
        var sink2 = new OrderedAuditSink();
        var composite = new CompositeAuditSink([sink1, sink2]);

        for (var i = 0; i < 5; i++)
            await composite.RecordAsync(MakeEvent(AuditEventType.VariantSelected, $"exp-{i}"));

        Assert.Equal(5, sink1.Events.Count);
        Assert.Equal(5, sink2.Events.Count);
        Assert.Equal(sink1.Events.Select(e => e.ExperimentName),
                     sink2.Events.Select(e => e.ExperimentName));
    }

    // ── filtering ─────────────────────────────────────────────────────────────

    [Scenario("Filter by experiment name from a combined event stream")]
    [Fact]
    public async Task Filter_by_experiment_name()
    {
        var sink = new OrderedAuditSink();
        var names = new[] { "alpha", "beta", "gamma", "alpha", "beta" };

        foreach (var name in names)
            await sink.RecordAsync(MakeEvent(AuditEventType.VariantSelected, name));

        var alphaEvents = sink.Events.Where(e => e.ExperimentName == "alpha").ToList();
        var betaEvents  = sink.Events.Where(e => e.ExperimentName == "beta").ToList();

        Assert.Equal(2, alphaEvents.Count);
        Assert.Equal(2, betaEvents.Count);
    }

    [Scenario("Filter by event type returns only matching events")]
    [Fact]
    public async Task Filter_by_event_type()
    {
        var sink = new OrderedAuditSink();
        await sink.RecordAsync(MakeEvent(AuditEventType.ExperimentCreated, "exp-1"));
        await sink.RecordAsync(MakeEvent(AuditEventType.VariantSelected, "exp-1"));
        await sink.RecordAsync(MakeEvent(AuditEventType.ExperimentCreated, "exp-2"));
        await sink.RecordAsync(MakeEvent(AuditEventType.ExperimentStopped, "exp-2"));

        var created = sink.Events.Where(e => e.EventType == AuditEventType.ExperimentCreated).ToList();

        Assert.Equal(2, created.Count);
        Assert.All(created, e => Assert.Equal(AuditEventType.ExperimentCreated, e.EventType));
    }

    [Scenario("Filter by time range returns events within window")]
    [Fact]
    public async Task Filter_by_time_range()
    {
        var sink = new OrderedAuditSink();
        var start = DateTimeOffset.UtcNow.AddMinutes(-10);
        var end   = DateTimeOffset.UtcNow.AddMinutes(-5);

        await sink.RecordAsync(MakeEvent(AuditEventType.VariantSelected, "before", start.AddMinutes(-1)));
        await sink.RecordAsync(MakeEvent(AuditEventType.VariantSelected, "in-window-1", start.AddMinutes(1)));
        await sink.RecordAsync(MakeEvent(AuditEventType.VariantSelected, "in-window-2", start.AddMinutes(3)));
        await sink.RecordAsync(MakeEvent(AuditEventType.VariantSelected, "after", end.AddMinutes(1)));

        var inWindow = sink.Events
            .Where(e => e.Timestamp >= start && e.Timestamp <= end)
            .ToList();

        Assert.Equal(2, inWindow.Count);
    }

    // ── serialization ────────────────────────────────────────────────────────

    [Scenario("AuditEvent serializes and round-trips via JSON")]
    [Fact]
    public Task AuditEvent_roundtrips_json()
    {
        var evt = new AuditEvent
        {
            EventId = "json-test-id",
            Timestamp = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero),
            EventType = AuditEventType.ExperimentCreated,
            ExperimentName = "json-exp",
            Actor = "admin@example.com",
            CorrelationId = "corr-001",
            Details = new Dictionary<string, object> { ["key"] = "value" }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(evt);
        var restored = System.Text.Json.JsonSerializer.Deserialize<AuditEvent>(json);

        Assert.NotNull(restored);
        Assert.Equal(evt.EventId, restored!.EventId);
        Assert.Equal(evt.EventType, restored.EventType);
        Assert.Equal(evt.ExperimentName, restored.ExperimentName);
        Assert.Equal(evt.Actor, restored.Actor);
        Assert.Equal(evt.CorrelationId, restored.CorrelationId);

        return Task.CompletedTask;
    }

    [Scenario("AuditEventType enum values serialize to expected strings")]
    [Fact]
    public Task AuditEventType_serializes_as_expected_strings()
    {
        var options = new System.Text.Json.JsonSerializerOptions
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        foreach (var value in Enum.GetValues<AuditEventType>())
        {
            var json = System.Text.Json.JsonSerializer.Serialize(value, options);
            Assert.NotNull(json);
            Assert.DoesNotContain("null", json);
        }

        return Task.CompletedTask;
    }

    // ── aggregation ──────────────────────────────────────────────────────────

    [Scenario("Aggregation counts events per experiment")]
    [Fact]
    public async Task Aggregation_counts_per_experiment()
    {
        var sink = new OrderedAuditSink();
        var experiments = new[] { "a", "a", "b", "b", "b", "c" };

        foreach (var name in experiments)
            await sink.RecordAsync(MakeEvent(AuditEventType.VariantSelected, name));

        var counts = sink.Events
            .GroupBy(e => e.ExperimentName)
            .ToDictionary(g => g.Key!, g => g.Count());

        Assert.Equal(2, counts["a"]);
        Assert.Equal(3, counts["b"]);
        Assert.Equal(1, counts["c"]);
    }

    [Scenario("Aggregation counts events per type")]
    [Fact]
    public async Task Aggregation_counts_per_type()
    {
        var sink = new OrderedAuditSink();
        await sink.RecordAsync(MakeEvent(AuditEventType.VariantSelected, "exp"));
        await sink.RecordAsync(MakeEvent(AuditEventType.VariantSelected, "exp"));
        await sink.RecordAsync(MakeEvent(AuditEventType.ExperimentStarted, "exp"));
        await sink.RecordAsync(MakeEvent(AuditEventType.FallbackTriggered, "exp"));

        var byType = sink.Events
            .GroupBy(e => e.EventType)
            .ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(2, byType[AuditEventType.VariantSelected]);
        Assert.Equal(1, byType[AuditEventType.ExperimentStarted]);
        Assert.Equal(1, byType[AuditEventType.FallbackTriggered]);
    }

    // ── DI registration ───────────────────────────────────────────────────────

    [Scenario("AddExperimentAuditLogging registers singleton IAuditSink")]
    [Fact]
    public Task AddExperimentAuditLogging_registers_sink()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExperimentAuditLogging();
        var sp = services.BuildServiceProvider();

        var sink = sp.GetService<IAuditSink>();

        Assert.NotNull(sink);

        return Task.CompletedTask;
    }

    [Scenario("AddExperimentAuditSink registers custom sink")]
    [Fact]
    public Task AddExperimentAuditSink_registers_custom_sink()
    {
        var services = new ServiceCollection();
        services.AddExperimentAuditSink<OrderedAuditSink>();
        var sp = services.BuildServiceProvider();

        var sinks = sp.GetServices<IAuditSink>().ToList();

        Assert.Single(sinks);
        Assert.IsType<OrderedAuditSink>(sinks[0]);

        return Task.CompletedTask;
    }

    private sealed class OrderedAuditSink : IAuditSink
    {
        public List<AuditEvent> Events { get; } = [];

        public ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return ValueTask.CompletedTask;
        }
    }
}
