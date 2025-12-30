using ExperimentFramework.DataPlane.Kafka;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.DataPlane.Kafka.Tests;

[Feature("Kafka backplane configuration options")]
public class KafkaDataBackplaneOptionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Options have correct default values")]
    [Fact]
    public Task Options_have_default_values()
        => Given("a new Kafka options instance with brokers", () => new KafkaDataBackplaneOptions
            {
                Brokers = new List<string> { "localhost:9092" }
            })
            .Then("partition strategy should be ByExperimentKey", 
                options => options.PartitionStrategy == KafkaPartitionStrategy.ByExperimentKey)
            .And("batch size should be 500", 
                options => options.BatchSize == 500)
            .And("linger ms should be 100", 
                options => options.LingerMs == 100)
            .And("idempotence should be enabled", 
                options => options.EnableIdempotence)
            .And("compression type should be snappy", 
                options => options.CompressionType == "snappy")
            .And("acks should be all", 
                options => options.Acks == "all")
            .And("max in flight should be 5", 
                options => options.MaxInFlight == 5)
            .And("request timeout should be 30000", 
                options => options.RequestTimeoutMs == 30000)
            .AssertPassed();

    [Scenario("Options allow customization")]
    [Fact]
    public Task Options_allow_customization()
        => Given("custom Kafka options", () => new KafkaDataBackplaneOptions
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
            })
            .Then("broker count should be 2", 
                options => options.Brokers.Count == 2)
            .And("topic should be custom-topic", 
                options => options.Topic == "custom-topic")
            .And("partition strategy should be BySubjectId", 
                options => options.PartitionStrategy == KafkaPartitionStrategy.BySubjectId)
            .And("batch size should be 1000", 
                options => options.BatchSize == 1000)
            .And("linger ms should be 200", 
                options => options.LingerMs == 200)
            .And("idempotence should be disabled", 
                options => !options.EnableIdempotence)
            .And("compression type should be gzip", 
                options => options.CompressionType == "gzip")
            .And("acks should be 1", 
                options => options.Acks == "1")
            .And("client id should be test-client", 
                options => options.ClientId == "test-client")
            .AssertPassed();
}
