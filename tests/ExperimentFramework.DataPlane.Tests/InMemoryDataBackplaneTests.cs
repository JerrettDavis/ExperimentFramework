using ExperimentFramework.DataPlane.Abstractions;
using ExperimentFramework.DataPlane.Implementations;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExperimentFramework.DataPlane.Tests;

public class InMemoryDataBackplaneTests
{
    [Fact]
    public async Task PublishAsync_StoresEvent()
    {
        // Arrange
        var backplane = new InMemoryDataBackplane(NullLogger<InMemoryDataBackplane>.Instance);
        var envelope = new DataPlaneEnvelope
        {
            EventId = "test-1",
            Timestamp = DateTimeOffset.UtcNow,
            EventType = DataPlaneEventType.Exposure,
            SchemaVersion = "1.0.0",
            Payload = new { Test = "value" }
        };

        // Act
        await backplane.PublishAsync(envelope);

        // Assert
        backplane.Events.Should().HaveCount(1);
        backplane.Events.First().EventId.Should().Be("test-1");
    }

    [Fact]
    public async Task HealthAsync_ReturnsHealthy()
    {
        // Arrange
        var backplane = new InMemoryDataBackplane(NullLogger<InMemoryDataBackplane>.Instance);

        // Act
        var health = await backplane.HealthAsync();

        // Assert
        health.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task Clear_RemovesAllEvents()
    {
        // Arrange
        var backplane = new InMemoryDataBackplane(NullLogger<InMemoryDataBackplane>.Instance);
        var envelope = new DataPlaneEnvelope
        {
            EventId = "test-1",
            Timestamp = DateTimeOffset.UtcNow,
            EventType = DataPlaneEventType.Exposure,
            SchemaVersion = "1.0.0",
            Payload = new { Test = "value" }
        };
        await backplane.PublishAsync(envelope);

        // Act
        backplane.Clear();

        // Assert
        backplane.Events.Should().BeEmpty();
    }
}
