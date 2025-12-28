using ExperimentFramework.Bandit;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Bandit;

[Feature("ArmStatistics tracks bandit arm performance")]
public sealed class ArmStatisticsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("AverageReward is 0 when no pulls")]
    [Fact]
    public Task AverageReward_zero_with_no_pulls()
        => Given("an arm with no pulls", () => new ArmStatistics { Key = "test", Pulls = 0, TotalReward = 0 })
            .Then("average reward is 0", arm => Math.Abs(arm.AverageReward) < 0.0001)
            .AssertPassed();

    [Scenario("AverageReward is calculated correctly")]
    [Fact]
    public Task AverageReward_calculated_correctly()
        => Given("an arm with pulls and rewards", () => new ArmStatistics
            {
                Key = "test",
                Pulls = 10,
                TotalReward = 8.0
            })
            .Then("average reward is total/pulls", arm => Math.Abs(arm.AverageReward - 0.8) < 0.0001)
            .AssertPassed();

    [Scenario("All properties can be set")]
    [Fact]
    public Task All_properties_set()
        => Given("an arm with all properties", () => new ArmStatistics
            {
                Key = "test-arm",
                Pulls = 100,
                TotalReward = 75.5,
                Successes = 75,
                Failures = 25
            })
            .Then("key is set", arm => arm.Key == "test-arm")
            .And("pulls is set", arm => arm.Pulls == 100)
            .And("total reward is set", arm => Math.Abs(arm.TotalReward - 75.5) < 0.0001)
            .And("successes is set", arm => arm.Successes == 75)
            .And("failures is set", arm => arm.Failures == 25)
            .AssertPassed();

    [Scenario("Properties can be modified")]
    [Fact]
    public Task Properties_can_be_modified()
        => Given("an arm", () => new ArmStatistics { Key = "test" })
            .When("modifying properties", arm =>
            {
                arm.Pulls = 5;
                arm.TotalReward = 3.0;
                arm.Successes = 3;
                arm.Failures = 2;
                return arm;
            })
            .Then("pulls is updated", arm => arm.Pulls == 5)
            .And("total reward is updated", arm => Math.Abs(arm.TotalReward - 3.0) < 0.0001)
            .And("successes is updated", arm => arm.Successes == 3)
            .And("failures is updated", arm => arm.Failures == 2)
            .And("average is correct", arm => Math.Abs(arm.AverageReward - 0.6) < 0.0001)
            .AssertPassed();
}
