using ExperimentFramework.DataPlane.Kafka;
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
        Assert.Equal(KafkaPartitionStrategy.ByExperimentKey, options.PartitionStrategy);
        Assert.Equal(500, options.BatchSize);
        Assert.Equal(100, options.LingerMs);
        Assert.True(options.EnableIdempotence);
        Assert.Equal("snappy", options.CompressionType);
        Assert.Equal("all", options.Acks);
        Assert.Equal(5, options.MaxInFlight);
        Assert.Equal(30000, options.RequestTimeoutMs);
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
        Assert.Equal(2, options.Brokers.Count);
        Assert.Equal("custom-topic", options.Topic);
        Assert.Equal(KafkaPartitionStrategy.BySubjectId, options.PartitionStrategy);
        Assert.Equal(1000, options.BatchSize);
        Assert.Equal(200, options.LingerMs);
        Assert.False(options.EnableIdempotence);
        Assert.Equal("gzip", options.CompressionType);
        Assert.Equal("1", options.Acks);
        Assert.Equal("test-client", options.ClientId);
    }
}
