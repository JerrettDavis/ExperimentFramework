using ExperimentFramework.AutoStop;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.AutoStop;

[Feature("StoppingDecision provides experiment stopping recommendations")]
public sealed class StoppingDecisionTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("StoppingDecision with basic properties")]
    [Fact]
    public Task StoppingDecision_basic_properties()
        => Given("a stopping decision", () => new StoppingDecision(true, "Test passed"))
            .Then("should stop is set", d => d.ShouldStop)
            .And("reason is set", d => d.Reason == "Test passed")
            .And("winning variant is null", d => d.WinningVariant == null)
            .And("confidence is null", d => d.Confidence == null)
            .AssertPassed();

    [Scenario("StoppingDecision with winner")]
    [Fact]
    public Task StoppingDecision_with_winner()
        => Given("a stopping decision with winner", () =>
                new StoppingDecision(true, "Significant result", "variant-a", 0.98))
            .Then("should stop", d => d.ShouldStop)
            .And("has reason", d => d.Reason == "Significant result")
            .And("has winning variant", d => d.WinningVariant == "variant-a")
            .And("has confidence", d => Math.Abs(d.Confidence!.Value - 0.98) < 0.001)
            .AssertPassed();

    [Scenario("StoppingDecision that says don't stop")]
    [Fact]
    public Task StoppingDecision_dont_stop()
        => Given("a continue decision", () =>
                new StoppingDecision(false, "More data needed"))
            .Then("should not stop", d => !d.ShouldStop)
            .And("has reason", d => d.Reason == "More data needed")
            .AssertPassed();

    [Scenario("StoppingDecision is a record struct")]
    [Fact]
    public Task StoppingDecision_is_value_type()
        => Given("two identical decisions", () =>
                (D1: new StoppingDecision(true, "Test", "winner", 0.95),
                 D2: new StoppingDecision(true, "Test", "winner", 0.95)))
            .Then("they are equal", x => x.D1.Equals(x.D2))
            .AssertPassed();

    [Scenario("StoppingDecision with null reason")]
    [Fact]
    public Task StoppingDecision_null_reason()
        => Given("a decision with null reason", () => new StoppingDecision(false, null))
            .Then("reason is null", d => d.Reason == null)
            .AssertPassed();
}
