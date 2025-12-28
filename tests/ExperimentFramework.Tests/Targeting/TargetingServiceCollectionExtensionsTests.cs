using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Selection;
using ExperimentFramework.Targeting;
using ExperimentFramework.Targeting.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Targeting;

[Feature("ServiceCollectionExtensions register targeting services")]
public sealed class TargetingServiceCollectionExtensionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("AddExperimentTargeting registers TargetingProvider factory")]
    [Fact]
    public async Task AddExperimentTargeting_registers_provider()
    {
        var services = new ServiceCollection();
        services.AddExperimentTargeting();
        var sp = services.BuildServiceProvider();

        var factories = sp.GetServices<ISelectionModeProviderFactory>().ToList();

        Assert.NotEmpty(factories);
        var factory = factories.FirstOrDefault(f => f.Create(sp) is TargetingProvider);
        Assert.NotNull(factory);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentTargeting registers TargetingOptions")]
    [Fact]
    public async Task AddExperimentTargeting_registers_options()
    {
        var services = new ServiceCollection();
        services.AddExperimentTargeting();
        var sp = services.BuildServiceProvider();

        var options = sp.GetService<TargetingOptions>();

        Assert.NotNull(options);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentTargeting with configuration applies options")]
    [Fact]
    public async Task AddExperimentTargeting_with_configuration()
    {
        var services = new ServiceCollection();
        services.AddExperimentTargeting(opts =>
        {
            opts.DefaultRule = TargetingRules.HasAttribute("test");
            opts.MatchedKey = "treatment";
            opts.UnmatchedKey = "control";
        });
        var sp = services.BuildServiceProvider();

        var options = sp.GetService<TargetingOptions>();

        Assert.NotNull(options);
        Assert.NotNull(options.DefaultRule);
        Assert.Equal("treatment", options.MatchedKey);
        Assert.Equal("control", options.UnmatchedKey);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentTargeting returns service collection for chaining")]
    [Fact]
    public Task AddExperimentTargeting_returns_service_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding experiment targeting", services => services.AddExperimentTargeting())
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("AddExperimentTargeting without configuration uses default options")]
    [Fact]
    public async Task AddExperimentTargeting_without_configuration_uses_defaults()
    {
        var services = new ServiceCollection();
        services.AddExperimentTargeting();
        var sp = services.BuildServiceProvider();

        var options = sp.GetService<TargetingOptions>();

        Assert.NotNull(options);
        Assert.Null(options.DefaultRule);
        Assert.Equal("true", options.MatchedKey);
        Assert.Null(options.UnmatchedKey);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentTargeting is idempotent for options")]
    [Fact]
    public async Task AddExperimentTargeting_is_idempotent_for_options()
    {
        var services = new ServiceCollection();
        services.AddExperimentTargeting(opts => opts.MatchedKey = "first");
        services.AddExperimentTargeting(opts => opts.MatchedKey = "second"); // Should not override
        var sp = services.BuildServiceProvider();

        var options = sp.GetService<TargetingOptions>();

        // TryAddSingleton keeps the first registration
        Assert.NotNull(options);
        Assert.Equal("first", options.MatchedKey);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentTargetingRules registers InMemoryTargetingConfiguration")]
    [Fact]
    public async Task AddExperimentTargetingRules_registers_configuration()
    {
        var services = new ServiceCollection();
        services.AddExperimentTargetingRules(cfg =>
        {
            cfg.AddRule("test-selector", TargetingRules.Always(), "test-key");
        });
        var sp = services.BuildServiceProvider();

        var config = sp.GetService<ITargetingConfigurationProvider>();

        Assert.NotNull(config);
        var rules = config.GetRulesFor("test-selector");
        Assert.NotNull(rules);
        Assert.Single(rules);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentTargetingRules returns service collection for chaining")]
    [Fact]
    public Task AddExperimentTargetingRules_returns_service_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding experiment targeting rules", services =>
                services.AddExperimentTargetingRules(cfg => { }))
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("AddExperimentTargetingRules applies configuration action")]
    [Fact]
    public async Task AddExperimentTargetingRules_applies_configuration()
    {
        var services = new ServiceCollection();
        services.AddExperimentTargetingRules(cfg =>
        {
            cfg.AddRule("selector-a", TargetingRules.Users("user-1"), "key-a");
            cfg.AddRule("selector-b", TargetingRules.HasAttribute("beta"), "key-b");
        });
        var sp = services.BuildServiceProvider();

        var config = sp.GetService<ITargetingConfigurationProvider>();

        Assert.NotNull(config);
        Assert.NotNull(config.GetRulesFor("selector-a"));
        Assert.NotNull(config.GetRulesFor("selector-b"));

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentTargetingRules is idempotent")]
    [Fact]
    public async Task AddExperimentTargetingRules_is_idempotent()
    {
        var services = new ServiceCollection();
        services.AddExperimentTargetingRules(cfg =>
        {
            cfg.AddRule("first-selector", TargetingRules.Always(), "first-key");
        });
        services.AddExperimentTargetingRules(cfg =>
        {
            cfg.AddRule("second-selector", TargetingRules.Never(), "second-key");
        });
        var sp = services.BuildServiceProvider();

        var config = sp.GetService<ITargetingConfigurationProvider>();

        // TryAddSingleton keeps the first registration
        Assert.NotNull(config);
        Assert.NotNull(config.GetRulesFor("first-selector"));
        Assert.Null(config.GetRulesFor("second-selector"));

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentTargetingConfiguration registers configuration handler")]
    [Fact]
    public async Task AddExperimentTargetingConfiguration_registers_handler()
    {
        var services = new ServiceCollection();
        services.AddExperimentTargetingConfiguration();
        var sp = services.BuildServiceProvider();

        var handlers = sp.GetServices<IConfigurationSelectionModeHandler>().ToList();

        Assert.Single(handlers);
        Assert.IsType<TargetingSelectionModeHandler>(handlers[0]);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentTargetingConfiguration handler has correct mode type")]
    [Fact]
    public async Task AddExperimentTargetingConfiguration_handler_mode_type()
    {
        var services = new ServiceCollection();
        services.AddExperimentTargetingConfiguration();
        var sp = services.BuildServiceProvider();

        var handlers = sp.GetServices<IConfigurationSelectionModeHandler>().ToList();

        Assert.Single(handlers);
        Assert.Equal("targeting", handlers[0].ModeType);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentTargetingConfiguration returns service collection for chaining")]
    [Fact]
    public Task AddExperimentTargetingConfiguration_returns_service_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding experiment targeting configuration",
                services => services.AddExperimentTargetingConfiguration())
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("AddExperimentTargetingConfiguration is idempotent")]
    [Fact]
    public async Task AddExperimentTargetingConfiguration_is_idempotent()
    {
        var services = new ServiceCollection();
        services.AddExperimentTargetingConfiguration();
        services.AddExperimentTargetingConfiguration(); // Call again
        var sp = services.BuildServiceProvider();

        var handlers = sp.GetServices<IConfigurationSelectionModeHandler>().ToList();

        // TryAddEnumerable prevents duplicates
        Assert.Single(handlers);

        await Task.CompletedTask;
    }

    [Scenario("Can combine all targeting registrations")]
    [Fact]
    public async Task Can_combine_all_registrations()
    {
        var services = new ServiceCollection();
        services
            .AddExperimentTargeting(opts => opts.MatchedKey = "enabled")
            .AddExperimentTargetingRules(cfg =>
            {
                cfg.AddRule("test", TargetingRules.Always(), "always-key");
            })
            .AddExperimentTargetingConfiguration();
        var sp = services.BuildServiceProvider();

        var options = sp.GetService<TargetingOptions>();
        var config = sp.GetService<ITargetingConfigurationProvider>();
        var handlers = sp.GetServices<IConfigurationSelectionModeHandler>().ToList();
        var factories = sp.GetServices<ISelectionModeProviderFactory>().ToList();

        Assert.NotNull(options);
        Assert.Equal("enabled", options.MatchedKey);
        Assert.NotNull(config);
        Assert.NotNull(config.GetRulesFor("test"));
        Assert.Single(handlers);
        Assert.Contains(factories, f => f.Create(sp) is TargetingProvider);

        await Task.CompletedTask;
    }
}
