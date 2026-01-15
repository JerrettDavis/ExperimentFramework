namespace ExperimentFramework.Testing.Tests;

public class InMemoryExperimentEventSinkTests
{
    [Fact]
    public void RecordEvent_ShouldAddEventToCollection()
    {
        // Arrange
        var sink = new InMemoryExperimentEventSink();
        var traceEvent = new ExperimentTraceEvent
        {
            ServiceType = typeof(IMyDatabase),
            MethodName = "GetValue",
            SelectedTrialKey = "control",
            StartTime = DateTimeOffset.UtcNow
        };

        // Act
        sink.RecordEvent(traceEvent);

        // Assert
        Assert.Single(sink.Events);
        Assert.Equal(typeof(IMyDatabase), sink.Events[0].ServiceType);
        Assert.Equal("control", sink.Events[0].SelectedTrialKey);
    }

    [Fact]
    public void Clear_ShouldRemoveAllEvents()
    {
        // Arrange
        var sink = new InMemoryExperimentEventSink();
        sink.RecordEvent(new ExperimentTraceEvent
        {
            ServiceType = typeof(IMyDatabase),
            SelectedTrialKey = "control",
            StartTime = DateTimeOffset.UtcNow
        });

        // Act
        sink.Clear();

        // Assert
        Assert.Empty(sink.Events);
    }

    [Fact]
    public void Count_ShouldReturnCorrectNumber()
    {
        // Arrange
        var sink = new InMemoryExperimentEventSink();

        // Act
        sink.RecordEvent(new ExperimentTraceEvent
        {
            ServiceType = typeof(IMyDatabase),
            SelectedTrialKey = "control",
            StartTime = DateTimeOffset.UtcNow
        });
        sink.RecordEvent(new ExperimentTraceEvent
        {
            ServiceType = typeof(IMyDatabase),
            SelectedTrialKey = "true",
            StartTime = DateTimeOffset.UtcNow
        });

        // Assert
        Assert.Equal(2, sink.Count);
    }
}
