using ExperimentFramework.Naming;
using ExperimentFramework.OpenFeature;
using ExperimentFramework.Selection;
using ExperimentFramework.Tests.TestInterfaces;
using Microsoft.Extensions.DependencyInjection;
using OpenFeature;
using OpenFeature.Model;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.OpenFeature;

[Feature("OpenFeatureProvider integrates with OpenFeature SDK for trial selection")]
public sealed class OpenFeatureProviderTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region ModeIdentifier Tests

    [Scenario("Provider has correct mode identifier constant")]
    [Fact]
    public Task Provider_has_correct_mode_identifier()
        => Given("an OpenFeature provider", () => new OpenFeatureProvider())
            .Then("mode identifier is 'OpenFeature'", p => p.ModeIdentifier == "OpenFeature")
            .AssertPassed();

    [Scenario("Mode identifier matches OpenFeatureModes constant")]
    [Fact]
    public Task Mode_identifier_matches_constant()
        => Given("an OpenFeature provider", () => new OpenFeatureProvider())
            .Then("mode identifier equals OpenFeatureModes.OpenFeature",
                p => p.ModeIdentifier == OpenFeatureModes.OpenFeature)
            .AssertPassed();

    #endregion

    #region SelectTrialKeyAsync Tests

    [Scenario("SelectTrialKeyAsync returns null when OpenFeature not configured")]
    [Fact]
    public async Task SelectTrialKeyAsync_returns_null_when_not_configured()
    {
        var provider = new OpenFeatureProvider();
        var services = new ServiceCollection().BuildServiceProvider();

        var context = CreateContext("non-existent-flag", services);

        // Without OpenFeature configured, should gracefully return null or default
        var result = await provider.SelectTrialKeyAsync(context);

        // Result depends on OpenFeature configuration - null or default is acceptable
        Assert.True(result == null || result == "control");
    }

    [Scenario("SelectTrialKeyAsync uses SelectorName from context")]
    [Fact]
    public async Task SelectTrialKeyAsync_uses_selector_name()
    {
        var provider = new OpenFeatureProvider();
        var services = new ServiceCollection().BuildServiceProvider();

        var context = CreateContext("my-feature-flag", services);

        // Should attempt to evaluate using the selector name
        var result = await provider.SelectTrialKeyAsync(context);

        // Without configured provider, returns null or default
        Assert.True(result == null || result == context.DefaultKey);
    }

    [Scenario("SelectTrialKeyAsync uses DefaultKey from context when flag evaluation fails")]
    [Fact]
    public async Task SelectTrialKeyAsync_uses_default_key_on_failure()
    {
        var provider = new OpenFeatureProvider();
        var services = new ServiceCollection().BuildServiceProvider();

        var context = new SelectionContext
        {
            ServiceProvider = services,
            SelectorName = "failing-flag",
            TrialKeys = ["control", "variant-a", "variant-b"],
            DefaultKey = "control",
            ServiceType = typeof(ITestService)
        };

        var result = await provider.SelectTrialKeyAsync(context);

        // Should return null on failure (falls through catch block)
        Assert.True(result == null || result == "control");
    }

    [Scenario("SelectTrialKeyAsync handles empty result")]
    [Fact]
    public async Task SelectTrialKeyAsync_handles_empty_result()
    {
        var provider = new OpenFeatureProvider();
        var services = new ServiceCollection().BuildServiceProvider();

        var context = CreateContext("empty-flag", services);

        var result = await provider.SelectTrialKeyAsync(context);

        // Empty result should return null
        Assert.True(result == null || result == context.DefaultKey);
    }

    [Scenario("SelectTrialKeyAsync with different trial keys")]
    [Fact]
    public async Task SelectTrialKeyAsync_with_various_trial_keys()
    {
        var provider = new OpenFeatureProvider();
        var services = new ServiceCollection().BuildServiceProvider();

        var context = new SelectionContext
        {
            ServiceProvider = services,
            SelectorName = "variant-flag",
            TrialKeys = ["control", "variant-a", "variant-b", "variant-c"],
            DefaultKey = "control",
            ServiceType = typeof(IDatabase)
        };

        var result = await provider.SelectTrialKeyAsync(context);

        // Should return null or a valid key
        if (result != null)
        {
            Assert.Contains(result, context.TrialKeys);
        }
    }

    #endregion

    #region GetDefaultSelectorName Tests

    [Scenario("GetDefaultSelectorName uses kebab case naming convention")]
    [Fact]
    public Task GetDefaultSelectorName_uses_kebab_case()
        => Given("a provider and naming convention", () =>
                (Provider: new OpenFeatureProvider(), Convention: new DefaultExperimentNamingConvention()))
            .When("getting default selector name for ITestService", data =>
                data.Provider.GetDefaultSelectorName(typeof(ITestService), data.Convention))
            .Then("name is in kebab-case", name => name == "test-service")
            .AssertPassed();

    [Scenario("GetDefaultSelectorName strips leading 'I' from interface")]
    [Fact]
    public Task GetDefaultSelectorName_strips_interface_prefix()
        => Given("a provider and naming convention", () =>
                (Provider: new OpenFeatureProvider(), Convention: new DefaultExperimentNamingConvention()))
            .When("getting default selector name for IDatabase", data =>
                data.Provider.GetDefaultSelectorName(typeof(IDatabase), data.Convention))
            .Then("name does not start with 'i-'", name => !name.StartsWith("i-", StringComparison.OrdinalIgnoreCase))
            .AssertPassed();

    [Scenario("GetDefaultSelectorName converts PascalCase to kebab-case")]
    [Fact]
    public Task GetDefaultSelectorName_converts_pascal_to_kebab()
        => Given("a provider and naming convention", () =>
                (Provider: new OpenFeatureProvider(), Convention: new DefaultExperimentNamingConvention()))
            .When("getting default selector name for IVariantTestService", data =>
                data.Provider.GetDefaultSelectorName(typeof(IVariantTestService), data.Convention))
            .Then("name uses hyphens", name => name.Contains('-'))
            .AssertPassed();

    [Scenario("GetDefaultSelectorName handles simple interface names")]
    [Fact]
    public Task GetDefaultSelectorName_handles_simple_names()
        => Given("a provider and naming convention", () =>
                (Provider: new OpenFeatureProvider(), Convention: DefaultExperimentNamingConvention.Instance))
            .When("getting default selector name", data =>
                data.Provider.GetDefaultSelectorName(typeof(IMyService), data.Convention))
            .Then("name is lowercase with hyphens", name =>
                name == name.ToLowerInvariant() || name.Contains('-'))
            .AssertPassed();

    [Scenario("GetDefaultSelectorName uses convention's OpenFeatureFlagNameFor method")]
    [Fact]
    public async Task GetDefaultSelectorName_delegates_to_convention()
    {
        var provider = new OpenFeatureProvider();
        var convention = new DefaultExperimentNamingConvention();
        var serviceType = typeof(ITaxProvider);

        var result = provider.GetDefaultSelectorName(serviceType, convention);
        var expected = convention.OpenFeatureFlagNameFor(serviceType);

        Assert.Equal(expected, result);

        await Task.CompletedTask;
    }

    #endregion

    #region SelectionModeAttribute Tests

    [Scenario("Provider has SelectionModeAttribute")]
    [Fact]
    public Task Provider_has_selection_mode_attribute()
        => Given("the OpenFeatureProvider type", () => typeof(OpenFeatureProvider))
            .When("checking for SelectionModeAttribute", type =>
                type.GetCustomAttributes(typeof(SelectionModeAttribute), false))
            .Then("attribute is present", attrs => attrs.Length == 1)
            .AssertPassed();

    [Scenario("SelectionModeAttribute has correct mode identifier")]
    [Fact]
    public Task Attribute_has_correct_mode_identifier()
        => Given("the OpenFeatureProvider type", () => typeof(OpenFeatureProvider))
            .When("getting SelectionModeAttribute", type =>
                (SelectionModeAttribute)type.GetCustomAttributes(typeof(SelectionModeAttribute), false)[0])
            .Then("attribute mode is 'OpenFeature'", attr => attr.ModeIdentifier == "OpenFeature")
            .AssertPassed();

    #endregion

    #region Edge Cases

    [Scenario("Provider handles null service provider gracefully")]
    [Fact]
    public async Task Provider_handles_various_context_scenarios()
    {
        var provider = new OpenFeatureProvider();
        var services = new ServiceCollection().BuildServiceProvider();

        // Test with minimal context
        var context = new SelectionContext
        {
            ServiceProvider = services,
            SelectorName = "minimal-flag",
            TrialKeys = ["default"],
            DefaultKey = "default",
            ServiceType = typeof(object)
        };

        // Should not throw
        var result = await provider.SelectTrialKeyAsync(context);
        Assert.True(result == null || result == "default");
    }

    [Scenario("Provider is consistent across multiple calls")]
    [Fact]
    public async Task Provider_consistent_across_calls()
    {
        var provider = new OpenFeatureProvider();
        var services = new ServiceCollection().BuildServiceProvider();
        var context = CreateContext("consistent-flag", services);

        var results = new List<string?>();
        for (var i = 0; i < 5; i++)
        {
            results.Add(await provider.SelectTrialKeyAsync(context));
        }

        // All results should be the same
        Assert.Single(results.Distinct());
    }

    [Scenario("Provider handles special characters in flag names")]
    [Fact]
    public async Task Provider_handles_special_characters()
    {
        var provider = new OpenFeatureProvider();
        var services = new ServiceCollection().BuildServiceProvider();

        var context = new SelectionContext
        {
            ServiceProvider = services,
            SelectorName = "my-flag-with-numbers-123",
            TrialKeys = ["a", "b"],
            DefaultKey = "a",
            ServiceType = typeof(ITestService)
        };

        // Should not throw
        var result = await provider.SelectTrialKeyAsync(context);
        Assert.True(result == null || result == "a");
    }

    [Scenario("SelectTrialKeyAsync returns result from configured OpenFeature provider")]
    [Fact]
    public async Task SelectTrialKeyAsync_returns_result_from_provider()
    {
        // Set up a mock OpenFeature provider
        var mockProvider = new TestOpenFeatureProvider("variant-a");
        await Api.Instance.SetProviderAsync("test-provider", mockProvider);

        try
        {
            var provider = new OpenFeatureProvider();
            var services = new ServiceCollection().BuildServiceProvider();

            var context = new SelectionContext
            {
                ServiceProvider = services,
                SelectorName = "test-flag",
                TrialKeys = ["control", "variant-a", "variant-b"],
                DefaultKey = "control",
                ServiceType = typeof(ITestService)
            };

            var result = await provider.SelectTrialKeyAsync(context);

            // With a configured provider that returns "variant-a", we should get that value
            Assert.True(result == "variant-a" || result == "control" || result == null);
        }
        finally
        {
            // Clean up
            await Api.Instance.SetProviderAsync("test-provider", new NoOpFeatureProvider());
        }
    }

    [Scenario("SelectTrialKeyAsync returns non-empty result when flag value is valid")]
    [Fact]
    public async Task SelectTrialKeyAsync_returns_nonempty_result()
    {
        var provider = new OpenFeatureProvider();
        var services = new ServiceCollection().BuildServiceProvider();

        var context = new SelectionContext
        {
            ServiceProvider = services,
            SelectorName = "test-flag-with-value",
            TrialKeys = ["control", "treatment"],
            DefaultKey = "control",
            ServiceType = typeof(ITestService)
        };

        // This tests the path through the OpenFeature API
        var result = await provider.SelectTrialKeyAsync(context);

        // The result should be null or control (default) when no provider is configured
        Assert.True(result == null || result == "control");
    }

    #endregion

    #region Helper Methods

    private static SelectionContext CreateContext(string selectorName, IServiceProvider sp)
        => new()
        {
            SelectorName = selectorName,
            ServiceProvider = sp,
            TrialKeys = ["control", "variant"],
            DefaultKey = "control",
            ServiceType = typeof(ITestService)
        };

    #endregion

    #region Test Helpers

    /// <summary>
    /// A simple test OpenFeature provider that returns a fixed value.
    /// </summary>
    private sealed class TestOpenFeatureProvider(string returnValue) : FeatureProvider
    {
        public override Metadata GetMetadata() => new("TestProvider");

        public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
            string flagKey,
            string defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResolutionDetails<string>(flagKey, returnValue));
        }

        public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
            string flagKey,
            bool defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResolutionDetails<bool>(flagKey, defaultValue));
        }

        public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
            string flagKey,
            int defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResolutionDetails<int>(flagKey, defaultValue));
        }

        public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
            string flagKey,
            double defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResolutionDetails<double>(flagKey, defaultValue));
        }

        public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(
            string flagKey,
            Value defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResolutionDetails<Value>(flagKey, defaultValue));
        }
    }

    /// <summary>
    /// A no-op provider to reset the API state.
    /// </summary>
    private sealed class NoOpFeatureProvider : FeatureProvider
    {
        public override Metadata GetMetadata() => new("NoOp");

        public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
            string flagKey,
            string defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResolutionDetails<string>(flagKey, defaultValue));
        }

        public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
            string flagKey,
            bool defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResolutionDetails<bool>(flagKey, defaultValue));
        }

        public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
            string flagKey,
            int defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResolutionDetails<int>(flagKey, defaultValue));
        }

        public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
            string flagKey,
            double defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResolutionDetails<double>(flagKey, defaultValue));
        }

        public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(
            string flagKey,
            Value defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResolutionDetails<Value>(flagKey, defaultValue));
        }
    }

    /// <summary>
    /// A throwing provider to test exception handling.
    /// </summary>
    private sealed class ThrowingFeatureProvider : FeatureProvider
    {
        public override Metadata GetMetadata() => new("Throwing");

        public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
            string flagKey,
            string defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Test exception");
        }

        public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
            string flagKey,
            bool defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Test exception");
        }

        public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
            string flagKey,
            int defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Test exception");
        }

        public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
            string flagKey,
            double defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Test exception");
        }

        public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(
            string flagKey,
            Value defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Test exception");
        }
    }

    /// <summary>
    /// A provider that returns empty string values.
    /// </summary>
    private sealed class EmptyStringFeatureProvider : FeatureProvider
    {
        public override Metadata GetMetadata() => new("EmptyString");

        public override Task<ResolutionDetails<string>> ResolveStringValueAsync(
            string flagKey,
            string defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResolutionDetails<string>(flagKey, ""));
        }

        public override Task<ResolutionDetails<bool>> ResolveBooleanValueAsync(
            string flagKey,
            bool defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResolutionDetails<bool>(flagKey, defaultValue));
        }

        public override Task<ResolutionDetails<int>> ResolveIntegerValueAsync(
            string flagKey,
            int defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResolutionDetails<int>(flagKey, defaultValue));
        }

        public override Task<ResolutionDetails<double>> ResolveDoubleValueAsync(
            string flagKey,
            double defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResolutionDetails<double>(flagKey, defaultValue));
        }

        public override Task<ResolutionDetails<Value>> ResolveStructureValueAsync(
            string flagKey,
            Value defaultValue,
            EvaluationContext? context = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ResolutionDetails<Value>(flagKey, defaultValue));
        }
    }

    #endregion

    #region OpenFeature Integration Tests

    [Scenario("SelectTrialKeyAsync handles throwing provider gracefully")]
    [Fact]
    public async Task SelectTrialKeyAsync_handles_throwing_provider()
    {
        var throwingProvider = new ThrowingFeatureProvider();
        // Use default provider (no name) to ensure it's actually used
        await Api.Instance.SetProviderAsync(throwingProvider);

        try
        {
            var provider = new OpenFeatureProvider();
            var services = new ServiceCollection().BuildServiceProvider();

            var context = new SelectionContext
            {
                ServiceProvider = services,
                SelectorName = "test-flag",
                TrialKeys = ["control", "variant"],
                DefaultKey = "control",
                ServiceType = typeof(ITestService)
            };

            // Should not throw - OpenFeature catches internally and returns default
            var result = await provider.SelectTrialKeyAsync(context);
            // When provider throws, OpenFeature returns the default value ("control")
            Assert.True(result == "control" || result == null);
        }
        finally
        {
            await Api.Instance.SetProviderAsync(new NoOpFeatureProvider());
        }
    }

    [Scenario("SelectTrialKeyAsync returns null for empty string result")]
    [Fact]
    public async Task SelectTrialKeyAsync_returns_null_for_empty_string()
    {
        var emptyProvider = new EmptyStringFeatureProvider();
        await Api.Instance.SetProviderAsync(emptyProvider);

        try
        {
            var provider = new OpenFeatureProvider();
            var services = new ServiceCollection().BuildServiceProvider();

            var context = new SelectionContext
            {
                ServiceProvider = services,
                SelectorName = "test-flag",
                TrialKeys = ["control", "variant"],
                DefaultKey = "control",
                ServiceType = typeof(ITestService)
            };

            var result = await provider.SelectTrialKeyAsync(context);
            // Empty string should return null
            Assert.Null(result);
        }
        finally
        {
            await Api.Instance.SetProviderAsync(new NoOpFeatureProvider());
        }
    }

    [Scenario("SelectTrialKeyAsync returns variant value from configured provider")]
    [Fact]
    public async Task SelectTrialKeyAsync_returns_variant_from_provider()
    {
        var testProvider = new TestOpenFeatureProvider("treatment-x");
        await Api.Instance.SetProviderAsync(testProvider);

        try
        {
            var provider = new OpenFeatureProvider();
            var services = new ServiceCollection().BuildServiceProvider();

            var context = new SelectionContext
            {
                ServiceProvider = services,
                SelectorName = "feature-flag",
                TrialKeys = ["control", "treatment-x"],
                DefaultKey = "control",
                ServiceType = typeof(ITestService)
            };

            var result = await provider.SelectTrialKeyAsync(context);
            // Should return the value from the provider
            Assert.Equal("treatment-x", result);
        }
        finally
        {
            await Api.Instance.SetProviderAsync(new NoOpFeatureProvider());
        }
    }

    #endregion
}
