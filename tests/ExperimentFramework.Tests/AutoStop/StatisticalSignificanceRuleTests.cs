using ExperimentFramework.AutoStop;
using ExperimentFramework.AutoStop.Rules;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.AutoStop;

[Feature("Statistical significance rule determines when results are conclusive")]
public sealed class StatisticalSignificanceRuleTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Rule has correct name")]
    [Fact]
    public Task Rule_has_correct_name()
        => Given("a statistical significance rule", () => new StatisticalSignificanceRule())
            .Then("name is StatisticalSignificance", rule => rule.Name == "StatisticalSignificance")
            .AssertPassed();

    [Scenario("Not significant with single variant")]
    [Fact]
    public Task Not_significant_with_single_variant()
        => Given("a significance rule", () => new StatisticalSignificanceRule())
            .When("evaluating with single variant", rule =>
            {
                var data = CreateExperimentData(("control", 1000, 100, true));
                return rule.Evaluate(data);
            })
            .Then("should not stop", decision => !decision.ShouldStop)
            .And("reason mentions need 2 variants", decision =>
                decision.Reason != null && decision.Reason.Contains("2 variants"))
            .AssertPassed();

    [Scenario("Not significant with insufficient samples")]
    [Fact]
    public Task Not_significant_with_insufficient_samples()
        => Given("a rule with minimum 100 samples", () => new StatisticalSignificanceRule(0.95, 100))
            .When("evaluating with samples below minimum", rule =>
            {
                var data = CreateExperimentData(
                    ("control", 50, 5, true),
                    ("treatment", 50, 25, false));
                return rule.Evaluate(data);
            })
            .Then("should not stop", decision => !decision.ShouldStop)
            .And("reason mentions minimum not reached", decision =>
                decision.Reason != null && decision.Reason.Contains("Minimum sample size"))
            .AssertPassed();

    [Scenario("Significant with large effect size")]
    [Fact]
    public Task Significant_with_large_effect()
        => Given("a significance rule at 95% confidence", () => new StatisticalSignificanceRule(0.95, 100))
            .When("evaluating with very different conversion rates", rule =>
            {
                var data = CreateExperimentData(
                    ("control", 10000, 1000, true),   // 10% conversion
                    ("treatment", 10000, 2000, false)); // 20% conversion
                return rule.Evaluate(data);
            })
            .Then("should stop", decision => decision.ShouldStop)
            .And("identifies winner", decision => decision.WinningVariant != null)
            .And("has high confidence", decision => decision.Confidence is > 0.95)
            .AssertPassed();

    [Scenario("Not significant with similar conversion rates")]
    [Fact]
    public Task Not_significant_with_similar_rates()
        => Given("a significance rule", () => new StatisticalSignificanceRule(0.95, 100))
            .When("evaluating with similar conversion rates", rule =>
            {
                var data = CreateExperimentData(
                    ("control", 1000, 100, true),     // 10% conversion
                    ("treatment", 1000, 102, false)); // 10.2% conversion
                return rule.Evaluate(data);
            })
            .Then("should not stop", decision => !decision.ShouldStop)
            .AssertPassed();

    [Scenario("Winner is treatment when it outperforms control")]
    [Fact]
    public Task Winner_is_treatment_when_better()
        => Given("a significance rule", () => new StatisticalSignificanceRule(0.95, 100))
            .When("treatment significantly outperforms control", rule =>
            {
                var data = CreateExperimentData(
                    ("control", 10000, 1000, true),   // 10% conversion
                    ("treatment", 10000, 2000, false)); // 20% conversion
                return rule.Evaluate(data);
            })
            .Then("winner is treatment", decision => decision.WinningVariant == "treatment")
            .AssertPassed();

    [Scenario("Winner is control when it outperforms treatment")]
    [Fact]
    public Task Winner_is_control_when_better()
        => Given("a significance rule", () => new StatisticalSignificanceRule(0.95, 100))
            .When("control significantly outperforms treatment", rule =>
            {
                var data = CreateExperimentData(
                    ("control", 10000, 2000, true),   // 20% conversion
                    ("treatment", 10000, 1000, false)); // 10% conversion
                return rule.Evaluate(data);
            })
            .Then("winner is control", decision => decision.WinningVariant == "control")
            .AssertPassed();

    [Scenario("First variant used as control when none marked")]
    [Fact]
    public Task First_variant_as_control_fallback()
        => Given("a significance rule", () => new StatisticalSignificanceRule(0.95, 100))
            .When("evaluating with no variant marked as control", rule =>
            {
                var data = new ExperimentData
                {
                    ExperimentName = "test",
                    StartedAt = DateTimeOffset.UtcNow,
                    Variants = new List<VariantData>
                    {
                        new() { Key = "variant-a", SampleSize = 10000, Successes = 1000, IsControl = false },
                        new() { Key = "variant-b", SampleSize = 10000, Successes = 2000, IsControl = false }
                    }
                };
                return rule.Evaluate(data);
            })
            .Then("still produces valid result", decision =>
                decision.ShouldStop && decision.WinningVariant != null)
            .AssertPassed();

    [Scenario("Multiple treatment variants finds best performer")]
    [Fact]
    public Task Multiple_treatments_finds_best()
        => Given("a significance rule", () => new StatisticalSignificanceRule(0.95, 100))
            .When("evaluating multiple treatment variants", rule =>
            {
                var data = CreateExperimentData(
                    ("control", 10000, 1000, true),      // 10%
                    ("treatment-a", 10000, 1500, false), // 15%
                    ("treatment-b", 10000, 2500, false), // 25% (best)
                    ("treatment-c", 10000, 500, false)); // 5%
                return rule.Evaluate(data);
            })
            .Then("identifies best treatment as winner", decision =>
                decision.WinningVariant == "treatment-b")
            .AssertPassed();

    [Scenario("No treatment variants returns not found")]
    [Fact]
    public Task No_treatment_variants_returns_not_found()
        => Given("a significance rule", () => new StatisticalSignificanceRule())
            .When("evaluating with only control variants", rule =>
            {
                var data = new ExperimentData
                {
                    ExperimentName = "test",
                    StartedAt = DateTimeOffset.UtcNow,
                    Variants = new List<VariantData>
                    {
                        new() { Key = "control-a", SampleSize = 1000, Successes = 100, IsControl = true },
                        new() { Key = "control-b", SampleSize = 1000, Successes = 200, IsControl = true }
                    }
                };
                return rule.Evaluate(data);
            })
            .Then("should not stop", decision => !decision.ShouldStop)
            .And("reason mentions no treatment", decision =>
                decision.Reason != null && decision.Reason.Contains("treatment"))
            .AssertPassed();

    [Scenario("P-value included in reason")]
    [Fact]
    public Task PValue_included_in_reason()
        => Given("a significance rule", () => new StatisticalSignificanceRule(0.95, 100))
            .When("evaluating an experiment", rule =>
            {
                var data = CreateExperimentData(
                    ("control", 10000, 1000, true),
                    ("treatment", 10000, 1500, false));
                return rule.Evaluate(data);
            })
            .Then("reason includes p-value", decision =>
                decision.Reason != null && decision.Reason.Contains("p="))
            .AssertPassed();

    [Scenario("Higher confidence level requires more data")]
    [Fact]
    public Task Higher_confidence_requires_more_data()
        => Given("two rules with different confidence levels", () =>
            {
                var data = CreateExperimentData(
                    ("control", 1000, 100, true),   // 10%
                    ("treatment", 1000, 130, false)); // 13%
                return (
                    Rule95: new StatisticalSignificanceRule(0.95, 100),
                    Rule99: new StatisticalSignificanceRule(0.99, 100),
                    Data: data);
            })
            .When("evaluating with same data", data =>
                (Decision95: data.Rule95.Evaluate(data.Data),
                 Decision99: data.Rule99.Evaluate(data.Data)))
            .Then("99% rule requires more evidence than 95%", decisions =>
                !decisions.Decision99.ShouldStop || decisions.Decision95.ShouldStop)
            .AssertPassed();

    [Scenario("Handles zero standard error case")]
    [Fact]
    public Task Handles_zero_standard_error()
        => Given("a significance rule", () => new StatisticalSignificanceRule(0.95, 100))
            .When("evaluating with identical conversion rates and very large samples", rule =>
            {
                // Create a case where pooled variance is zero or very small
                var data = new ExperimentData
                {
                    ExperimentName = "test",
                    StartedAt = DateTimeOffset.UtcNow,
                    Variants = new List<VariantData>
                    {
                        new() { Key = "control", SampleSize = 10000, Successes = 0, IsControl = true },
                        new() { Key = "treatment", SampleSize = 10000, Successes = 0, IsControl = false }
                    }
                };
                return rule.Evaluate(data);
            })
            .Then("should not throw and not stop", decision => !decision.ShouldStop)
            .AssertPassed();

    [Scenario("Handles very small standard error edge case")]
    [Fact]
    public Task Handles_small_standard_error()
        => Given("a significance rule", () => new StatisticalSignificanceRule(0.95, 100))
            .When("evaluating with same rates for control and treatment", rule =>
            {
                // Same conversion rates should result in near-zero z-score
                var data = CreateExperimentData(
                    ("control", 10000, 5000, true),    // 50%
                    ("treatment", 10000, 5000, false)); // 50%
                return rule.Evaluate(data);
            })
            .Then("should not stop", decision => !decision.ShouldStop)
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
