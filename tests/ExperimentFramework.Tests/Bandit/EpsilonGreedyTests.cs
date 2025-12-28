using ExperimentFramework.Bandit;
using ExperimentFramework.Bandit.Algorithms;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Bandit;

[Feature("Epsilon-Greedy bandit algorithm balances exploration and exploitation")]
public sealed class EpsilonGreedyTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Algorithm has correct name")]
    [Fact]
    public Task Algorithm_has_correct_name()
        => Given("an Epsilon-Greedy algorithm", () => new EpsilonGreedy())
            .Then("name is EpsilonGreedy", alg => alg.Name == "EpsilonGreedy")
            .AssertPassed();

    [Scenario("Throws for empty arms")]
    [Fact]
    public void Throws_for_empty_arms()
    {
        var algorithm = new EpsilonGreedy();
        var exception = Assert.Throws<ArgumentException>(() =>
            algorithm.SelectArm(Array.Empty<ArmStatistics>()));

        Assert.Equal("arms", exception.ParamName);
    }

    [Scenario("Throws for invalid epsilon below 0")]
    [Fact]
    public void Throws_for_epsilon_below_zero()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EpsilonGreedy(epsilon: -0.1));

        Assert.Equal("epsilon", exception.ParamName);
    }

    [Scenario("Throws for invalid epsilon above 1")]
    [Fact]
    public void Throws_for_epsilon_above_one()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EpsilonGreedy(epsilon: 1.1));

        Assert.Equal("epsilon", exception.ParamName);
    }

    [Scenario("With epsilon=0 always selects best arm")]
    [Fact]
    public Task Zero_epsilon_always_exploits()
        => Given("an algorithm with epsilon=0 and arms with different rewards", () =>
            {
                var arms = new[]
                {
                    new ArmStatistics { Key = "arm-0", Pulls = 10, TotalReward = 2.0 },  // 0.2 avg
                    new ArmStatistics { Key = "arm-1", Pulls = 10, TotalReward = 8.0 },  // 0.8 avg (best)
                    new ArmStatistics { Key = "arm-2", Pulls = 10, TotalReward = 5.0 }   // 0.5 avg
                };
                return (Algorithm: new EpsilonGreedy(epsilon: 0), Arms: arms);
            })
            .When("selecting 100 times", data =>
                Enumerable.Range(0, 100)
                    .Select(_ => data.Algorithm.SelectArm(data.Arms))
                    .ToList())
            .Then("all selections are the best arm", selections =>
                selections.All(s => s == 1))
            .AssertPassed();

    [Scenario("With epsilon=1 always explores randomly")]
    [Fact]
    public Task Full_epsilon_always_explores()
        => Given("an algorithm with epsilon=1 and 3 arms", () =>
            {
                var arms = new[]
                {
                    new ArmStatistics { Key = "arm-0", Pulls = 10, TotalReward = 2.0 },
                    new ArmStatistics { Key = "arm-1", Pulls = 10, TotalReward = 8.0 },
                    new ArmStatistics { Key = "arm-2", Pulls = 10, TotalReward = 5.0 }
                };
                return (Algorithm: new EpsilonGreedy(epsilon: 1, seed: 42), Arms: arms);
            })
            .When("selecting 1000 times", data =>
                Enumerable.Range(0, 1000)
                    .Select(_ => data.Algorithm.SelectArm(data.Arms))
                    .GroupBy(x => x)
                    .ToDictionary(g => g.Key, g => g.Count()))
            .Then("all arms are selected", selections =>
                selections.Count == 3 && selections.Values.All(c => c > 200))
            .AssertPassed();

    [Scenario("UpdateArm increments pulls and rewards")]
    [Fact]
    public Task UpdateArm_updates_statistics()
        => Given("an arm with initial statistics", () => new ArmStatistics
            {
                Key = "test-arm",
                Pulls = 10,
                TotalReward = 5.0
            })
            .When("updating with a reward", arm =>
            {
                var algorithm = new EpsilonGreedy();
                algorithm.UpdateArm(arm, 0.8);
                return arm;
            })
            .Then("pulls is incremented", arm => arm.Pulls == 11)
            .And("reward is added", arm => Math.Abs(arm.TotalReward - 5.8) < 0.001)
            .AssertPassed();

    [Scenario("Single arm always selected")]
    [Fact]
    public Task Single_arm_always_selected()
        => Given("an algorithm with a single arm", () =>
            {
                var arms = new[] { new ArmStatistics { Key = "arm-0", Pulls = 0, TotalReward = 0 } };
                return (Algorithm: new EpsilonGreedy(), Arms: arms);
            })
            .When("selecting 100 times", data =>
                Enumerable.Range(0, 100)
                    .Select(_ => data.Algorithm.SelectArm(data.Arms))
                    .ToList())
            .Then("arm 0 is always selected", selections =>
                selections.All(s => s == 0))
            .AssertPassed();

    [Scenario("Seeded algorithm produces deterministic results")]
    [Fact]
    public Task Seeded_algorithm_is_deterministic()
        => Given("two algorithms with same seed", () =>
            {
                var arms = new[]
                {
                    new ArmStatistics { Key = "arm-0", Pulls = 10, TotalReward = 5.0 },
                    new ArmStatistics { Key = "arm-1", Pulls = 10, TotalReward = 5.0 }
                };
                var alg1 = new EpsilonGreedy(epsilon: 0.5, seed: 42);
                var alg2 = new EpsilonGreedy(epsilon: 0.5, seed: 42);
                return (Alg1: alg1, Alg2: alg2, Arms: arms);
            })
            .When("selecting 100 times from each", data =>
            {
                var results1 = Enumerable.Range(0, 100).Select(_ => data.Alg1.SelectArm(data.Arms)).ToList();
                var results2 = Enumerable.Range(0, 100).Select(_ => data.Alg2.SelectArm(data.Arms)).ToList();
                return (Results1: results1, Results2: results2);
            })
            .Then("results are identical", results =>
                results.Results1.SequenceEqual(results.Results2))
            .AssertPassed();
}
