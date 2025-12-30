using ExperimentFramework.Configuration.Models;
using ExperimentFramework.DataPlane.Kafka.Configuration;
using FluentAssertions;
using Xunit;

namespace ExperimentFramework.DataPlane.Kafka.Tests;

public class KafkaBackplaneConfigurationHandlerTests
{
    [Fact]
    public void BackplaneType_ShouldBeKafka()
    {
        // Arrange
        var handler = new KafkaBackplaneConfigurationHandler();

        // Act & Assert
        handler.BackplaneType.Should().Be("kafka");
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenBrokersAreMissing()
    {
        // Arrange
        var handler = new KafkaBackplaneConfigurationHandler();
        var config = new DataPlaneBackplaneConfig
        {
            Type = "kafka",
            Options = new Dictionary<string, object>()
        };

        // Act
        var errors = handler.Validate(config, "dataPlane.backplane");

        // Assert
        errors.Should().HaveCount(1);
        errors.First().Message.Should().Contain("brokers");
    }

    [Fact]
    public void Validate_ShouldReturnNoErrors_WhenBrokersAreProvided()
    {
        // Arrange
        var handler = new KafkaBackplaneConfigurationHandler();
        var config = new DataPlaneBackplaneConfig
        {
            Type = "kafka",
            Options = new Dictionary<string, object>
            {
                ["brokers"] = new List<object> { "localhost:9092" }
            }
        };

        // Act
        var errors = handler.Validate(config, "dataPlane.backplane");

        // Assert
        errors.Should().BeEmpty();
    }
}
