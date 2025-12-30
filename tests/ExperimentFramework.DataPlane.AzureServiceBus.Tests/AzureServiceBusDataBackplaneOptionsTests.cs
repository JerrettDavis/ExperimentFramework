using ExperimentFramework.DataPlane.AzureServiceBus;
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
        Assert.False(options.UseTypeSpecificDestinations);
        Assert.Equal(3, options.MaxRetryAttempts);
        Assert.Equal(100, options.BatchSize);
        Assert.False(options.EnableSessions);
        Assert.Equal(ServiceBusSessionStrategy.ByExperimentKey, options.SessionStrategy);
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
        Assert.NotEmpty(options.ConnectionString);
        Assert.Equal("custom-queue", options.QueueName);
        Assert.True(options.UseTypeSpecificDestinations);
        Assert.Equal(1440, options.MessageTimeToLiveMinutes);
        Assert.Equal(5, options.MaxRetryAttempts);
        Assert.Equal(200, options.BatchSize);
        Assert.True(options.EnableSessions);
        Assert.Equal(ServiceBusSessionStrategy.BySubjectId, options.SessionStrategy);
        Assert.Equal("test-client", options.ClientId);
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
        Assert.Equal("experiment-queue", options.QueueName);
        Assert.Null(options.TopicName);
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
        Assert.Equal("experiment-topic", options.TopicName);
        Assert.Null(options.QueueName);
    }
}
