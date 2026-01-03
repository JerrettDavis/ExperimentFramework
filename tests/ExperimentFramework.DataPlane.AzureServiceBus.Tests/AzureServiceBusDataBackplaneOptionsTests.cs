using ExperimentFramework.DataPlane.AzureServiceBus;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.DataPlane.AzureServiceBus.Tests;

[Feature("Azure Service Bus backplane configuration options")]
public class AzureServiceBusDataBackplaneOptionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Options have correct default values")]
    [Fact]
    public Task Options_have_default_values()
        => Given("a new Azure Service Bus options instance", () => new AzureServiceBusDataBackplaneOptions
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;..."
            })
            .Then("UseTypeSpecificDestinations should be false", 
                options => !options.UseTypeSpecificDestinations)
            .And("MaxRetryAttempts should be 3", 
                options => options.MaxRetryAttempts == 3)
            .And("BatchSize should be 100", 
                options => options.BatchSize == 100)
            .And("EnableSessions should be false", 
                options => !options.EnableSessions)
            .And("SessionStrategy should be ByExperimentKey", 
                options => options.SessionStrategy == ServiceBusSessionStrategy.ByExperimentKey)
            .AssertPassed();

    [Scenario("Options allow customization")]
    [Fact]
    public Task Options_allow_customization()
        => Given("custom Azure Service Bus options", () => new AzureServiceBusDataBackplaneOptions
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
            })
            .Then("ConnectionString should not be empty", 
                options => !string.IsNullOrEmpty(options.ConnectionString))
            .And("QueueName should be custom-queue", 
                options => options.QueueName == "custom-queue")
            .And("UseTypeSpecificDestinations should be true", 
                options => options.UseTypeSpecificDestinations)
            .And("MessageTimeToLiveMinutes should be 1440", 
                options => options.MessageTimeToLiveMinutes == 1440)
            .And("MaxRetryAttempts should be 5", 
                options => options.MaxRetryAttempts == 5)
            .And("BatchSize should be 200", 
                options => options.BatchSize == 200)
            .And("EnableSessions should be true", 
                options => options.EnableSessions)
            .And("SessionStrategy should be BySubjectId", 
                options => options.SessionStrategy == ServiceBusSessionStrategy.BySubjectId)
            .And("ClientId should be test-client", 
                options => options.ClientId == "test-client")
            .AssertPassed();

    [Scenario("Options support queue mode")]
    [Fact]
    public Task Options_support_queue_mode()
        => Given("options configured for queue", () => new AzureServiceBusDataBackplaneOptions
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;...",
                QueueName = "experiment-queue"
            })
            .Then("QueueName should be experiment-queue", 
                options => options.QueueName == "experiment-queue")
            .And("TopicName should be null", 
                options => options.TopicName == null)
            .AssertPassed();

    [Scenario("Options support topic mode")]
    [Fact]
    public Task Options_support_topic_mode()
        => Given("options configured for topic", () => new AzureServiceBusDataBackplaneOptions
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;...",
                TopicName = "experiment-topic"
            })
            .Then("TopicName should be experiment-topic", 
                options => options.TopicName == "experiment-topic")
            .And("QueueName should be null", 
                options => options.QueueName == null)
            .AssertPassed();
}
