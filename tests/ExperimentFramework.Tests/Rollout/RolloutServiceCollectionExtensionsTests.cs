using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Rollout;
using ExperimentFramework.Selection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Rollout;

[Feature("ServiceCollectionExtensions register rollout providers")]
public sealed class RolloutServiceCollectionExtensionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("AddExperimentRollout registers RolloutProvider factory")]
    [Fact]
    public async Task AddExperimentRollout_registers_provider()
    {
        var services = new ServiceCollection();
        services.AddExperimentRollout();
        var sp = services.BuildServiceProvider();

        var factories = sp.GetServices<ISelectionModeProviderFactory>().ToList();

        // Should have at least one factory that can create RolloutProvider
        Assert.NotEmpty(factories);
        var factory = factories.FirstOrDefault(f => f.Create(sp) is RolloutProvider);
        Assert.NotNull(factory);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentRollout with configuration applies options")]
    [Fact]
    public async Task AddExperimentRollout_with_configuration()
    {
        var services = new ServiceCollection();
        services.AddExperimentRollout(opts =>
        {
            opts.Percentage = 75;
            opts.Seed = "test-seed";
            opts.IncludedKey = "treatment";
            opts.ExcludedKey = "control";
        });
        var sp = services.BuildServiceProvider();

        var options = sp.GetService<IOptions<RolloutOptions>>();

        Assert.NotNull(options);
        Assert.Equal(75, options.Value.Percentage);
        Assert.Equal("test-seed", options.Value.Seed);
        Assert.Equal("treatment", options.Value.IncludedKey);
        Assert.Equal("control", options.Value.ExcludedKey);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentRollout returns service collection for chaining")]
    [Fact]
    public Task AddExperimentRollout_returns_service_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding experiment rollout", services => services.AddExperimentRollout())
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("AddExperimentStagedRollout registers StagedRolloutProvider factory")]
    [Fact]
    public async Task AddExperimentStagedRollout_registers_provider()
    {
        var services = new ServiceCollection();
        services.AddExperimentStagedRollout();
        var sp = services.BuildServiceProvider();

        var factories = sp.GetServices<ISelectionModeProviderFactory>().ToList();

        // Should have at least one factory that can create StagedRolloutProvider
        Assert.NotEmpty(factories);
        var factory = factories.FirstOrDefault(f => f.Create(sp) is StagedRolloutProvider);
        Assert.NotNull(factory);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentStagedRollout with configuration applies options")]
    [Fact]
    public async Task AddExperimentStagedRollout_with_configuration()
    {
        var services = new ServiceCollection();
        var stage = new RolloutStage { StartsAt = DateTimeOffset.UtcNow, Percentage = 25 };
        services.AddExperimentStagedRollout(opts =>
        {
            opts.Stages.Add(stage);
            opts.IncludedKey = "enabled";
            opts.ExcludedKey = "disabled";
        });
        var sp = services.BuildServiceProvider();

        var options = sp.GetService<IOptions<StagedRolloutOptions>>();

        Assert.NotNull(options);
        Assert.Single(options.Value.Stages);
        Assert.Equal("enabled", options.Value.IncludedKey);
        Assert.Equal("disabled", options.Value.ExcludedKey);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentStagedRollout returns service collection for chaining")]
    [Fact]
    public Task AddExperimentStagedRollout_returns_service_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding experiment staged rollout", services => services.AddExperimentStagedRollout())
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("AddExperimentRolloutConfiguration registers configuration handlers")]
    [Fact]
    public async Task AddExperimentRolloutConfiguration_registers_handlers()
    {
        var services = new ServiceCollection();
        services.AddExperimentRolloutConfiguration();
        var sp = services.BuildServiceProvider();

        var handlers = sp.GetServices<IConfigurationSelectionModeHandler>().ToList();

        Assert.Equal(2, handlers.Count);
        Assert.Contains(handlers, h => h.ModeType == "rollout");
        Assert.Contains(handlers, h => h.ModeType == "stagedRollout");

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentRolloutConfiguration is idempotent")]
    [Fact]
    public async Task AddExperimentRolloutConfiguration_is_idempotent()
    {
        var services = new ServiceCollection();
        services.AddExperimentRolloutConfiguration();
        services.AddExperimentRolloutConfiguration(); // Call again
        var sp = services.BuildServiceProvider();

        var handlers = sp.GetServices<IConfigurationSelectionModeHandler>().ToList();

        // Should still only have 2 handlers due to TryAddEnumerable
        Assert.Equal(2, handlers.Count);

        await Task.CompletedTask;
    }
}
