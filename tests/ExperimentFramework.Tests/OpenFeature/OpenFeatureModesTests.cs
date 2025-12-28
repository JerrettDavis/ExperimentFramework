using ExperimentFramework.OpenFeature;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.OpenFeature;

[Feature("OpenFeatureModes provides well-known mode identifiers")]
public sealed class OpenFeatureModesTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("OpenFeature constant has correct value")]
    [Fact]
    public Task OpenFeature_constant_has_correct_value()
        => Given("the OpenFeatureModes class", () => typeof(OpenFeatureModes))
            .Then("OpenFeature constant is 'OpenFeature'",
                _ => OpenFeatureModes.OpenFeature == "OpenFeature")
            .AssertPassed();

    [Scenario("OpenFeature constant is not null or empty")]
    [Fact]
    public Task OpenFeature_constant_is_not_empty()
        => Given("the OpenFeature constant", () => OpenFeatureModes.OpenFeature)
            .Then("it is not null or empty", value => !string.IsNullOrEmpty(value))
            .AssertPassed();

    [Scenario("OpenFeature constant can be used as mode identifier")]
    [Fact]
    public Task OpenFeature_constant_usable_as_identifier()
        => Given("the OpenFeature constant", () => OpenFeatureModes.OpenFeature)
            .When("using it as mode identifier", mode =>
            {
                var provider = new OpenFeatureProvider();
                return provider.ModeIdentifier == mode;
            })
            .Then("provider uses the same identifier", matches => matches)
            .AssertPassed();

    [Scenario("OpenFeatureModes is a static class")]
    [Fact]
    public Task OpenFeatureModes_is_static()
        => Given("the OpenFeatureModes type", () => typeof(OpenFeatureModes))
            .Then("it is abstract and sealed", type => type.IsAbstract && type.IsSealed)
            .AssertPassed();

    [Scenario("OpenFeature constant is consistent across references")]
    [Fact]
    public Task OpenFeature_constant_is_consistent()
        => Given("multiple references to OpenFeature constant", () =>
            {
                var ref1 = OpenFeatureModes.OpenFeature;
                var ref2 = OpenFeatureModes.OpenFeature;
                var ref3 = OpenFeatureModes.OpenFeature;
                return (ref1, ref2, ref3);
            })
            .Then("all references are equal", refs =>
                refs.ref1 == refs.ref2 && refs.ref2 == refs.ref3)
            .AssertPassed();
}
