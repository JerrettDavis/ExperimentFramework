using ExperimentFramework.Configuration.Models;
using ExperimentFramework.DataPlane.Kafka.Configuration;
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
        Assert.Equal("kafka", handler.BackplaneType);
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
        Assert.Single(errors);
        Assert.Contains("brokers", errors.First().Message);
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
        Assert.Empty(errors);
    }
}
