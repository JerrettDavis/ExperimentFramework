namespace ExperimentFramework.Testing.Tests;

public class ExperimentTraceAssertionsTests
{
    [Fact]
    public void ExpectRouted_WithMatchingEvent_ReturnsTrue()
    {
        // Arrange
        var sink = new InMemoryExperimentEventSink();
        sink.RecordEvent(new ExperimentTraceEvent
        {
            ServiceType = typeof(IMyDatabase),
            SelectedTrialKey = "true",
            StartTime = DateTimeOffset.UtcNow
        });

        var assertions = new ExperimentTraceAssertions(sink);

        // Act
        var result = assertions.ExpectRouted<IMyDatabase>("true");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ExpectRouted_WithoutMatchingEvent_ReturnsFalse()
    {
        // Arrange
        var sink = new InMemoryExperimentEventSink();
        var assertions = new ExperimentTraceAssertions(sink);

        // Act
        var result = assertions.ExpectRouted<IMyDatabase>("true");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ExpectFallback_WithFallbackEvent_ReturnsTrue()
    {
        // Arrange
        var sink = new InMemoryExperimentEventSink();
        sink.RecordEvent(new ExperimentTraceEvent
        {
            ServiceType = typeof(IMyDatabase),
            IsFallback = true,
            StartTime = DateTimeOffset.UtcNow
        });

        var assertions = new ExperimentTraceAssertions(sink);

        // Act
        var result = assertions.ExpectFallback<IMyDatabase>();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ExpectCall_WithMatchingMethodName_ReturnsTrue()
    {
        // Arrange
        var sink = new InMemoryExperimentEventSink();
        sink.RecordEvent(new ExperimentTraceEvent
        {
            ServiceType = typeof(IMyDatabase),
            MethodName = "GetValue",
            StartTime = DateTimeOffset.UtcNow
        });

        var assertions = new ExperimentTraceAssertions(sink);

        // Act
        var result = assertions.ExpectCall<IMyDatabase>("GetValue");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetEventsFor_ReturnsMatchingEvents()
    {
        // Arrange
        var sink = new InMemoryExperimentEventSink();
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

        var assertions = new ExperimentTraceAssertions(sink);

        // Act
        var events = assertions.GetEventsFor<IMyDatabase>();

        // Assert
        Assert.Equal(2, events.Count);
    }
}
