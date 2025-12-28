using ExperimentFramework.AutoStop;
using ExperimentFramework.AutoStop.Rules;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.AutoStop;

[Feature("ServiceCollectionExtensions registers auto-stop services")]
public sealed class AutoStopServiceCollectionExtensionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("AddExperimentAutoStop without configure uses defaults")]
    [Fact]
    public async Task AddExperimentAutoStop_without_configure_uses_defaults()
    {
        var services = new ServiceCollection();
        services.AddExperimentAutoStop();
        var sp = services.BuildServiceProvider();

        var options = sp.GetService<AutoStopOptions>();
        var rules = sp.GetServices<IStoppingRule>().ToList();

        Assert.NotNull(options);
        Assert.Equal(1000, options.MinimumSampleSize);
        Assert.Equal(0.95, options.ConfidenceLevel);
        Assert.Equal(TimeSpan.FromMinutes(5), options.CheckInterval);
        Assert.Equal(2, rules.Count); // Default rules added

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentAutoStop with configure applies options")]
    [Fact]
    public async Task AddExperimentAutoStop_with_configure_applies_options()
    {
        var services = new ServiceCollection();
        services.AddExperimentAutoStop(opts =>
        {
            opts.MinimumSampleSize = 500;
            opts.ConfidenceLevel = 0.99;
            opts.CheckInterval = TimeSpan.FromMinutes(10);
        });
        var sp = services.BuildServiceProvider();

        var options = sp.GetService<AutoStopOptions>();

        Assert.NotNull(options);
        Assert.Equal(500, options.MinimumSampleSize);
        Assert.Equal(0.99, options.ConfidenceLevel);
        Assert.Equal(TimeSpan.FromMinutes(10), options.CheckInterval);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentAutoStop with custom rules uses them")]
    [Fact]
    public async Task AddExperimentAutoStop_with_custom_rules()
    {
        var customRule = new MinimumSampleSizeRule(100);

        var services = new ServiceCollection();
        services.AddExperimentAutoStop(opts =>
        {
            opts.Rules.Add(customRule);
        });
        var sp = services.BuildServiceProvider();

        var rules = sp.GetServices<IStoppingRule>().ToList();

        Assert.Single(rules);
        Assert.Same(customRule, rules[0]);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentAutoStop is idempotent for options")]
    [Fact]
    public async Task AddExperimentAutoStop_is_idempotent()
    {
        var services = new ServiceCollection();
        services.AddExperimentAutoStop(opts => opts.MinimumSampleSize = 100);
        services.AddExperimentAutoStop(opts => opts.MinimumSampleSize = 500);
        var sp = services.BuildServiceProvider();

        var options = sp.GetService<AutoStopOptions>();

        // TryAddSingleton keeps first registration
        Assert.NotNull(options);
        Assert.Equal(100, options.MinimumSampleSize);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentAutoStop returns service collection for chaining")]
    [Fact]
    public Task AddExperimentAutoStop_returns_service_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding auto stop", services => services.AddExperimentAutoStop())
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("AddStoppingRule registers custom rule")]
    [Fact]
    public async Task AddStoppingRule_registers_custom_rule()
    {
        var services = new ServiceCollection();
        services.AddStoppingRule<CustomStoppingRule>();
        var sp = services.BuildServiceProvider();

        var rules = sp.GetServices<IStoppingRule>().ToList();

        Assert.Single(rules);
        Assert.IsType<CustomStoppingRule>(rules[0]);

        await Task.CompletedTask;
    }

    [Scenario("AddStoppingRule returns service collection for chaining")]
    [Fact]
    public Task AddStoppingRule_returns_service_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding stopping rule", services => services.AddStoppingRule<CustomStoppingRule>())
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("AddStoppingRule is idempotent")]
    [Fact]
    public async Task AddStoppingRule_is_idempotent()
    {
        var services = new ServiceCollection();
        services.AddStoppingRule<CustomStoppingRule>();
        services.AddStoppingRule<CustomStoppingRule>();
        var sp = services.BuildServiceProvider();

        var rules = sp.GetServices<IStoppingRule>().ToList();

        // TryAddEnumerable only adds once
        Assert.Single(rules);

        await Task.CompletedTask;
    }

    [Scenario("Multiple different rules can be registered")]
    [Fact]
    public async Task Multiple_different_rules_registered()
    {
        var services = new ServiceCollection();
        services.AddStoppingRule<CustomStoppingRule>();
        services.AddStoppingRule<MinimumSampleSizeRule>();
        var sp = services.BuildServiceProvider();

        var rules = sp.GetServices<IStoppingRule>().ToList();

        Assert.Equal(2, rules.Count);

        await Task.CompletedTask;
    }

    [Scenario("AutoStopOptions has correct default values")]
    [Fact]
    public Task AutoStopOptions_has_correct_defaults()
        => Given("default auto stop options", () => new AutoStopOptions())
            .Then("minimum sample size is 1000", opts => opts.MinimumSampleSize == 1000)
            .And("confidence level is 0.95", opts => opts.ConfidenceLevel == 0.95)
            .And("check interval is 5 minutes", opts => opts.CheckInterval == TimeSpan.FromMinutes(5))
            .And("rules list is empty", opts => opts.Rules.Count == 0)
            .AssertPassed();

    [Scenario("AutoStopOptions rules can be modified")]
    [Fact]
    public Task AutoStopOptions_rules_modifiable()
        => Given("auto stop options", () => new AutoStopOptions())
            .When("adding a rule", opts =>
            {
                opts.Rules.Add(new MinimumSampleSizeRule(100));
                return opts;
            })
            .Then("rules list contains the rule", opts => opts.Rules.Count == 1)
            .AssertPassed();

    private sealed class CustomStoppingRule : IStoppingRule
    {
        public string Name => "Custom";

        public StoppingDecision Evaluate(ExperimentData data)
            => new(false, "Custom rule");
    }
}
