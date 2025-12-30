using ExperimentFramework.Configuration.Models;
using ExperimentFramework.DataPlane.AzureServiceBus.Configuration;
using FluentAssertions;
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
        handler.BackplaneType.Should().Be("azureServiceBus");
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
        errors.Should().HaveCount(1);
        errors.First().Message.Should().Contain("connectionString");
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
        errors.Should().BeEmpty();
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
        errors.Should().BeEmpty();
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
        errors.Should().BeEmpty();
    }
}
