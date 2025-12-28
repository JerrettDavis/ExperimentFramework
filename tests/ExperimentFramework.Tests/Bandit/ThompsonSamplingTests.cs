using ExperimentFramework.Bandit;
using ExperimentFramework.Bandit.Algorithms;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Bandit;

[Feature("Thompson Sampling bandit algorithm uses Bayesian probability matching")]
public sealed class ThompsonSamplingTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Algorithm has correct name")]
    [Fact]
    public Task Algorithm_has_correct_name()
        => Given("a Thompson Sampling algorithm", () => new ThompsonSampling())
            .Then("name is ThompsonSampling", alg => alg.Name == "ThompsonSampling")
            .AssertPassed();

    [Scenario("Throws for empty arms")]
    [Fact]
    public void Throws_for_empty_arms()
    {
        var algorithm = new ThompsonSampling();
        var exception = Assert.Throws<ArgumentException>(() =>
            algorithm.SelectArm(Array.Empty<ArmStatistics>()));

        Assert.Equal("arms", exception.ParamName);
    }

    [Scenario("Selects from available arms")]
    [Fact]
    public Task Selects_valid_arm_index()
        => Given("arms with statistics", () =>
            {
                var arms = new[]
                {
                    new ArmStatistics { Key = "arm-0", Successes = 10, Failures = 5 },
                    new ArmStatistics { Key = "arm-1", Successes = 20, Failures = 10 },
                    new ArmStatistics { Key = "arm-2", Successes = 5, Failures = 15 }
                };
                return (Algorithm: new ThompsonSampling(seed: 42), Arms: arms);
            })
            .When("selecting 100 times", data =>
                Enumerable.Range(0, 100)
                    .Select(_ => data.Algorithm.SelectArm(data.Arms))
                    .ToList())
            .Then("all selections are valid indices", selections =>
                selections.All(s => s >= 0 && s < 3))
            .AssertPassed();

    [Scenario("UpdateArm increments statistics")]
    [Fact]
    public Task UpdateArm_updates_statistics()
        => Given("an arm with initial statistics", () => new ArmStatistics
            {
                Key = "test-arm",
                Pulls = 10,
                TotalReward = 5.0,
                Successes = 5,
                Failures = 5
            })
            .When("updating with a high reward", arm =>
            {
                var algorithm = new ThompsonSampling();
                algorithm.UpdateArm(arm, 0.8);
                return arm;
            })
            .Then("pulls is incremented", arm => arm.Pulls == 11)
            .And("reward is added", arm => Math.Abs(arm.TotalReward - 5.8) < 0.001)
            .And("successes is incremented for high reward", arm => arm.Successes == 6)
            .And("failures unchanged", arm => arm.Failures == 5)
            .AssertPassed();

    [Scenario("UpdateArm increments failures for low reward")]
    [Fact]
    public Task UpdateArm_increments_failures_for_low_reward()
        => Given("an arm with initial statistics", () => new ArmStatistics
            {
                Key = "test-arm",
                Pulls = 10,
                TotalReward = 5.0,
                Successes = 5,
                Failures = 5
            })
            .When("updating with a low reward", arm =>
            {
                var algorithm = new ThompsonSampling();
                algorithm.UpdateArm(arm, 0.2);
                return arm;
            })
            .Then("pulls is incremented", arm => arm.Pulls == 11)
            .And("failures is incremented for low reward", arm => arm.Failures == 6)
            .And("successes unchanged", arm => arm.Successes == 5)
            .AssertPassed();

    [Scenario("Arm with more successes is selected more often")]
    [Fact]
    public Task Better_arm_selected_more_often()
        => Given("arms with different success rates", () =>
            {
                var arms = new[]
                {
                    new ArmStatistics { Key = "bad-arm", Successes = 2, Failures = 18 },   // 10% success
                    new ArmStatistics { Key = "good-arm", Successes = 18, Failures = 2 }   // 90% success
                };
                return (Algorithm: new ThompsonSampling(seed: 42), Arms: arms);
            })
            .When("selecting 1000 times", data =>
                Enumerable.Range(0, 1000)
                    .Select(_ => data.Algorithm.SelectArm(data.Arms))
                    .GroupBy(x => x)
                    .ToDictionary(g => g.Key, g => g.Count()))
            .Then("good arm is selected more often", selections =>
                selections.GetValueOrDefault(1, 0) > selections.GetValueOrDefault(0, 0))
            .AssertPassed();

    [Scenario("Single arm always selected")]
    [Fact]
    public Task Single_arm_always_selected()
        => Given("an algorithm with a single arm", () =>
            {
                var arms = new[] { new ArmStatistics { Key = "arm-0", Successes = 5, Failures = 5 } };
                return (Algorithm: new ThompsonSampling(), Arms: arms);
            })
            .When("selecting 100 times", data =>
                Enumerable.Range(0, 100)
                    .Select(_ => data.Algorithm.SelectArm(data.Arms))
                    .ToList())
            .Then("arm 0 is always selected", selections =>
                selections.All(s => s == 0))
            .AssertPassed();

    [Scenario("New arms with no history get explored")]
    [Fact]
    public Task New_arms_get_explored()
        => Given("arms where one has no history", () =>
            {
                var arms = new[]
                {
                    new ArmStatistics { Key = "experienced-arm", Successes = 100, Failures = 100 },
                    new ArmStatistics { Key = "new-arm", Successes = 0, Failures = 0 }
                };
                return (Algorithm: new ThompsonSampling(seed: 42), Arms: arms);
            })
            .When("selecting 100 times", data =>
                Enumerable.Range(0, 100)
                    .Select(_ => data.Algorithm.SelectArm(data.Arms))
                    .ToList())
            .Then("new arm is selected at least once", selections =>
                selections.Contains(1))
            .AssertPassed();
}
