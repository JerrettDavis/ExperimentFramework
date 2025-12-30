using ExperimentFramework.Configuration.Models;
using ExperimentFramework.DataPlane.AzureServiceBus.Configuration;
using Xunit;

namespace ExperimentFramework.DataPlane.AzureServiceBus.Tests;

public class AzureServiceBusBackplaneConfigurationHandlerTests
{
    [Fact]
    public void BackplaneType_ShouldBeAzureServiceBus()
    {
        // Arrange
        var handler = new AzureServiceBusBackplaneConfigurationHandler();

        // Act & Assert
        Assert.Equal("azureServiceBus", handler.BackplaneType);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenConnectionStringIsMissing()
    {
        // Arrange
        var handler = new AzureServiceBusBackplaneConfigurationHandler();
        var config = new DataPlaneBackplaneConfig
        {
            Type = "azureServiceBus",
            Options = new Dictionary<string, object>()
        };

        // Act
        var errors = handler.Validate(config, "dataPlane.backplane");

        // Assert
        Assert.Single(errors);
        Assert.Contains("connectionString", errors.First().Message);
    }

    [Fact]
    public void Validate_ShouldReturnNoErrors_WhenConnectionStringIsProvided()
    {
        // Arrange
        var handler = new AzureServiceBusBackplaneConfigurationHandler();
        var config = new DataPlaneBackplaneConfig
        {
            Type = "azureServiceBus",
            Options = new Dictionary<string, object>
            {
                ["connectionString"] = "Endpoint=sb://test.servicebus.windows.net/;..."
            }
        };

        // Act
        var errors = handler.Validate(config, "dataPlane.backplane");

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldAcceptQueueConfiguration()
    {
        // Arrange
        var handler = new AzureServiceBusBackplaneConfigurationHandler();
        var config = new DataPlaneBackplaneConfig
        {
            Type = "azureServiceBus",
            Options = new Dictionary<string, object>
            {
                ["connectionString"] = "Endpoint=sb://test.servicebus.windows.net/;...",
                ["queueName"] = "experiment-queue"
            }
        };

        // Act
        var errors = handler.Validate(config, "dataPlane.backplane");

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ShouldAcceptTopicConfiguration()
    {
        // Arrange
        var handler = new AzureServiceBusBackplaneConfigurationHandler();
        var config = new DataPlaneBackplaneConfig
        {
            Type = "azureServiceBus",
            Options = new Dictionary<string, object>
            {
                ["connectionString"] = "Endpoint=sb://test.servicebus.windows.net/;...",
                ["topicName"] = "experiment-topic"
            }
        };

        // Act
        var errors = handler.Validate(config, "dataPlane.backplane");

        // Assert
        Assert.Empty(errors);
    }
}
