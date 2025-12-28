using ExperimentFramework.FeatureManagement;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.FeatureManagement;

[Feature("VariantFeatureFlagModes defines well-known mode identifiers")]
public sealed class VariantFeatureFlagModesTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("VariantFeatureFlag constant has correct value")]
    [Fact]
    public Task VariantFeatureFlag_constant_has_correct_value()
        => Given("the VariantFeatureFlagModes class", () => typeof(VariantFeatureFlagModes))
            .Then("VariantFeatureFlag constant equals 'VariantFeatureFlag'",
                _ => VariantFeatureFlagModes.VariantFeatureFlag == "VariantFeatureFlag")
            .AssertPassed();

    [Scenario("VariantFeatureFlag constant is not null")]
    [Fact]
    public Task VariantFeatureFlag_constant_is_not_null()
        => Given("the VariantFeatureFlagModes class", () => typeof(VariantFeatureFlagModes))
            .Then("VariantFeatureFlag constant is not null",
                _ => VariantFeatureFlagModes.VariantFeatureFlag != null)
            .AssertPassed();

    [Scenario("VariantFeatureFlag constant is not empty")]
    [Fact]
    public Task VariantFeatureFlag_constant_is_not_empty()
        => Given("the VariantFeatureFlagModes class", () => typeof(VariantFeatureFlagModes))
            .Then("VariantFeatureFlag constant is not empty",
                _ => !string.IsNullOrEmpty(VariantFeatureFlagModes.VariantFeatureFlag))
            .AssertPassed();

    [Scenario("VariantFeatureFlag constant matches provider ModeIdentifier")]
    [Fact]
    public Task VariantFeatureFlag_matches_provider_mode_identifier()
        => Given("a VariantFeatureFlagProvider", () => new VariantFeatureFlagProvider())
            .Then("provider's ModeIdentifier matches the constant",
                provider => provider.ModeIdentifier == VariantFeatureFlagModes.VariantFeatureFlag)
            .AssertPassed();

    [Scenario("VariantFeatureFlagModes class is static")]
    [Fact]
    public void VariantFeatureFlagModes_class_is_static()
    {
        var type = typeof(VariantFeatureFlagModes);

        Assert.True(type.IsAbstract);
        Assert.True(type.IsSealed);
    }

    [Scenario("VariantFeatureFlag constant is accessible")]
    [Fact]
    public void VariantFeatureFlag_constant_is_accessible()
    {
        var fieldInfo = typeof(VariantFeatureFlagModes).GetField(nameof(VariantFeatureFlagModes.VariantFeatureFlag));

        Assert.NotNull(fieldInfo);
        Assert.True(fieldInfo.IsPublic);
        Assert.True(fieldInfo.IsStatic);
        Assert.True(fieldInfo.IsLiteral);
    }
}
