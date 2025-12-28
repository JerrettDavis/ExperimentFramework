using ExperimentFramework.Naming;
using ExperimentFramework.Selection;
using ExperimentFramework.Targeting;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Targeting;

[Feature("TargetingProvider provides mode-based targeting selection")]
public sealed class TargetingProviderTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("ModeIdentifier returns 'Targeting'")]
    [Fact]
    public Task Mode_identifier_is_targeting()
        => Given("a targeting provider", () => new TargetingProvider())
            .Then("mode identifier is 'Targeting'", p => p.ModeIdentifier == "Targeting")
            .AssertPassed();

    [Scenario("TargetingModes.Targeting constant is 'Targeting'")]
    [Fact]
    public Task Targeting_modes_constant()
        => Given("the TargetingModes class", () => TargetingModes.Targeting)
            .Then("Targeting constant is 'Targeting'", value => value == "Targeting")
            .AssertPassed();

    [Scenario("Provider can be created with no parameters")]
    [Fact]
    public Task Provider_created_with_no_parameters()
        => Given("creating a provider with no parameters", () => new TargetingProvider())
            .Then("provider is created", p => p != null)
            .And("has correct mode identifier", p => p.ModeIdentifier == "Targeting")
            .AssertPassed();

    [Scenario("Provider can be created with context provider only")]
    [Fact]
    public Task Provider_created_with_context_provider()
        => Given("creating a provider with context provider", () =>
            {
                var contextProvider = new TestContextProvider(new SimpleTargetingContext("user-1"));
                return new TargetingProvider(contextProvider);
            })
            .Then("provider is created", p => p != null)
            .AssertPassed();

    [Scenario("Provider can be created with options only")]
    [Fact]
    public Task Provider_created_with_options()
        => Given("creating a provider with options", () =>
            {
                var options = new TargetingOptions { MatchedKey = "test" };
                return new TargetingProvider(options: options);
            })
            .Then("provider is created", p => p != null)
            .AssertPassed();

    [Scenario("Provider can be created with both parameters")]
    [Fact]
    public Task Provider_created_with_both_parameters()
        => Given("creating a provider with both parameters", () =>
            {
                var contextProvider = new TestContextProvider(new SimpleTargetingContext("user-1"));
                var options = new TargetingOptions { MatchedKey = "test" };
                return new TargetingProvider(contextProvider, options);
            })
            .Then("provider is created", p => p != null)
            .AssertPassed();

    [Scenario("GetDefaultSelectorName formats correctly")]
    [Fact]
    public Task GetDefaultSelectorName_formats_correctly()
        => Given("a targeting provider", () => new TargetingProvider())
            .When("getting default selector name", p =>
                p.GetDefaultSelectorName(typeof(IDisposable), DefaultExperimentNamingConvention.Instance))
            .Then("name starts with 'Targeting:'", name => name.StartsWith("Targeting:"))
            .AssertPassed();

    [Scenario("GetDefaultSelectorName uses naming convention")]
    [Fact]
    public Task GetDefaultSelectorName_uses_convention()
        => Given("a targeting provider and custom convention", () =>
            {
                var provider = new TargetingProvider();
                var convention = new TestNamingConvention("CustomFlag");
                return (Provider: provider, Convention: convention);
            })
            .When("getting default selector name", data =>
                data.Provider.GetDefaultSelectorName(typeof(IFormattable), data.Convention))
            .Then("name uses convention result", name => name == "Targeting:CustomFlag")
            .AssertPassed();

    [Scenario("SelectTrialKeyAsync returns null when no context provider available")]
    [Fact]
    public async Task SelectTrialKeyAsync_returns_null_no_context_provider()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider();
        var context = CreateSelectionContext("test", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Scenario("SelectTrialKeyAsync returns null when context is null")]
    [Fact]
    public async Task SelectTrialKeyAsync_returns_null_when_context_null()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITargetingContextProvider>(new NullContextProvider());
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider(sp.GetService<ITargetingContextProvider>());
        var context = CreateSelectionContext("test", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Scenario("SelectTrialKeyAsync returns MatchedKey when default rule matches")]
    [Fact]
    public async Task SelectTrialKeyAsync_returns_matched_key()
    {
        var targetingContext = new SimpleTargetingContext("user-1")
            .WithAttribute("premium", true);

        var options = new TargetingOptions
        {
            DefaultRule = TargetingRules.HasAttribute("premium"),
            MatchedKey = "premium-variant",
            UnmatchedKey = "standard-variant"
        };

        var services = new ServiceCollection();
        services.AddSingleton<ITargetingContextProvider>(new TestContextProvider(targetingContext));
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider(
            sp.GetService<ITargetingContextProvider>(),
            options);
        var context = CreateSelectionContext("test", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("premium-variant", result);
    }

    [Scenario("SelectTrialKeyAsync returns UnmatchedKey when default rule does not match")]
    [Fact]
    public async Task SelectTrialKeyAsync_returns_unmatched_key()
    {
        var targetingContext = new SimpleTargetingContext("user-1"); // No premium attribute

        var options = new TargetingOptions
        {
            DefaultRule = TargetingRules.HasAttribute("premium"),
            MatchedKey = "premium-variant",
            UnmatchedKey = "standard-variant"
        };

        var services = new ServiceCollection();
        services.AddSingleton<ITargetingContextProvider>(new TestContextProvider(targetingContext));
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider(
            sp.GetService<ITargetingContextProvider>(),
            options);
        var context = CreateSelectionContext("test", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("standard-variant", result);
    }

    [Scenario("SelectTrialKeyAsync returns null when no default rule and no config rules")]
    [Fact]
    public async Task SelectTrialKeyAsync_returns_null_no_rules()
    {
        var targetingContext = new SimpleTargetingContext("user-1");

        var services = new ServiceCollection();
        services.AddSingleton<ITargetingContextProvider>(new TestContextProvider(targetingContext));
        // No options, no config provider
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider(sp.GetService<ITargetingContextProvider>());
        var context = CreateSelectionContext("test", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Scenario("SelectTrialKeyAsync evaluates configuration rules in order")]
    [Fact]
    public async Task SelectTrialKeyAsync_evaluates_config_rules_in_order()
    {
        var targetingContext = new SimpleTargetingContext("user-1")
            .WithAttribute("plan", "enterprise");

        var configProvider = new TestTargetingConfigProvider(new Dictionary<string, IReadOnlyList<(ITargetingRule, string)>>
        {
            ["test-selector"] = new List<(ITargetingRule, string)>
            {
                (TargetingRules.AttributeEquals("plan", "premium"), "premium-key"),
                (TargetingRules.AttributeEquals("plan", "enterprise"), "enterprise-key"),
                (TargetingRules.Always(), "default-key")
            }
        });

        var services = new ServiceCollection();
        services.AddSingleton<ITargetingContextProvider>(new TestContextProvider(targetingContext));
        services.AddSingleton<ITargetingConfigurationProvider>(configProvider);
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider(sp.GetService<ITargetingContextProvider>());
        var context = CreateSelectionContext("test-selector", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("enterprise-key", result);
    }

    [Scenario("SelectTrialKeyAsync returns first matching config rule")]
    [Fact]
    public async Task SelectTrialKeyAsync_returns_first_match()
    {
        var targetingContext = new SimpleTargetingContext("user-1")
            .WithAttribute("plan", "premium");

        var configProvider = new TestTargetingConfigProvider(new Dictionary<string, IReadOnlyList<(ITargetingRule, string)>>
        {
            ["test-selector"] = new List<(ITargetingRule, string)>
            {
                (TargetingRules.AttributeEquals("plan", "premium"), "first-match"),
                (TargetingRules.Always(), "second-match")
            }
        });

        var services = new ServiceCollection();
        services.AddSingleton<ITargetingContextProvider>(new TestContextProvider(targetingContext));
        services.AddSingleton<ITargetingConfigurationProvider>(configProvider);
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider(sp.GetService<ITargetingContextProvider>());
        var context = CreateSelectionContext("test-selector", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("first-match", result);
    }

    [Scenario("SelectTrialKeyAsync returns null when no config rules match")]
    [Fact]
    public async Task SelectTrialKeyAsync_returns_null_no_match()
    {
        var targetingContext = new SimpleTargetingContext("user-1")
            .WithAttribute("plan", "free");

        var configProvider = new TestTargetingConfigProvider(new Dictionary<string, IReadOnlyList<(ITargetingRule, string)>>
        {
            ["test-selector"] = new List<(ITargetingRule, string)>
            {
                (TargetingRules.AttributeEquals("plan", "premium"), "premium-key"),
                (TargetingRules.AttributeEquals("plan", "enterprise"), "enterprise-key")
            }
        });

        var services = new ServiceCollection();
        services.AddSingleton<ITargetingContextProvider>(new TestContextProvider(targetingContext));
        services.AddSingleton<ITargetingConfigurationProvider>(configProvider);
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider(sp.GetService<ITargetingContextProvider>());
        var context = CreateSelectionContext("test-selector", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Scenario("SelectTrialKeyAsync uses context provider from DI when not injected")]
    [Fact]
    public async Task SelectTrialKeyAsync_uses_di_context_provider()
    {
        var targetingContext = new SimpleTargetingContext("di-user")
            .WithAttribute("source", "di");

        var options = new TargetingOptions
        {
            DefaultRule = TargetingRules.HasAttribute("source"),
            MatchedKey = "from-di"
        };

        var services = new ServiceCollection();
        services.AddSingleton<ITargetingContextProvider>(new TestContextProvider(targetingContext));
        services.AddSingleton(options);
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider(options: options); // No constructor-injected context provider
        var context = CreateSelectionContext("test", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("from-di", result);
    }

    [Scenario("SelectTrialKeyAsync uses options from DI when not injected")]
    [Fact]
    public async Task SelectTrialKeyAsync_uses_di_options()
    {
        var targetingContext = new SimpleTargetingContext("user-1")
            .WithAttribute("test", true);

        var options = new TargetingOptions
        {
            DefaultRule = TargetingRules.HasAttribute("test"),
            MatchedKey = "from-di-options",
            UnmatchedKey = "fallback"
        };

        var services = new ServiceCollection();
        services.AddSingleton<ITargetingContextProvider>(new TestContextProvider(targetingContext));
        services.AddSingleton(options);
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider(sp.GetService<ITargetingContextProvider>());
        var context = CreateSelectionContext("test", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("from-di-options", result);
    }

    [Scenario("Config rules take precedence over default options")]
    [Fact]
    public async Task Config_rules_override_default_options()
    {
        var targetingContext = new SimpleTargetingContext("user-1")
            .WithAttribute("plan", "premium");

        var options = new TargetingOptions
        {
            DefaultRule = TargetingRules.Always(),
            MatchedKey = "from-options"
        };

        var configProvider = new TestTargetingConfigProvider(new Dictionary<string, IReadOnlyList<(ITargetingRule, string)>>
        {
            ["test-selector"] = new List<(ITargetingRule, string)>
            {
                (TargetingRules.AttributeEquals("plan", "premium"), "from-config")
            }
        });

        var services = new ServiceCollection();
        services.AddSingleton<ITargetingContextProvider>(new TestContextProvider(targetingContext));
        services.AddSingleton<ITargetingConfigurationProvider>(configProvider);
        services.AddSingleton(options);
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider(
            sp.GetService<ITargetingContextProvider>(),
            options);
        var context = CreateSelectionContext("test-selector", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("from-config", result);
    }

    [Scenario("Empty config rules list falls back to default options")]
    [Fact]
    public async Task Empty_config_rules_uses_default_options()
    {
        var targetingContext = new SimpleTargetingContext("user-1")
            .WithAttribute("active", true);

        var options = new TargetingOptions
        {
            DefaultRule = TargetingRules.HasAttribute("active"),
            MatchedKey = "from-options"
        };

        var configProvider = new TestTargetingConfigProvider(new Dictionary<string, IReadOnlyList<(ITargetingRule, string)>>
        {
            ["test-selector"] = new List<(ITargetingRule, string)>() // Empty list
        });

        var services = new ServiceCollection();
        services.AddSingleton<ITargetingContextProvider>(new TestContextProvider(targetingContext));
        services.AddSingleton<ITargetingConfigurationProvider>(configProvider);
        services.AddSingleton(options);
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider(
            sp.GetService<ITargetingContextProvider>(),
            options);
        var context = CreateSelectionContext("test-selector", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("from-options", result);
    }

    private static SelectionContext CreateSelectionContext(string selectorName, IServiceProvider sp)
        => new()
        {
            SelectorName = selectorName,
            ServiceProvider = sp,
            TrialKeys = ["control", "treatment"],
            DefaultKey = "control",
            ServiceType = typeof(IFormattable)
        };

    private sealed class NullContextProvider : ITargetingContextProvider
    {
        public ITargetingContext? GetContext() => null;
        public ValueTask<ITargetingContext?> GetContextAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult<ITargetingContext?>(null);
    }

    private sealed class TestContextProvider(ITargetingContext context) : ITargetingContextProvider
    {
        public ITargetingContext? GetContext() => context;
        public ValueTask<ITargetingContext?> GetContextAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult<ITargetingContext?>(context);
    }

    private sealed class TestTargetingConfigProvider(
        Dictionary<string, IReadOnlyList<(ITargetingRule, string)>> rules) : ITargetingConfigurationProvider
    {
        public IReadOnlyList<(ITargetingRule Rule, string Key)>? GetRulesFor(string selectorName)
            => rules.TryGetValue(selectorName, out var r) ? r : null;
    }

    private sealed class TestNamingConvention(string flagName) : IExperimentNamingConvention
    {
        public string FeatureFlagNameFor(Type serviceType) => flagName;
        public string VariantFlagNameFor(Type serviceType) => flagName;
        public string ConfigurationKeyFor(Type serviceType) => $"Config:{flagName}";
        public string OpenFeatureFlagNameFor(Type serviceType) => flagName.ToLowerInvariant();
    }
}
