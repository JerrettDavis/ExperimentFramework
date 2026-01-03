using ExperimentFramework.Configuration.Models;
using ExperimentFramework.DataPlane.Kafka.Configuration;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.DataPlane.Kafka.Tests;

[Feature("Kafka backplane DSL configuration handler")]
public class KafkaBackplaneConfigurationHandlerTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Handler has correct backplane type")]
    [Fact]
    public Task Handler_has_kafka_backplane_type()
        => Given("a Kafka backplane configuration handler", () => new KafkaBackplaneConfigurationHandler())
            .Then("backplane type should be kafka", 
                handler => handler.BackplaneType == "kafka")
            .AssertPassed();

    [Scenario("Validation returns error when brokers are missing")]
    [Fact]
    public Task Validation_returns_error_when_brokers_missing()
        => Given("a handler and config without brokers", () => 
            {
                var handler = new KafkaBackplaneConfigurationHandler();
                var config = new DataPlaneBackplaneConfig
                {
                    Type = "kafka",
                    Options = new Dictionary<string, object>()
                };
                return (handler, config);
            })
            .When("validating the configuration", 
                state => state.handler.Validate(state.config, "dataPlane.backplane"))
            .Then("should have one error", 
                errors => errors.Count() == 1)
            .And("error message should mention brokers", 
                errors => errors.First().Message.Contains("brokers"))
            .AssertPassed();

    [Scenario("Validation returns no errors when brokers are provided")]
    [Fact]
    public Task Validation_returns_no_errors_when_brokers_provided()
        => Given("a handler and config with brokers", () =>
            {
                var handler = new KafkaBackplaneConfigurationHandler();
                var config = new DataPlaneBackplaneConfig
                {
                    Type = "kafka",
                    Options = new Dictionary<string, object>
                    {
                        ["brokers"] = new List<object> { "localhost:9092" }
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
