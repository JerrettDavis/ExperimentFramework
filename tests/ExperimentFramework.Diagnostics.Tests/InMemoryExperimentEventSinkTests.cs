using System.Diagnostics;

namespace ExperimentFramework.Diagnostics.Tests;

public class InMemoryExperimentEventSinkTests
{
    [Fact]
    public void UnboundedSink_StoresAllEvents()
    {
        // Arrange
        var sink = new InMemoryExperimentEventSink();

        // Act
        for (int i = 0; i < 100; i++)
        {
            sink.OnEvent(CreateTestEvent(i));
        }

        // Assert
        Assert.Equal(100, sink.Count);
        Assert.Equal(100, sink.TotalEventCount);
        Assert.Null(sink.MaxCapacity);
    }

    [Fact]
    public void BoundedSink_EnforcesCapacity()
    {
        // Arrange
        var sink = new InMemoryExperimentEventSink(maxCapacity: 10);

        // Act
        for (int i = 0; i < 20; i++)
        {
            sink.OnEvent(CreateTestEvent(i));
        }

        // Assert
        Assert.Equal(10, sink.Count); // Only last 10 retained
        Assert.Equal(20, sink.TotalEventCount); // Total tracked
        Assert.Equal(10, sink.MaxCapacity);
    }

    [Fact]
    public void BoundedSink_RejectsInvalidCapacity()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new InMemoryExperimentEventSink(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InMemoryExperimentEventSink(-1));
    }

    [Fact]
    public void Clear_RemovesAllEvents()
    {
        // Arrange
        var sink = new InMemoryExperimentEventSink();
        sink.OnEvent(CreateTestEvent(1));
        sink.OnEvent(CreateTestEvent(2));

        // Act
        sink.Clear();

        // Assert
        Assert.Equal(0, sink.Count);
        Assert.Equal(0, sink.TotalEventCount);
    }

    [Fact]
    public void GetEventsByKind_FiltersCorrectly()
    {
        // Arrange
        var sink = new InMemoryExperimentEventSink();
        sink.OnEvent(CreateTestEvent(1, ExperimentEventKind.TrialStarted));
        sink.OnEvent(CreateTestEvent(2, ExperimentEventKind.TrialEnded));
        sink.OnEvent(CreateTestEvent(3, ExperimentEventKind.TrialStarted));

        // Act
        var startedEvents = sink.GetEventsByKind(ExperimentEventKind.TrialStarted);

        // Assert
        Assert.Equal(2, startedEvents.Count);
        Assert.All(startedEvents, e => Assert.Equal(ExperimentEventKind.TrialStarted, e.Kind));
    }

    [Fact]
    public void GetEvents_WithPredicate_FiltersCorrectly()
    {
        // Arrange
        var sink = new InMemoryExperimentEventSink();
        sink.OnEvent(CreateTestEvent(1, ExperimentEventKind.TrialStarted, "trial-1"));
        sink.OnEvent(CreateTestEvent(2, ExperimentEventKind.TrialEnded, "trial-2"));
        sink.OnEvent(CreateTestEvent(3, ExperimentEventKind.TrialStarted, "trial-1"));

        // Act
        var trial1Events = sink.GetEvents(e => e.TrialKey == "trial-1");

        // Assert
        Assert.Equal(2, trial1Events.Count);
        Assert.All(trial1Events, e => Assert.Equal("trial-1", e.TrialKey));
    }

    [Fact]
    public void Events_ReturnsSnapshotNotLiveView()
    {
        // Arrange
        var sink = new InMemoryExperimentEventSink();
        sink.OnEvent(CreateTestEvent(1));

        // Act
        var snapshot1 = sink.Events;
        sink.OnEvent(CreateTestEvent(2));
        var snapshot2 = sink.Events;

        // Assert
        Assert.Equal(1, snapshot1.Count);
        Assert.Equal(2, snapshot2.Count);
    }

    [Fact]
    public void ConcurrentAccess_IsThreadSafe()
    {
        // Arrange
        var sink = new InMemoryExperimentEventSink();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    sink.OnEvent(CreateTestEvent(taskId * 100 + j));
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.Equal(1000, sink.TotalEventCount);
    }

    private static ExperimentEvent CreateTestEvent(
        int id,
        ExperimentEventKind kind = ExperimentEventKind.TrialStarted,
        string trialKey = "test-trial")
    {
        return new ExperimentEvent
        {
            Kind = kind,
            Timestamp = DateTimeOffset.UtcNow,
            ServiceType = typeof(InMemoryExperimentEventSinkTests),
            MethodName = $"TestMethod_{id}",
            TrialKey = trialKey,
            SelectorName = "test-selector"
        };
    }
}
