using ExperimentFramework.DataPlane.AzureServiceBus;
using FluentAssertions;
using Xunit;

namespace ExperimentFramework.DataPlane.AzureServiceBus.Tests;

public class AzureServiceBusDataBackplaneOptionsTests
{
    [Fact]
    public void Options_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new AzureServiceBusDataBackplaneOptions
        {
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;..."
        };

        // Assert
        options.UseTypeSpecificDestinations.Should().BeFalse();
        options.MaxRetryAttempts.Should().Be(3);
        options.BatchSize.Should().Be(100);
        options.EnableSessions.Should().BeFalse();
        options.SessionStrategy.Should().Be(ServiceBusSessionStrategy.ByExperimentKey);
    }

    [Fact]
    public void Options_ShouldAllowCustomization()
    {
        // Arrange & Act
        var options = new AzureServiceBusDataBackplaneOptions
        {
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;...",
            QueueName = "custom-queue",
            UseTypeSpecificDestinations = true,
            MessageTimeToLiveMinutes = 1440,
            MaxRetryAttempts = 5,
            BatchSize = 200,
            EnableSessions = true,
            SessionStrategy = ServiceBusSessionStrategy.BySubjectId,
            ClientId = "test-client"
        };

        // Assert
        options.ConnectionString.Should().NotBeNullOrEmpty();
        options.QueueName.Should().Be("custom-queue");
        options.UseTypeSpecificDestinations.Should().BeTrue();
        options.MessageTimeToLiveMinutes.Should().Be(1440);
        options.MaxRetryAttempts.Should().Be(5);
        options.BatchSize.Should().Be(200);
        options.EnableSessions.Should().BeTrue();
        options.SessionStrategy.Should().Be(ServiceBusSessionStrategy.BySubjectId);
        options.ClientId.Should().Be("test-client");
    }

    [Fact]
    public void Options_ShouldSupportQueueMode()
    {
        // Arrange & Act
        var options = new AzureServiceBusDataBackplaneOptions
        {
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;...",
            QueueName = "experiment-queue"
        };

        // Assert
        options.QueueName.Should().Be("experiment-queue");
        options.TopicName.Should().BeNull();
    }

    [Fact]
    public void Options_ShouldSupportTopicMode()
    {
        // Arrange & Act
        var options = new AzureServiceBusDataBackplaneOptions
        {
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;...",
            TopicName = "experiment-topic"
        };

        // Assert
        options.TopicName.Should().Be("experiment-topic");
        options.QueueName.Should().BeNull();
    }
}
