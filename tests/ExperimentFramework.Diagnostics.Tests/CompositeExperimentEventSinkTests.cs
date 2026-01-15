namespace ExperimentFramework.Diagnostics.Tests;

public class CompositeExperimentEventSinkTests
{
    [Fact]
    public void Constructor_AcceptsMultipleSinks()
    {
        // Arrange
        var sink1 = new InMemoryExperimentEventSink();
        var sink2 = new InMemoryExperimentEventSink();

        // Act
        var composite = new CompositeExperimentEventSink(sink1, sink2);

        // Assert
        Assert.Equal(2, composite.SinkCount);
    }

    [Fact]
    public void Constructor_ThrowsOnNullSinks()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CompositeExperimentEventSink((IExperimentEventSink[])null!));
        Assert.Throws<ArgumentNullException>(() => new CompositeExperimentEventSink((IEnumerable<IExperimentEventSink>)null!));
    }

    [Fact]
    public void OnEvent_ForwardsToAllSinks()
    {
        // Arrange
        var sink1 = new InMemoryExperimentEventSink();
        var sink2 = new InMemoryExperimentEventSink();
        var composite = new CompositeExperimentEventSink(sink1, sink2);
        var evt = CreateEvent();

        // Act
        composite.OnEvent(evt);

        // Assert
        Assert.Equal(1, sink1.Count);
        Assert.Equal(1, sink2.Count);
        Assert.Equal(evt.Kind, sink1.Events[0].Kind);
        Assert.Equal(evt.Kind, sink2.Events[0].Kind);
    }

    [Fact]
    public void OnEvent_ContinuesOnSinkException()
    {
        // Arrange
        var goodSink = new InMemoryExperimentEventSink();
        var throwingSink = new ThrowingEventSink();
        var composite = new CompositeExperimentEventSink(throwingSink, goodSink);
        var evt = CreateEvent();

        // Act - should not throw
        composite.OnEvent(evt);

        // Assert - good sink should still receive the event
        Assert.Equal(1, goodSink.Count);
    }

    [Fact]
    public void OnEvent_PreservesOrder()
    {
        // Arrange
        var recorder = new OrderRecordingSink();
        var sink1 = new OrderedSink(recorder, 1);
        var sink2 = new OrderedSink(recorder, 2);
        var sink3 = new OrderedSink(recorder, 3);
        var composite = new CompositeExperimentEventSink(sink1, sink2, sink3);
        var evt = CreateEvent();

        // Act
        composite.OnEvent(evt);

        // Assert
        Assert.Equal(new[] { 1, 2, 3 }, recorder.Order);
    }

    private static ExperimentEvent CreateEvent()
    {
        return new ExperimentEvent
        {
            Kind = ExperimentEventKind.TrialStarted,
            Timestamp = DateTimeOffset.UtcNow,
            ServiceType = typeof(CompositeExperimentEventSinkTests),
            MethodName = "TestMethod",
            TrialKey = "test-trial"
        };
    }

    private class ThrowingEventSink : IExperimentEventSink
    {
        public void OnEvent(in ExperimentEvent e)
        {
            throw new InvalidOperationException("Test exception");
        }
    }

    private class OrderRecordingSink
    {
        public List<int> Order { get; } = new();
    }

    private class OrderedSink : IExperimentEventSink
    {
        private readonly OrderRecordingSink _recorder;
        private readonly int _id;

        public OrderedSink(OrderRecordingSink recorder, int id)
        {
            _recorder = recorder;
            _id = id;
        }

        public void OnEvent(in ExperimentEvent e)
        {
            _recorder.Order.Add(_id);
        }
    }
}
