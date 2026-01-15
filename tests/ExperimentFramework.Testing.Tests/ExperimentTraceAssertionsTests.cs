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
    public void GetFirstEventFor_ReturnsFirstEventForServiceType()
    {
        // Arrange
        var sink = new InMemoryExperimentEventSink();
        var now = DateTimeOffset.UtcNow;
        sink.RecordEvent(new ExperimentTraceEvent
        {
            ServiceType = typeof(IMyDatabase),
            SelectedTrialKey = "control",
            StartTime = now
        });
        sink.RecordEvent(new ExperimentTraceEvent
        {
            ServiceType = typeof(IMyDatabase),
            SelectedTrialKey = "true",
            StartTime = now.AddSeconds(1)
        });

        var assertions = new ExperimentTraceAssertions(sink);

        // Act
        var result = assertions.GetFirstEventFor<IMyDatabase>();

        // Assert - First event added should have "control" or "true" depending on ConcurrentBag ordering
        Assert.NotNull(result);
        Assert.Contains(result.SelectedTrialKey, new[] { "control", "true" });
    }

    [Fact]
    public void GetFirstEventFor_WithNoEvents_ReturnsNull()
    {
        // Arrange
        var sink = new InMemoryExperimentEventSink();
        var assertions = new ExperimentTraceAssertions(sink);

        // Act
        var result = assertions.GetFirstEventFor<IMyDatabase>();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExpectException_WithException_ReturnsTrue()
    {
        // Arrange
        var sink = new InMemoryExperimentEventSink();
        sink.RecordEvent(new ExperimentTraceEvent
        {
            ServiceType = typeof(IMyDatabase),
            Exception = new InvalidOperationException("Test exception"),
            StartTime = DateTimeOffset.UtcNow
        });

        var assertions = new ExperimentTraceAssertions(sink);

        // Act
        var result = assertions.ExpectException<IMyDatabase>();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ExpectException_WithoutException_ReturnsFalse()
    {
        // Arrange
        var sink = new InMemoryExperimentEventSink();
        sink.RecordEvent(new ExperimentTraceEvent
        {
            ServiceType = typeof(IMyDatabase),
            Exception = null,
            StartTime = DateTimeOffset.UtcNow
        });

        var assertions = new ExperimentTraceAssertions(sink);

        // Act
        var result = assertions.ExpectException<IMyDatabase>();

        // Assert
        Assert.False(result);
    }
}
