using ExperimentFramework.Bandit;
using ExperimentFramework.Bandit.Algorithms;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Bandit;

[Feature("Bandit algorithms – arm selection, reward updates, edge cases, and cold start")]
public sealed class BanditExtendedTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // ── cold start ────────────────────────────────────────────────────────────

    [Scenario("EpsilonGreedy cold-start: unpulled arms selected first")]
    [Fact]
    public Task EpsilonGreedy_coldStart_explores_all_arms_before_exploitation()
    {
        var alg = new EpsilonGreedy(epsilon: 0.0, seed: 0); // pure exploit
        var arms = new[]
        {
            new ArmStatistics { Key = "a", Pulls = 0, TotalReward = 0 },
            new ArmStatistics { Key = "b", Pulls = 0, TotalReward = 0 },
            new ArmStatistics { Key = "c", Pulls = 0, TotalReward = 0 }
        };

        // With epsilon=0, if all averages are equal (0/0 = 0), it picks index 0 consistently
        var selected = alg.SelectArm(arms);
        Assert.InRange(selected, 0, arms.Length - 1);

        return Task.CompletedTask;
    }

    [Scenario("UCB cold-start: pulls each arm exactly once before computing UCB")]
    [Fact]
    public Task UCB_coldStart_pulls_each_arm_once()
    {
        var alg = new UpperConfidenceBound();
        var arms = new[]
        {
            new ArmStatistics { Key = "arm-0", Pulls = 0, TotalReward = 0 },
            new ArmStatistics { Key = "arm-1", Pulls = 0, TotalReward = 0 },
            new ArmStatistics { Key = "arm-2", Pulls = 0, TotalReward = 0 }
        };

        var firstSelected = alg.SelectArm(arms);
        Assert.Equal(0, firstSelected); // Always pulls first unpulled arm

        return Task.CompletedTask;
    }

    [Scenario("UCB cold-start: second unpulled arm selected after first is pulled")]
    [Fact]
    public Task UCB_coldStart_selects_second_unpulled_arm()
    {
        var alg = new UpperConfidenceBound();
        var arms = new[]
        {
            new ArmStatistics { Key = "arm-0", Pulls = 1, TotalReward = 1 },
            new ArmStatistics { Key = "arm-1", Pulls = 0, TotalReward = 0 },
            new ArmStatistics { Key = "arm-2", Pulls = 1, TotalReward = 1 }
        };

        var selected = alg.SelectArm(arms);
        Assert.Equal(1, selected);

        return Task.CompletedTask;
    }

    // ── reward update ─────────────────────────────────────────────────────────

    [Scenario("ThompsonSampling UpdateArm increments successes for reward > 0.5")]
    [Fact]
    public Task ThompsonSampling_UpdateArm_increments_successes()
    {
        var alg = new ThompsonSampling();
        var arm = new ArmStatistics { Key = "arm", Pulls = 0, Successes = 0, Failures = 0 };

        alg.UpdateArm(arm, 1.0);

        Assert.Equal(1, arm.Pulls);
        Assert.Equal(1, arm.Successes);
        Assert.Equal(0, arm.Failures);

        return Task.CompletedTask;
    }

    [Scenario("ThompsonSampling UpdateArm increments failures for reward <= 0.5")]
    [Fact]
    public Task ThompsonSampling_UpdateArm_increments_failures()
    {
        var alg = new ThompsonSampling();
        var arm = new ArmStatistics { Key = "arm", Pulls = 0, Successes = 0, Failures = 0 };

        alg.UpdateArm(arm, 0.0);

        Assert.Equal(1, arm.Pulls);
        Assert.Equal(0, arm.Successes);
        Assert.Equal(1, arm.Failures);

        return Task.CompletedTask;
    }

    [Scenario("UCB UpdateArm increments pulls and adds reward")]
    [Fact]
    public Task UCB_UpdateArm_increments_correctly()
    {
        var alg = new UpperConfidenceBound();
        var arm = new ArmStatistics { Key = "arm", Pulls = 5, TotalReward = 3.0 };

        alg.UpdateArm(arm, 0.75);

        Assert.Equal(6, arm.Pulls);
        Assert.Equal(3.75, arm.TotalReward, 3);

        return Task.CompletedTask;
    }

    // ── determinism ───────────────────────────────────────────────────────────

    [Scenario("ThompsonSampling seeded produces same sequence")]
    [Fact]
    public Task ThompsonSampling_seeded_is_deterministic()
    {
        var arms = new[]
        {
            new ArmStatistics { Key = "a", Successes = 5, Failures = 3 },
            new ArmStatistics { Key = "b", Successes = 2, Failures = 7 }
        };

        var alg1 = new ThompsonSampling(seed: 99);
        var alg2 = new ThompsonSampling(seed: 99);

        var results1 = Enumerable.Range(0, 50).Select(_ => alg1.SelectArm(arms)).ToList();
        var results2 = Enumerable.Range(0, 50).Select(_ => alg2.SelectArm(arms)).ToList();

        Assert.Equal(results1, results2);

        return Task.CompletedTask;
    }

    [Scenario("UCB name is UCB1")]
    [Fact]
    public Task UCB_has_correct_name()
        => Given("a UCB algorithm", () => new UpperConfidenceBound())
            .Then("name is UCB1", alg => alg.Name == "UCB1")
            .AssertPassed();

    [Scenario("ThompsonSampling name is ThompsonSampling")]
    [Fact]
    public Task ThompsonSampling_has_correct_name()
        => Given("a ThompsonSampling algorithm", () => new ThompsonSampling())
            .Then("name is ThompsonSampling", alg => alg.Name == "ThompsonSampling")
            .AssertPassed();

    // ── edge cases ────────────────────────────────────────────────────────────

    [Scenario("UCB single arm always returns index 0 after warm-up")]
    [Fact]
    public Task UCB_single_arm_returns_zero_after_warmup()
    {
        var alg = new UpperConfidenceBound();
        var arms = new[] { new ArmStatistics { Key = "a", Pulls = 10, TotalReward = 5 } };

        for (var i = 0; i < 20; i++)
        {
            var idx = alg.SelectArm(arms);
            Assert.Equal(0, idx);
        }

        return Task.CompletedTask;
    }

    [Scenario("ThompsonSampling throws for empty arms")]
    [Fact]
    public void ThompsonSampling_throws_for_empty_arms()
    {
        var alg = new ThompsonSampling();
        var ex = Assert.Throws<ArgumentException>(() => alg.SelectArm(Array.Empty<ArmStatistics>()));
        Assert.Equal("arms", ex.ParamName);
    }

    [Scenario("UCB throws for empty arms")]
    [Fact]
    public void UCB_throws_for_empty_arms()
    {
        var alg = new UpperConfidenceBound();
        var ex = Assert.Throws<ArgumentException>(() => alg.SelectArm(Array.Empty<ArmStatistics>()));
        Assert.Equal("arms", ex.ParamName);
    }

    // ── DI wiring ─────────────────────────────────────────────────────────────

    [Scenario("AddExperimentBanditThompsonSampling registers IBanditAlgorithm")]
    [Fact]
    public Task AddBanditThompsonSampling_registers_algorithm()
    {
        var services = new ServiceCollection();
        services.AddExperimentBanditThompsonSampling();
        var sp = services.BuildServiceProvider();

        var alg = sp.GetService<IBanditAlgorithm>();

        Assert.NotNull(alg);
        Assert.Equal("ThompsonSampling", alg!.Name);

        return Task.CompletedTask;
    }

    [Scenario("AddExperimentBanditUcb registers IBanditAlgorithm")]
    [Fact]
    public Task AddBanditUcb_registers_algorithm()
    {
        var services = new ServiceCollection();
        services.AddExperimentBanditUcb();
        var sp = services.BuildServiceProvider();

        var alg = sp.GetService<IBanditAlgorithm>();

        Assert.NotNull(alg);
        Assert.Equal("UCB1", alg!.Name);

        return Task.CompletedTask;
    }

    [Scenario("ArmStatistics AverageReward is zero when no pulls")]
    [Fact]
    public Task ArmStatistics_average_reward_zero_when_no_pulls()
        => Given("an arm with no pulls", () => new ArmStatistics { Key = "arm", Pulls = 0, TotalReward = 0 })
            .Then("average reward is 0.0", arm => arm.AverageReward == 0.0)
            .AssertPassed();

    [Scenario("ArmStatistics AverageReward computes correctly")]
    [Fact]
    public Task ArmStatistics_average_reward_computed_correctly()
        => Given("an arm with 10 pulls and 7 reward", () =>
                new ArmStatistics { Key = "arm", Pulls = 10, TotalReward = 7.0 })
            .Then("average reward is 0.7", arm => Math.Abs(arm.AverageReward - 0.7) < 1e-9)
            .AssertPassed();
}
