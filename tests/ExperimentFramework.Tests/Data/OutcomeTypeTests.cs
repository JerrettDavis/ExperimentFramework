using ExperimentFramework.Data.Models;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Data;

[Feature("OutcomeType enum defines all supported outcome types")]
public sealed class OutcomeTypeTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Binary outcome type exists")]
    [Fact]
    public Task Binary_outcome_type_exists()
        => Given("OutcomeType enum", () => typeof(OutcomeType))
            .Then("Binary is defined", type => Enum.IsDefined(type, OutcomeType.Binary))
            .And("Binary has value 0", _ => (int)OutcomeType.Binary == 0)
            .AssertPassed();

    [Scenario("Continuous outcome type exists")]
    [Fact]
    public Task Continuous_outcome_type_exists()
        => Given("OutcomeType enum", () => typeof(OutcomeType))
            .Then("Continuous is defined", type => Enum.IsDefined(type, OutcomeType.Continuous))
            .And("Continuous has value 1", _ => (int)OutcomeType.Continuous == 1)
            .AssertPassed();

    [Scenario("Count outcome type exists")]
    [Fact]
    public Task Count_outcome_type_exists()
        => Given("OutcomeType enum", () => typeof(OutcomeType))
            .Then("Count is defined", type => Enum.IsDefined(type, OutcomeType.Count))
            .And("Count has value 2", _ => (int)OutcomeType.Count == 2)
            .AssertPassed();

    [Scenario("Duration outcome type exists")]
    [Fact]
    public Task Duration_outcome_type_exists()
        => Given("OutcomeType enum", () => typeof(OutcomeType))
            .Then("Duration is defined", type => Enum.IsDefined(type, OutcomeType.Duration))
            .And("Duration has value 3", _ => (int)OutcomeType.Duration == 3)
            .AssertPassed();

    [Scenario("All four outcome types are defined")]
    [Fact]
    public Task All_outcome_types_defined()
        => Given("all OutcomeType values", () => Enum.GetValues<OutcomeType>())
            .Then("there are exactly 4 types", values => values.Length == 4)
            .AssertPassed();

    [Scenario("Outcome types can be parsed from string")]
    [Fact]
    public Task Outcome_types_parseable_from_string()
        => Given("outcome type names", () => new[] { "Binary", "Continuous", "Count", "Duration" })
            .Then("all can be parsed", names => names.All(name => Enum.TryParse<OutcomeType>(name, out _)))
            .AssertPassed();

    [Scenario("Outcome types can be converted to string")]
    [Fact]
    public Task Outcome_types_convertible_to_string()
        => Given("all outcome types", () => Enum.GetValues<OutcomeType>())
            .Then("Binary converts to 'Binary'", _ => OutcomeType.Binary.ToString() == "Binary")
            .And("Continuous converts to 'Continuous'", _ => OutcomeType.Continuous.ToString() == "Continuous")
            .And("Count converts to 'Count'", _ => OutcomeType.Count.ToString() == "Count")
            .And("Duration converts to 'Duration'", _ => OutcomeType.Duration.ToString() == "Duration")
            .AssertPassed();
}
