using ExperimentFramework.Configuration.Models;
using ExperimentFramework.DataPlane.AzureServiceBus.Configuration;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.DataPlane.AzureServiceBus.Tests;

[Feature("Azure Service Bus backplane DSL configuration handler")]
public class AzureServiceBusBackplaneConfigurationHandlerTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Handler has correct backplane type")]
    [Fact]
    public Task Handler_has_azure_service_bus_backplane_type()
        => Given("an Azure Service Bus backplane configuration handler", () => new AzureServiceBusBackplaneConfigurationHandler())
            .Then("backplane type should be azureServiceBus", 
                handler => handler.BackplaneType == "azureServiceBus")
            .AssertPassed();

    [Scenario("Validation returns error when connection string is missing")]
    [Fact]
    public Task Validation_returns_error_when_connection_string_missing()
        => Given("a handler and config without connection string", () =>
            {
                var handler = new AzureServiceBusBackplaneConfigurationHandler();
                var config = new DataPlaneBackplaneConfig
                {
                    Type = "azureServiceBus",
                    Options = new Dictionary<string, object>()
                };
                return (handler, config);
            })
            .When("validating the configuration", 
                state => state.handler.Validate(state.config, "dataPlane.backplane"))
            .Then("should have one error", 
                errors => errors.Count() == 1)
            .And("error message should mention connectionString", 
                errors => errors.First().Message.Contains("connectionString"))
            .AssertPassed();

    [Scenario("Validation returns no errors when connection string is provided")]
    [Fact]
    public Task Validation_returns_no_errors_when_connection_string_provided()
        => Given("a handler and config with connection string", () =>
            {
                var handler = new AzureServiceBusBackplaneConfigurationHandler();
                var config = new DataPlaneBackplaneConfig
                {
                    Type = "azureServiceBus",
                    Options = new Dictionary<string, object>
                    {
                        ["connectionString"] = "Endpoint=sb://test.servicebus.windows.net/;..."
                    }
                };
                return (handler, config);
            })
            .When("validating the configuration", 
                state => state.handler.Validate(state.config, "dataPlane.backplane"))
            .Then("should have no errors", 
                errors => !errors.Any())
            .AssertPassed();

    [Scenario("Validation accepts queue configuration")]
    [Fact]
    public Task Validation_accepts_queue_configuration()
        => Given("a handler and config with queue", () =>
            {
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
                return (handler, config);
            })
            .When("validating the configuration", 
                state => state.handler.Validate(state.config, "dataPlane.backplane"))
            .Then("should have no errors", 
                errors => !errors.Any())
            .AssertPassed();

    [Scenario("Validation accepts topic configuration")]
    [Fact]
    public Task Validation_accepts_topic_configuration()
        => Given("a handler and config with topic", () =>
            {
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
                return (handler, config);
            })
            .When("validating the configuration", 
                state => state.handler.Validate(state.config, "dataPlane.backplane"))
            .Then("should have no errors", 
                errors => !errors.Any())
            .AssertPassed();
}
