using ExperimentFramework.DataPlane.Kafka;
using FluentAssertions;
using Xunit;

namespace ExperimentFramework.DataPlane.Kafka.Tests;

public class KafkaDataBackplaneOptionsTests
{
    [Fact]
    public void Options_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new KafkaDataBackplaneOptions
        {
            Brokers = new List<string> { "localhost:9092" }
        };

        // Assert
        options.PartitionStrategy.Should().Be(KafkaPartitionStrategy.ByExperimentKey);
        options.BatchSize.Should().Be(500);
        options.LingerMs.Should().Be(100);
        options.EnableIdempotence.Should().BeTrue();
        options.CompressionType.Should().Be("snappy");
        options.Acks.Should().Be("all");
        options.MaxInFlight.Should().Be(5);
        options.RequestTimeoutMs.Should().Be(30000);
    }

    [Fact]
    public void Options_ShouldAllowCustomization()
    {
        // Arrange & Act
        var options = new KafkaDataBackplaneOptions
        {
            Brokers = new List<string> { "broker1:9092", "broker2:9092" },
            Topic = "custom-topic",
            PartitionStrategy = KafkaPartitionStrategy.BySubjectId,
            BatchSize = 1000,
            LingerMs = 200,
            EnableIdempotence = false,
            CompressionType = "gzip",
            Acks = "1",
            ClientId = "test-client"
        };

        // Assert
        options.Brokers.Should().HaveCount(2);
        options.Topic.Should().Be("custom-topic");
        options.PartitionStrategy.Should().Be(KafkaPartitionStrategy.BySubjectId);
        options.BatchSize.Should().Be(1000);
        options.LingerMs.Should().Be(200);
        options.EnableIdempotence.Should().BeFalse();
        options.CompressionType.Should().Be("gzip");
        options.Acks.Should().Be("1");
        options.ClientId.Should().Be("test-client");
    }
}
