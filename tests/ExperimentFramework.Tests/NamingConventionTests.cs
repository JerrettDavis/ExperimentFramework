using ExperimentFramework.Naming;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests;

[Feature("Naming conventions control how feature flags and config keys are resolved")]
public sealed class NamingConventionTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record TestState(
        Type ServiceType,
        IExperimentNamingConvention Convention);

    private sealed record NamingResult(
        TestState State,
        string FeatureFlagName,
        string VariantFlagName,
        string ConfigurationKey);

    private static TestState CreateState(IExperimentNamingConvention convention)
        => new(typeof(IMyTestService), convention);

    private static NamingResult ApplyConvention(TestState state)
        => new(
            state,
            state.Convention.FeatureFlagNameFor(state.ServiceType),
            state.Convention.VariantFlagNameFor(state.ServiceType),
            state.Convention.ConfigurationKeyFor(state.ServiceType));

    [Scenario("Default naming convention matches service type name for feature flags")]
    [Fact]
    public Task Default_convention_uses_service_type_name()
        => Given("default naming convention", () => CreateState(new DefaultExperimentNamingConvention()))
            .When("apply convention", ApplyConvention)
            .Then("feature flag name is service type name", r => r.FeatureFlagName == "IMyTestService")
            .And("variant flag name is service type name", r => r.VariantFlagName == "IMyTestService")
            .And("configuration key includes Experiments prefix", r => r.ConfigurationKey == "Experiments:IMyTestService")
            .AssertPassed();

    [Scenario("Custom naming convention can override all name patterns")]
    [Fact]
    public Task Custom_convention_applies_custom_patterns()
        => Given("custom naming convention", () => CreateState(new CustomTestNamingConvention()))
            .When("apply convention", ApplyConvention)
            .Then("feature flag uses custom pattern", r => r.FeatureFlagName == "Features.IMyTestService")
            .And("variant flag uses custom pattern", r => r.VariantFlagName == "Variants.IMyTestService")
            .And("configuration key uses custom pattern", r => r.ConfigurationKey == "CustomExperiments:IMyTestService")
            .AssertPassed();

    [Scenario("Naming convention handles nested and generic types")]
    [Fact]
    public Task Convention_handles_complex_types()
        => Given("default convention with generic type", () => CreateState(new DefaultExperimentNamingConvention()) with { ServiceType = typeof(IGenericService<string>) })
            .When("apply convention", ApplyConvention)
            .Then("feature flag name includes generic type syntax", r => r.FeatureFlagName.Contains("IGenericService"))
            .And("configuration key is valid", r => !string.IsNullOrWhiteSpace(r.ConfigurationKey))
            .AssertPassed();

    // Test interfaces and classes
    private interface IMyTestService { }
    private interface IGenericService<T> { }

    private sealed class CustomTestNamingConvention : IExperimentNamingConvention
    {
        public string FeatureFlagNameFor(Type serviceType)
            => $"Features.{serviceType.Name}";

        public string VariantFlagNameFor(Type serviceType)
            => $"Variants.{serviceType.Name}";

        public string ConfigurationKeyFor(Type serviceType)
            => $"CustomExperiments:{serviceType.Name}";
    }
}
