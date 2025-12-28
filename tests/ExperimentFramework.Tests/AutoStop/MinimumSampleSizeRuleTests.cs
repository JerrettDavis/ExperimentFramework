using ExperimentFramework.AutoStop;
using ExperimentFramework.AutoStop.Rules;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.AutoStop;

[Feature("Minimum sample size rule ensures adequate sample before stopping")]
public sealed class MinimumSampleSizeRuleTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Rule has correct name")]
    [Fact]
    public Task Rule_has_correct_name()
        => Given("a minimum sample size rule", () => new MinimumSampleSizeRule())
            .Then("name is MinimumSampleSize", rule => rule.Name == "MinimumSampleSize")
            .AssertPassed();

    [Scenario("Not met when any variant below threshold")]
    [Fact]
    public Task Not_met_when_below_threshold()
        => Given("a rule with minimum 1000 samples", () => new MinimumSampleSizeRule(1000))
            .When("evaluating with variants below threshold", rule =>
            {
                var data = CreateExperimentData(
                    ("control", 1000, 100, true),
                    ("treatment", 500, 50, false));
                return rule.Evaluate(data);
            })
            .Then("should not stop", decision => !decision.ShouldStop)
            .And("reason mentions minimum not reached", decision =>
                decision.Reason != null && decision.Reason.Contains("not reached"))
            .AssertPassed();

    [Scenario("Met when all variants at threshold")]
    [Fact]
    public Task Met_when_all_at_threshold()
        => Given("a rule with minimum 1000 samples", () => new MinimumSampleSizeRule(1000))
            .When("evaluating with all variants at threshold", rule =>
            {
                var data = CreateExperimentData(
                    ("control", 1000, 100, true),
                    ("treatment", 1000, 150, false));
                return rule.Evaluate(data);
            })
            .Then("should stop", decision => decision.ShouldStop)
            .And("reason confirms reached", decision =>
                decision.Reason != null && decision.Reason.Contains("reached"))
            .AssertPassed();

    [Scenario("Met when all variants above threshold")]
    [Fact]
    public Task Met_when_all_above_threshold()
        => Given("a rule with minimum 1000 samples", () => new MinimumSampleSizeRule(1000))
            .When("evaluating with all variants above threshold", rule =>
            {
                var data = CreateExperimentData(
                    ("control", 5000, 500, true),
                    ("treatment", 5000, 750, false));
                return rule.Evaluate(data);
            })
            .Then("should stop", decision => decision.ShouldStop)
            .AssertPassed();

    [Scenario("Works with single variant")]
    [Fact]
    public Task Works_with_single_variant()
        => Given("a rule with minimum 100 samples", () => new MinimumSampleSizeRule(100))
            .When("evaluating single variant below threshold", rule =>
            {
                var data = CreateExperimentData(("control", 50, 10, true));
                return rule.Evaluate(data);
            })
            .Then("should not stop", decision => !decision.ShouldStop)
            .AssertPassed();

    [Scenario("Reports current minimum sample count")]
    [Fact]
    public Task Reports_current_minimum()
        => Given("a rule with minimum 1000 samples", () => new MinimumSampleSizeRule(1000))
            .When("evaluating with various sample sizes", rule =>
            {
                var data = CreateExperimentData(
                    ("control", 800, 80, true),
                    ("treatment-a", 500, 50, false),
                    ("treatment-b", 1200, 120, false));
                return rule.Evaluate(data);
            })
            .Then("reason includes minimum current count", decision =>
                decision.Reason != null && decision.Reason.Contains("500"))
            .AssertPassed();

    [Scenario("Default minimum is 1000")]
    [Fact]
    public Task Default_minimum_is_1000()
        => Given("a rule with default settings", () => new MinimumSampleSizeRule())
            .When("evaluating with 999 samples", rule =>
            {
                var data = CreateExperimentData(
                    ("control", 999, 100, true),
                    ("treatment", 999, 100, false));
                return rule.Evaluate(data);
            })
            .Then("should not stop", decision => !decision.ShouldStop)
            .And("when at 1000 samples should stop", _ =>
            {
                var rule = new MinimumSampleSizeRule();
                var data = CreateExperimentData(
                    ("control", 1000, 100, true),
                    ("treatment", 1000, 100, false));
                return rule.Evaluate(data).ShouldStop;
            })
            .AssertPassed();

    [Scenario("Zero minimum always passes")]
    [Fact]
    public Task Zero_minimum_always_passes()
        => Given("a rule with minimum 0 samples", () => new MinimumSampleSizeRule(0))
            .When("evaluating with zero samples", rule =>
            {
                var data = CreateExperimentData(
                    ("control", 0, 0, true),
                    ("treatment", 0, 0, false));
                return rule.Evaluate(data);
            })
            .Then("should stop", decision => decision.ShouldStop)
            .AssertPassed();

    private static ExperimentData CreateExperimentData(
        params (string Key, long SampleSize, long Successes, bool IsControl)[] variants)
    {
        return new ExperimentData
        {
            ExperimentName = "test-experiment",
            StartedAt = DateTimeOffset.UtcNow.AddDays(-7),
            Variants = variants.Select(v => new VariantData
            {
                Key = v.Key,
                SampleSize = v.SampleSize,
                Successes = v.Successes,
                IsControl = v.IsControl
            }).ToList()
        };
    }
}
