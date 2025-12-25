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
        string ConfigurationKey,
        string OpenFeatureFlagName);

    private static TestState CreateState(IExperimentNamingConvention convention)
        => new(typeof(IMyTestService), convention);

    private static NamingResult ApplyConvention(TestState state)
        => new(
            state,
            state.Convention.FeatureFlagNameFor(state.ServiceType),
            state.Convention.VariantFlagNameFor(state.ServiceType),
            state.Convention.ConfigurationKeyFor(state.ServiceType),
            state.Convention.OpenFeatureFlagNameFor(state.ServiceType));

    [Scenario("Default naming convention matches service type name for feature flags")]
    [Fact]
    public Task Default_convention_uses_service_type_name()
        => Given("default naming convention", () => CreateState(new DefaultExperimentNamingConvention()))
            .When("apply convention", ApplyConvention)
            .Then("feature flag name is service type name", r => r.FeatureFlagName == "IMyTestService")
            .And("variant flag name is service type name", r => r.VariantFlagName == "IMyTestService")
            .And("configuration key includes Experiments prefix", r => r.ConfigurationKey == "Experiments:IMyTestService")
            .And("OpenFeature flag uses kebab-case", r => r.OpenFeatureFlagName == "my-test-service")
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

    [Scenario("OpenFeature kebab-case handles acronyms correctly")]
    [Fact]
    public Task OpenFeature_kebab_case_handles_acronyms()
    {
        var convention = new DefaultExperimentNamingConvention();
        return Given("default convention with acronym types", () => convention)
            .Then("IHTTPService becomes http-service", c => c.OpenFeatureFlagNameFor(typeof(IHTTPService)) == "http-service")
            .And("IXMLParser becomes xml-parser", c => c.OpenFeatureFlagNameFor(typeof(IXMLParser)) == "xml-parser")
            .And("IMyHTTPClient becomes my-http-client", c => c.OpenFeatureFlagNameFor(typeof(IMyHTTPClient)) == "my-http-client")
            .And("IParseXML becomes parse-xml", c => c.OpenFeatureFlagNameFor(typeof(IParseXML)) == "parse-xml")
            .And("IAWSService becomes aws-service", c => c.OpenFeatureFlagNameFor(typeof(IAWSService)) == "aws-service")
            .AssertPassed();
    }

    // Test interfaces and classes
    private interface IMyTestService { }
    private interface IHTTPService { }
    private interface IXMLParser { }
    private interface IMyHTTPClient { }
    private interface IParseXML { }
    private interface IAWSService { }
    private interface IGenericService<T> { }

    private sealed class CustomTestNamingConvention : IExperimentNamingConvention
    {
        public string FeatureFlagNameFor(Type serviceType)
            => $"Features.{serviceType.Name}";

        public string VariantFlagNameFor(Type serviceType)
            => $"Variants.{serviceType.Name}";

        public string ConfigurationKeyFor(Type serviceType)
            => $"CustomExperiments:{serviceType.Name}";

        public string OpenFeatureFlagNameFor(Type serviceType)
            => $"openfeature-{serviceType.Name.ToLowerInvariant()}";
    }
}

/// <summary>
/// Tests for ExperimentSelectorName value object.
/// </summary>
public sealed class ExperimentSelectorNameTests
{
    [Fact]
    public void ExperimentSelectorName_implicit_conversion_from_string()
    {
        ExperimentSelectorName name = "my-feature-flag";

        Assert.Equal("my-feature-flag", name.Value);
    }

    [Fact]
    public void ExperimentSelectorName_implicit_conversion_to_string()
    {
        var name = new ExperimentSelectorName("my-feature-flag");

        string value = name;

        Assert.Equal("my-feature-flag", value);
    }

    [Fact]
    public void ExperimentSelectorName_ToString_returns_value()
    {
        var name = new ExperimentSelectorName("test-selector");

        Assert.Equal("test-selector", name.ToString());
    }

    [Fact]
    public void ExperimentSelectorName_equality()
    {
        var name1 = new ExperimentSelectorName("test");
        var name2 = new ExperimentSelectorName("test");
        var name3 = new ExperimentSelectorName("other");

        Assert.Equal(name1, name2);
        Assert.NotEqual(name1, name3);
    }
}
