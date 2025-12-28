using ExperimentFramework.Bandit;
using ExperimentFramework.Bandit.Algorithms;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Bandit;

[Feature("Upper Confidence Bound algorithm balances exploration and exploitation")]
public sealed class UpperConfidenceBoundTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Algorithm has correct name")]
    [Fact]
    public Task Algorithm_has_correct_name()
        => Given("a UCB1 algorithm", () => new UpperConfidenceBound())
            .Then("name is UCB1", alg => alg.Name == "UCB1")
            .AssertPassed();

    [Scenario("Throws for empty arms")]
    [Fact]
    public void Throws_for_empty_arms()
    {
        var algorithm = new UpperConfidenceBound();
        var exception = Assert.Throws<ArgumentException>(() =>
            algorithm.SelectArm(Array.Empty<ArmStatistics>()));

        Assert.Equal("arms", exception.ParamName);
    }

    [Scenario("Unpulled arms are selected first")]
    [Fact]
    public Task Unpulled_arms_selected_first()
        => Given("arms where some have no pulls", () =>
            {
                var arms = new[]
                {
                    new ArmStatistics { Key = "arm-0", Pulls = 10, TotalReward = 8.0 },
                    new ArmStatistics { Key = "arm-1", Pulls = 0, TotalReward = 0 },
                    new ArmStatistics { Key = "arm-2", Pulls = 5, TotalReward = 3.0 }
                };
                return (Algorithm: new UpperConfidenceBound(), Arms: arms);
            })
            .When("selecting an arm", data => data.Algorithm.SelectArm(data.Arms))
            .Then("unpulled arm is selected", selection => selection == 1)
            .AssertPassed();

    [Scenario("First unpulled arm is selected when multiple unpulled")]
    [Fact]
    public Task First_unpulled_arm_selected()
        => Given("multiple unpulled arms", () =>
            {
                var arms = new[]
                {
                    new ArmStatistics { Key = "arm-0", Pulls = 10, TotalReward = 8.0 },
                    new ArmStatistics { Key = "arm-1", Pulls = 0, TotalReward = 0 },
                    new ArmStatistics { Key = "arm-2", Pulls = 0, TotalReward = 0 }
                };
                return (Algorithm: new UpperConfidenceBound(), Arms: arms);
            })
            .When("selecting an arm", data => data.Algorithm.SelectArm(data.Arms))
            .Then("first unpulled arm is selected", selection => selection == 1)
            .AssertPassed();

    [Scenario("All arms pulled at least once before exploitation")]
    [Fact]
    public Task All_arms_explored_first()
        => Given("all unpulled arms", () =>
            {
                var arms = new[]
                {
                    new ArmStatistics { Key = "arm-0", Pulls = 0, TotalReward = 0 },
                    new ArmStatistics { Key = "arm-1", Pulls = 0, TotalReward = 0 },
                    new ArmStatistics { Key = "arm-2", Pulls = 0, TotalReward = 0 }
                };
                return (Algorithm: new UpperConfidenceBound(), Arms: arms);
            })
            .When("selecting 3 times sequentially", data =>
            {
                var selections = new List<int>();
                for (var i = 0; i < 3; i++)
                {
                    var selected = data.Algorithm.SelectArm(data.Arms);
                    selections.Add(selected);
                    data.Arms[selected].Pulls = 1;
                }
                return selections;
            })
            .Then("all arms are selected once", selections =>
                selections.Distinct().Count() == 3 && selections.All(s => s is >= 0 and < 3))
            .AssertPassed();

    [Scenario("Best arm selected after exploration")]
    [Fact]
    public Task Best_arm_selected_after_exploration()
        => Given("arms with different rewards all pulled", () =>
            {
                var arms = new[]
                {
                    new ArmStatistics { Key = "arm-0", Pulls = 100, TotalReward = 20.0 },  // 0.2 avg
                    new ArmStatistics { Key = "arm-1", Pulls = 100, TotalReward = 80.0 },  // 0.8 avg (best)
                    new ArmStatistics { Key = "arm-2", Pulls = 100, TotalReward = 50.0 }   // 0.5 avg
                };
                return (Algorithm: new UpperConfidenceBound(), Arms: arms);
            })
            .When("selecting 100 times", data =>
                Enumerable.Range(0, 100)
                    .Select(_ => data.Algorithm.SelectArm(data.Arms))
                    .ToList())
            .Then("best arm is selected most often", selections =>
                selections.GroupBy(x => x)
                    .OrderByDescending(g => g.Count())
                    .First().Key == 1)
            .AssertPassed();

    [Scenario("UpdateArm increments statistics")]
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
                var algorithm = new UpperConfidenceBound();
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
                var arms = new[] { new ArmStatistics { Key = "arm-0", Pulls = 1, TotalReward = 0.5 } };
                return (Algorithm: new UpperConfidenceBound(), Arms: arms);
            })
            .When("selecting 100 times", data =>
                Enumerable.Range(0, 100)
                    .Select(_ => data.Algorithm.SelectArm(data.Arms))
                    .ToList())
            .Then("arm 0 is always selected", selections =>
                selections.All(s => s == 0))
            .AssertPassed();

    [Scenario("Higher exploration parameter encourages more exploration")]
    [Fact]
    public Task Higher_exploration_explores_more()
        => Given("two algorithms with different exploration parameters", () =>
            {
                var arms = new[]
                {
                    new ArmStatistics { Key = "arm-0", Pulls = 100, TotalReward = 80.0 },  // 0.8 avg
                    new ArmStatistics { Key = "arm-1", Pulls = 10, TotalReward = 7.0 }     // 0.7 avg, less certain
                };
                var lowExploration = new UpperConfidenceBound(explorationParameter: 0.5);
                var highExploration = new UpperConfidenceBound(explorationParameter: 3.0);
                return (LowExp: lowExploration, HighExp: highExploration, Arms: arms);
            })
            .When("selecting 100 times from each", data =>
            {
                var lowSelections = Enumerable.Range(0, 100)
                    .Select(_ => data.LowExp.SelectArm(data.Arms))
                    .Count(s => s == 1);
                var highSelections = Enumerable.Range(0, 100)
                    .Select(_ => data.HighExp.SelectArm(data.Arms))
                    .Count(s => s == 1);
                return (LowExplorations: lowSelections, HighExplorations: highSelections);
            })
            .Then("high exploration selects uncertain arm more often", results =>
                results.HighExplorations >= results.LowExplorations)
            .AssertPassed();
}
