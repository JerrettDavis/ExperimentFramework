using ExperimentFramework.Rollout;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Rollout;

[Feature("RolloutOptions configures percentage-based rollouts")]
public sealed class RolloutOptionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Default values are set correctly")]
    [Fact]
    public Task Default_values_set()
        => Given("default rollout options", () => new RolloutOptions())
            .Then("percentage is 100", opts => opts.Percentage == 100)
            .And("included key is 'true'", opts => opts.IncludedKey == "true")
            .And("excluded key is null", opts => opts.ExcludedKey == null)
            .And("seed is null", opts => opts.Seed == null)
            .AssertPassed();

    [Scenario("All properties can be set")]
    [Fact]
    public Task All_properties_can_be_set()
        => Given("rollout options with all properties", () => new RolloutOptions
            {
                Percentage = 50,
                IncludedKey = "enabled",
                ExcludedKey = "disabled",
                Seed = "my-seed"
            })
            .Then("percentage is set", opts => opts.Percentage == 50)
            .And("included key is set", opts => opts.IncludedKey == "enabled")
            .And("excluded key is set", opts => opts.ExcludedKey == "disabled")
            .And("seed is set", opts => opts.Seed == "my-seed")
            .AssertPassed();

    [Scenario("Percentage can be modified")]
    [Fact]
    public Task Percentage_can_be_modified()
        => Given("rollout options", () => new RolloutOptions())
            .When("changing percentage", opts =>
            {
                opts.Percentage = 25;
                return opts;
            })
            .Then("percentage is updated", opts => opts.Percentage == 25)
            .AssertPassed();
}
