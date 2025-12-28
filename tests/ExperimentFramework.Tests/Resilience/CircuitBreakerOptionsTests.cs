using ExperimentFramework.Resilience;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Resilience;

[Feature("CircuitBreakerOptions provides circuit breaker configuration")]
public sealed class CircuitBreakerOptionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Default options are set correctly")]
    [Fact]
    public Task Default_options_are_set()
        => Given("default circuit breaker options", () => new CircuitBreakerOptions())
            .Then("failure threshold is 5", o => o.FailureThreshold == 5)
            .And("minimum throughput is 10", o => o.MinimumThroughput == 10)
            .And("sampling duration is 10 seconds", o => o.SamplingDuration == TimeSpan.FromSeconds(10))
            .And("break duration is 30 seconds", o => o.BreakDuration == TimeSpan.FromSeconds(30))
            .And("failure ratio threshold is null", o => o.FailureRatioThreshold == null)
            .And("on circuit open is ThrowException", o => o.OnCircuitOpen == CircuitBreakerAction.ThrowException)
            .And("fallback trial key is null", o => o.FallbackTrialKey == null)
            .AssertPassed();

    [Scenario("Options can be modified")]
    [Fact]
    public Task Options_can_be_modified()
        => Given("circuit breaker options", () => new CircuitBreakerOptions())
            .When("modifying all properties", o =>
            {
                o.FailureThreshold = 10;
                o.MinimumThroughput = 20;
                o.SamplingDuration = TimeSpan.FromSeconds(30);
                o.BreakDuration = TimeSpan.FromMinutes(1);
                o.FailureRatioThreshold = 0.75;
                o.OnCircuitOpen = CircuitBreakerAction.FallbackToSpecificTrial;
                o.FallbackTrialKey = "fallback-trial";
                return o;
            })
            .Then("failure threshold is updated", o => o.FailureThreshold == 10)
            .And("minimum throughput is updated", o => o.MinimumThroughput == 20)
            .And("sampling duration is updated", o => o.SamplingDuration == TimeSpan.FromSeconds(30))
            .And("break duration is updated", o => o.BreakDuration == TimeSpan.FromMinutes(1))
            .And("failure ratio threshold is updated", o => Math.Abs(o.FailureRatioThreshold!.Value - 0.75) < 0.001)
            .And("on circuit open is updated", o => o.OnCircuitOpen == CircuitBreakerAction.FallbackToSpecificTrial)
            .And("fallback trial key is updated", o => o.FallbackTrialKey == "fallback-trial")
            .AssertPassed();

    [Scenario("CircuitBreakerAction enum has correct values")]
    [Fact]
    public Task CircuitBreakerAction_enum_values()
        => Given("circuit breaker action enum", () => true)
            .Then("ThrowException is defined", _ => Enum.IsDefined(typeof(CircuitBreakerAction), CircuitBreakerAction.ThrowException))
            .And("FallbackToDefault is defined", _ => Enum.IsDefined(typeof(CircuitBreakerAction), CircuitBreakerAction.FallbackToDefault))
            .And("FallbackToSpecificTrial is defined", _ => Enum.IsDefined(typeof(CircuitBreakerAction), CircuitBreakerAction.FallbackToSpecificTrial))
            .AssertPassed();
}
