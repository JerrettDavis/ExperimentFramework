using ExperimentFramework.Naming;
using ExperimentFramework.Selection;
using ExperimentFramework.Targeting;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Targeting;

[Feature("TargetingProvider integrates with DI for rule-based selection")]
public sealed class TargetingProviderIntegrationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Provider returns null when no context provider")]
    [Fact]
    public async Task Returns_null_without_context_provider()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider();
        var context = CreateContext("test-targeting", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Scenario("Provider returns null when context is null")]
    [Fact]
    public async Task Returns_null_when_context_null()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITargetingContextProvider>(new NullContextProvider());
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider(sp.GetService<ITargetingContextProvider>());
        var context = CreateContext("test-targeting", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Scenario("Provider evaluates default rule from options")]
    [Fact]
    public async Task Evaluates_default_rule_from_options()
    {
        var targetingContext = new SimpleTargetingContext("user-1")
            .WithAttribute("beta", true);

        var options = new TargetingOptions
        {
            DefaultRule = TargetingRules.HasAttribute("beta"),
            MatchedKey = "beta-user",
            UnmatchedKey = "regular-user"
        };

        var services = new ServiceCollection();
        services.AddSingleton<ITargetingContextProvider>(new TestContextProvider(targetingContext));
        services.AddSingleton(options);
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider(
            sp.GetService<ITargetingContextProvider>(),
            options);
        var context = CreateContext("test-targeting", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("beta-user", result);
    }

    [Scenario("Provider returns unmatched key when rule fails")]
    [Fact]
    public async Task Returns_unmatched_key_when_rule_fails()
    {
        var targetingContext = new SimpleTargetingContext("user-1"); // No beta attribute

        var options = new TargetingOptions
        {
            DefaultRule = TargetingRules.HasAttribute("beta"),
            MatchedKey = "beta-user",
            UnmatchedKey = "regular-user"
        };

        var services = new ServiceCollection();
        services.AddSingleton<ITargetingContextProvider>(new TestContextProvider(targetingContext));
        services.AddSingleton(options);
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider(
            sp.GetService<ITargetingContextProvider>(),
            options);
        var context = CreateContext("test-targeting", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("regular-user", result);
    }

    [Scenario("Provider evaluates rules from configuration provider")]
    [Fact]
    public async Task Evaluates_rules_from_configuration()
    {
        var targetingContext = new SimpleTargetingContext("user-1")
            .WithAttribute("plan", "premium");

        var configProvider = new TestTargetingConfigProvider(new Dictionary<string, IReadOnlyList<(ITargetingRule, string)>>
        {
            ["test-targeting"] = new List<(ITargetingRule, string)>
            {
                (TargetingRules.AttributeEquals("plan", "premium"), "premium-variant"),
                (TargetingRules.Always(), "default-variant")
            }
        });

        var services = new ServiceCollection();
        services.AddSingleton<ITargetingContextProvider>(new TestContextProvider(targetingContext));
        services.AddSingleton<ITargetingConfigurationProvider>(configProvider);
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider(sp.GetService<ITargetingContextProvider>());
        var context = CreateContext("test-targeting", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("premium-variant", result);
    }

    [Scenario("Provider returns null when no rules match")]
    [Fact]
    public async Task Returns_null_when_no_rules_match()
    {
        var targetingContext = new SimpleTargetingContext("user-1")
            .WithAttribute("plan", "free");

        var configProvider = new TestTargetingConfigProvider(new Dictionary<string, IReadOnlyList<(ITargetingRule, string)>>
        {
            ["test-targeting"] = new List<(ITargetingRule, string)>
            {
                (TargetingRules.AttributeEquals("plan", "premium"), "premium-variant"),
                (TargetingRules.AttributeEquals("plan", "enterprise"), "enterprise-variant")
            }
        });

        var services = new ServiceCollection();
        services.AddSingleton<ITargetingContextProvider>(new TestContextProvider(targetingContext));
        services.AddSingleton<ITargetingConfigurationProvider>(configProvider);
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider(sp.GetService<ITargetingContextProvider>());
        var context = CreateContext("test-targeting", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Scenario("Provider has correct mode identifier")]
    [Fact]
    public Task Provider_has_correct_mode_identifier()
        => Given("a targeting provider", () => new TargetingProvider())
            .Then("mode identifier is 'Targeting'", p => p.ModeIdentifier == "Targeting")
            .AssertPassed();

    [Scenario("Provider generates default selector name")]
    [Fact]
    public Task Provider_generates_default_selector_name()
        => Given("a targeting provider and naming convention", () =>
                (Provider: new TargetingProvider(), Convention: DefaultExperimentNamingConvention.Instance))
            .When("getting default selector name", data =>
                data.Provider.GetDefaultSelectorName(typeof(IFormattable), data.Convention))
            .Then("name starts with Targeting:", name => name.StartsWith("Targeting:"))
            .AssertPassed();

    [Scenario("Provider uses context provider from service provider")]
    [Fact]
    public async Task Uses_context_from_service_provider()
    {
        var targetingContext = new SimpleTargetingContext("sp-user")
            .WithAttribute("source", "service-provider");

        var options = new TargetingOptions
        {
            DefaultRule = TargetingRules.HasAttribute("source"),
            MatchedKey = "from-sp"
        };

        var services = new ServiceCollection();
        services.AddSingleton<ITargetingContextProvider>(new TestContextProvider(targetingContext));
        services.AddSingleton(options);
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider(options: options); // No constructor-injected context provider
        var context = CreateContext("test-targeting", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("from-sp", result);
    }

    [Scenario("Provider uses options from service provider")]
    [Fact]
    public async Task Uses_options_from_service_provider()
    {
        var targetingContext = new SimpleTargetingContext("user-1")
            .WithAttribute("test", true);

        var options = new TargetingOptions
        {
            DefaultRule = TargetingRules.HasAttribute("test"),
            MatchedKey = "from-options",
            UnmatchedKey = "fallback"
        };

        var services = new ServiceCollection();
        services.AddSingleton<ITargetingContextProvider>(new TestContextProvider(targetingContext));
        services.AddSingleton(options);
        var sp = services.BuildServiceProvider();

        var provider = new TargetingProvider(sp.GetService<ITargetingContextProvider>());
        var context = CreateContext("test-targeting", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("from-options", result);
    }

    private static SelectionContext CreateContext(string selectorName, IServiceProvider sp)
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
}
