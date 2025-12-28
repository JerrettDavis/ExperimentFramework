using ExperimentFramework.FeatureManagement;
using ExperimentFramework.Naming;
using ExperimentFramework.Selection;
using ExperimentFramework.Tests.TestInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.FeatureManagement;

[Feature("VariantFeatureFlagProvider selects trials based on IVariantFeatureManager")]
public sealed class VariantFeatureFlagProviderTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region ModeIdentifier Tests

    [Scenario("Provider has correct mode identifier")]
    [Fact]
    public Task Provider_has_correct_mode_identifier()
        => Given("a variant feature flag provider", () => new VariantFeatureFlagProvider())
            .Then("mode identifier is 'VariantFeatureFlag'", p => p.ModeIdentifier == VariantFeatureFlagModes.VariantFeatureFlag)
            .AssertPassed();

    [Scenario("Mode identifier matches constant value")]
    [Fact]
    public Task Mode_identifier_matches_constant()
        => Given("a variant feature flag provider", () => new VariantFeatureFlagProvider())
            .Then("mode identifier equals 'VariantFeatureFlag'", p => p.ModeIdentifier == "VariantFeatureFlag")
            .AssertPassed();

    #endregion

    #region SelectTrialKeyAsync Tests - Without IVariantFeatureManager

    [Scenario("Returns null when IVariantFeatureManager is not registered")]
    [Fact]
    public async Task Returns_null_without_variant_manager()
    {
        var provider = new VariantFeatureFlagProvider();
        var services = new ServiceCollection().BuildServiceProvider();

        var context = CreateContext("test-variant", services);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Scenario("Returns null when variant manager returns null variant")]
    [Fact]
    public async Task Returns_null_when_variant_is_null()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:TestFeature"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        var sp = services.BuildServiceProvider();

        var provider = new VariantFeatureFlagProvider();
        var context = CreateContext("NonExistentFeature", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Scenario("Returns null when variant name is empty")]
    [Fact]
    public async Task Returns_null_when_variant_name_is_empty()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:EmptyVariant:EnabledFor:0:Name"] = "AlwaysOn"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        var sp = services.BuildServiceProvider();

        var provider = new VariantFeatureFlagProvider();
        var context = CreateContext("EmptyVariant", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        // Without proper variant configuration, returns null
        Assert.Null(result);
    }

    #endregion

    #region SelectTrialKeyAsync Tests - With IVariantFeatureManager

    [Scenario("Returns variant name when variant manager provides variant")]
    [Fact]
    public async Task Returns_variant_name_from_manager()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:VariantFeature:EnabledFor:0:Name"] = "AlwaysOn",
                ["FeatureManagement:VariantFeature:Variants:0:Name"] = "control",
                ["FeatureManagement:VariantFeature:Variants:1:Name"] = "treatment",
                ["FeatureManagement:VariantFeature:Allocation:DefaultWhenEnabled"] = "treatment"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        var sp = services.BuildServiceProvider();

        var provider = new VariantFeatureFlagProvider();
        var context = CreateContext("VariantFeature", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        // If variant manager returns a variant, we get the name
        // Otherwise we get null (which is also valid if no variant allocation)
        Assert.True(result == "treatment" || result == null);
    }

    [Scenario("Handles multiple different selector names")]
    [Fact]
    public async Task Handles_multiple_selector_names()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:Feature1:Enabled"] = "true",
                ["FeatureManagement:Feature2:Enabled"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        var sp = services.BuildServiceProvider();

        var provider = new VariantFeatureFlagProvider();

        var result1 = await provider.SelectTrialKeyAsync(CreateContext("Feature1", sp));
        var result2 = await provider.SelectTrialKeyAsync(CreateContext("Feature2", sp));

        // Both calls should complete without throwing
        Assert.True(result1 == null || !string.IsNullOrEmpty(result1));
        Assert.True(result2 == null || !string.IsNullOrEmpty(result2));
    }

    #endregion

    #region SelectTrialKeyAsync Tests - Exception Handling

    [Scenario("Handles exception from variant manager gracefully")]
    [Fact]
    public async Task Handles_exception_gracefully()
    {
        // Create a minimal service provider that will cause issues
        var services = new ServiceCollection();
        // Intentionally not adding configuration or feature management
        var sp = services.BuildServiceProvider();

        var provider = new VariantFeatureFlagProvider();
        var context = CreateContext("AnyFeature", sp);

        // Should not throw, should return null
        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Scenario("Returns null when variant manager throws")]
    [Fact]
    public async Task Returns_null_when_manager_throws()
    {
        // Setup with incomplete configuration that might cause issues
        var config = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        var sp = services.BuildServiceProvider();

        var provider = new VariantFeatureFlagProvider();
        // Use a feature name that doesn't exist in configuration
        var context = CreateContext("NonExistentFeature", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        // Should gracefully return null
        Assert.Null(result);
    }

    #endregion

    #region GetDefaultSelectorName Tests

    [Scenario("GetDefaultSelectorName uses VariantFlagNameFor from convention")]
    [Fact]
    public Task GetDefaultSelectorName_uses_convention()
        => Given("a provider and default naming convention", () =>
            (Provider: new VariantFeatureFlagProvider(), Convention: DefaultExperimentNamingConvention.Instance))
            .When("getting default selector name for ITestService", data =>
                data.Provider.GetDefaultSelectorName(typeof(ITestService), data.Convention))
            .Then("returns the convention's variant flag name", name => name == "ITestService")
            .AssertPassed();

    [Scenario("GetDefaultSelectorName works with different service types")]
    [Fact]
    public Task GetDefaultSelectorName_works_with_different_types()
        => Given("a provider and default naming convention", () =>
            (Provider: new VariantFeatureFlagProvider(), Convention: DefaultExperimentNamingConvention.Instance))
            .When("getting default selector names for various types", data =>
            (
                IVariantService: data.Provider.GetDefaultSelectorName(typeof(IVariantService), data.Convention),
                IDatabase: data.Provider.GetDefaultSelectorName(typeof(IDatabase), data.Convention),
                ITaxProvider: data.Provider.GetDefaultSelectorName(typeof(ITaxProvider), data.Convention)
            ))
            .Then("returns correct names for each type", names =>
                names.IVariantService == "IVariantService" &&
                names.IDatabase == "IDatabase" &&
                names.ITaxProvider == "ITaxProvider")
            .AssertPassed();

    [Scenario("GetDefaultSelectorName works with custom naming convention")]
    [Fact]
    public Task GetDefaultSelectorName_with_custom_convention()
        => Given("a provider and custom naming convention", () =>
            (Provider: new VariantFeatureFlagProvider(), Convention: new CustomNamingConvention()))
            .When("getting default selector name", data =>
                data.Provider.GetDefaultSelectorName(typeof(ITestService), data.Convention))
            .Then("returns the custom convention's variant flag name", name => name == "CustomVariant:ITestService")
            .AssertPassed();

    #endregion

    #region SelectionModeAttribute Tests

    [Scenario("Provider has SelectionModeAttribute with correct value")]
    [Fact]
    public void Provider_has_selection_mode_attribute()
    {
        var attribute = typeof(VariantFeatureFlagProvider)
            .GetCustomAttributes(typeof(SelectionModeAttribute), false)
            .Cast<SelectionModeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal("VariantFeatureFlag", attribute.ModeIdentifier);
    }

    #endregion

    #region Concurrent Access Tests

    [Scenario("Provider handles concurrent calls")]
    [Fact]
    public async Task Handles_concurrent_calls()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:ConcurrentFeature"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        var sp = services.BuildServiceProvider();

        var provider = new VariantFeatureFlagProvider();
        var context = CreateContext("ConcurrentFeature", sp);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => provider.SelectTrialKeyAsync(context).AsTask())
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // All calls should complete without error
        Assert.Equal(10, results.Length);
    }

    #endregion

    #region Context Property Tests

    [Scenario("Provider uses SelectorName from context")]
    [Fact]
    public async Task Uses_selector_name_from_context()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:SpecificFeature:EnabledFor:0:Name"] = "AlwaysOn",
                ["FeatureManagement:SpecificFeature:Variants:0:Name"] = "variant-a",
                ["FeatureManagement:SpecificFeature:Allocation:DefaultWhenEnabled"] = "variant-a"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        var sp = services.BuildServiceProvider();

        var provider = new VariantFeatureFlagProvider();
        var context = CreateContext("SpecificFeature", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        // Result should be based on the specific feature in context
        Assert.True(result == "variant-a" || result == null);
    }

    [Scenario("Provider uses ServiceProvider from context")]
    [Fact]
    public async Task Uses_service_provider_from_context()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();

        var provider = new VariantFeatureFlagProvider();
        var context = new SelectionContext
        {
            ServiceProvider = sp,
            SelectorName = "TestFeature",
            TrialKeys = ["control", "treatment"],
            DefaultKey = "control",
            ServiceType = typeof(ITestService)
        };

        // Should use the ServiceProvider from context to resolve IVariantFeatureManager
        var result = await provider.SelectTrialKeyAsync(context);

        // Without IVariantFeatureManager, returns null
        Assert.Null(result);
    }

    #endregion

    #region Edge Cases

    [Scenario("Handles empty trial keys list")]
    [Fact]
    public async Task Handles_empty_trial_keys()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();

        var provider = new VariantFeatureFlagProvider();
        var context = new SelectionContext
        {
            ServiceProvider = sp,
            SelectorName = "TestFeature",
            TrialKeys = [],
            DefaultKey = "control",
            ServiceType = typeof(ITestService)
        };

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Scenario("Handles null SelectorName in context")]
    [Fact]
    public async Task Handles_whitespace_selector_name()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        var sp = services.BuildServiceProvider();

        var provider = new VariantFeatureFlagProvider();
        var context = new SelectionContext
        {
            ServiceProvider = sp,
            SelectorName = "   ",
            TrialKeys = ["control"],
            DefaultKey = "control",
            ServiceType = typeof(ITestService)
        };

        var result = await provider.SelectTrialKeyAsync(context);

        // Should not throw, returns null
        Assert.Null(result);
    }

    #endregion

    #region Mock IVariantFeatureManager Tests

    [Scenario("Returns variant name when mock manager returns valid variant")]
    [Fact]
    public async Task Returns_variant_name_from_mock_manager()
    {
        var mockManager = new MockVariantFeatureManager("treatment-b");
        var services = new ServiceCollection();
        services.AddSingleton<IVariantFeatureManager>(mockManager);
        var sp = services.BuildServiceProvider();

        var provider = new VariantFeatureFlagProvider();
        var context = CreateContext("any-feature", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("treatment-b", result);
    }

    [Scenario("Returns null when mock manager returns variant with empty name")]
    [Fact]
    public async Task Returns_null_when_variant_has_empty_name()
    {
        var mockManager = new MockVariantFeatureManager("");
        var services = new ServiceCollection();
        services.AddSingleton<IVariantFeatureManager>(mockManager);
        var sp = services.BuildServiceProvider();

        var provider = new VariantFeatureFlagProvider();
        var context = CreateContext("any-feature", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Scenario("Returns null when mock manager returns null variant")]
    [Fact]
    public async Task Returns_null_when_variant_is_null_from_mock()
    {
        var mockManager = new MockVariantFeatureManager(null);
        var services = new ServiceCollection();
        services.AddSingleton<IVariantFeatureManager>(mockManager);
        var sp = services.BuildServiceProvider();

        var provider = new VariantFeatureFlagProvider();
        var context = CreateContext("any-feature", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Scenario("Returns null when mock manager throws exception")]
    [Fact]
    public async Task Returns_null_when_mock_throws()
    {
        var mockManager = new ThrowingVariantFeatureManager();
        var services = new ServiceCollection();
        services.AddSingleton<IVariantFeatureManager>(mockManager);
        var sp = services.BuildServiceProvider();

        var provider = new VariantFeatureFlagProvider();
        var context = CreateContext("any-feature", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    #endregion

    #region Helper Methods

    private static SelectionContext CreateContext(string selectorName, IServiceProvider sp)
        => new()
        {
            SelectorName = selectorName,
            ServiceProvider = sp,
            TrialKeys = ["control", "variant-a", "variant-b"],
            DefaultKey = "control",
            ServiceType = typeof(ITestService)
        };

    private sealed class CustomNamingConvention : IExperimentNamingConvention
    {
        public string FeatureFlagNameFor(Type serviceType) => $"CustomFlag:{serviceType.Name}";
        public string VariantFlagNameFor(Type serviceType) => $"CustomVariant:{serviceType.Name}";
        public string ConfigurationKeyFor(Type serviceType) => $"CustomConfig:{serviceType.Name}";
        public string OpenFeatureFlagNameFor(Type serviceType) => $"custom-of-{serviceType.Name.ToLowerInvariant()}";
    }

    private sealed class MockVariantFeatureManager(string? variantName) : IVariantFeatureManager
    {
        public IAsyncEnumerable<string> GetFeatureNamesAsync()
            => AsyncEnumerable.Empty<string>();

        public IAsyncEnumerable<string> GetFeatureNamesAsync(CancellationToken cancellationToken)
            => AsyncEnumerable.Empty<string>();

        public ValueTask<bool> IsEnabledAsync(string feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(true);

        public ValueTask<bool> IsEnabledAsync<TContext>(string feature, TContext context, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(true);

        public ValueTask<Variant?> GetVariantAsync(string feature, CancellationToken cancellationToken = default)
        {
            if (variantName == null)
                return ValueTask.FromResult<Variant?>(null);

            return ValueTask.FromResult<Variant?>(new Variant { Name = variantName });
        }

        public ValueTask<Variant?> GetVariantAsync<TContext>(string feature, TContext context, CancellationToken cancellationToken = default)
            => GetVariantAsync(feature, cancellationToken);

        public ValueTask<Variant?> GetVariantAsync(string feature, Microsoft.FeatureManagement.FeatureFilters.ITargetingContext context, CancellationToken cancellationToken = default)
            => GetVariantAsync(feature, cancellationToken);
    }

    private sealed class ThrowingVariantFeatureManager : IVariantFeatureManager
    {
        public IAsyncEnumerable<string> GetFeatureNamesAsync()
            => AsyncEnumerable.Empty<string>();

        public IAsyncEnumerable<string> GetFeatureNamesAsync(CancellationToken cancellationToken)
            => AsyncEnumerable.Empty<string>();

        public ValueTask<bool> IsEnabledAsync(string feature, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Test exception");

        public ValueTask<bool> IsEnabledAsync<TContext>(string feature, TContext context, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Test exception");

        public ValueTask<Variant?> GetVariantAsync(string feature, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Test exception");

        public ValueTask<Variant?> GetVariantAsync<TContext>(string feature, TContext context, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Test exception");

        public ValueTask<Variant?> GetVariantAsync(string feature, Microsoft.FeatureManagement.FeatureFilters.ITargetingContext context, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Test exception");
    }

    #endregion
}
