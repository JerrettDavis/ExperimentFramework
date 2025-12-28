using ExperimentFramework.Admin;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Admin;

[Feature("ExperimentInfo represents experiment metadata")]
public sealed class ExperimentInfoTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("ExperimentInfo is created with required properties")]
    [Fact]
    public Task ExperimentInfo_created_with_name()
        => Given("an experiment info", () => new ExperimentInfo
            {
                Name = "test-experiment"
            })
            .Then("has name", info => info.Name == "test-experiment")
            .AssertPassed();

    [Scenario("ExperimentInfo with all properties")]
    [Fact]
    public Task ExperimentInfo_with_all_properties()
        => Given("a fully populated experiment info", () => new ExperimentInfo
            {
                Name = "payment-processor-test",
                ServiceType = typeof(IFormattable),
                IsActive = true,
                Trials = new List<TrialInfo>
                {
                    new() { Key = "control", IsControl = true },
                    new() { Key = "variant-a", IsControl = false }
                },
                Metadata = new Dictionary<string, object>
                {
                    ["owner"] = "team-payments",
                    ["priority"] = 1
                }
            })
            .Then("has service type", info => info.ServiceType == typeof(IFormattable))
            .And("is active", info => info.IsActive)
            .And("has trials", info => info.Trials?.Count == 2)
            .And("has metadata", info => info.Metadata?.Count == 2)
            .AssertPassed();

    [Scenario("IsActive can be modified")]
    [Fact]
    public Task IsActive_can_be_modified()
        => Given("an inactive experiment", () => new ExperimentInfo
            {
                Name = "test",
                IsActive = false
            })
            .When("setting IsActive to true", info =>
            {
                info.IsActive = true;
                return info;
            })
            .Then("is now active", info => info.IsActive)
            .AssertPassed();

    [Scenario("TrialInfo with all properties")]
    [Fact]
    public Task TrialInfo_with_all_properties()
        => Given("a trial info", () => new TrialInfo
            {
                Key = "variant-b",
                ImplementationType = typeof(string),
                IsControl = true
            })
            .Then("has key", trial => trial.Key == "variant-b")
            .And("has implementation type", trial => trial.ImplementationType == typeof(string))
            .And("is control", trial => trial.IsControl)
            .AssertPassed();

    [Scenario("TrialInfo defaults")]
    [Fact]
    public Task TrialInfo_defaults()
        => Given("a minimal trial info", () => new TrialInfo { Key = "test" })
            .Then("implementation type is null", trial => trial.ImplementationType == null)
            .And("is not control by default", trial => !trial.IsControl)
            .AssertPassed();

    [Scenario("ExperimentInfo defaults")]
    [Fact]
    public Task ExperimentInfo_defaults()
        => Given("a minimal experiment info", () => new ExperimentInfo { Name = "test" })
            .Then("service type is null", info => info.ServiceType == null)
            .And("is not active by default", info => !info.IsActive)
            .And("trials is null", info => info.Trials == null)
            .And("metadata is null", info => info.Metadata == null)
            .AssertPassed();
}
