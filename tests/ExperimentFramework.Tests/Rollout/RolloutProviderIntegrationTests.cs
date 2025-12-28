using ExperimentFramework.Naming;
using ExperimentFramework.Rollout;
using ExperimentFramework.Selection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Rollout;

[Feature("RolloutProvider integrates with DI for percentage-based selection")]
public sealed class RolloutProviderIntegrationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Provider returns excluded key when no identity provider")]
    [Fact]
    public async Task Returns_excluded_key_without_identity()
    {
        var services = new ServiceCollection();
        services.Configure<RolloutOptions>(o => o.ExcludedKey = "control");
        var sp = services.BuildServiceProvider();

        var provider = new RolloutProvider(
            options: sp.GetService<IOptions<RolloutOptions>>());
        var context = CreateContext("test-rollout", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("control", result);
    }

    [Scenario("Provider returns included key when user is in rollout")]
    [Fact]
    public async Task Returns_included_key_when_in_rollout()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRolloutIdentityProvider>(new TestIdentityProvider("user-included"));
        services.Configure<RolloutOptions>(o =>
        {
            o.Percentage = 100;
            o.IncludedKey = "treatment";
        });
        var sp = services.BuildServiceProvider();

        var provider = new RolloutProvider(
            sp.GetService<IRolloutIdentityProvider>(),
            sp.GetService<IOptions<RolloutOptions>>());
        var context = CreateContext("test-rollout", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("treatment", result);
    }

    [Scenario("Provider returns excluded key when user is not in rollout")]
    [Fact]
    public async Task Returns_excluded_key_when_not_in_rollout()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRolloutIdentityProvider>(new TestIdentityProvider("user-excluded"));
        services.Configure<RolloutOptions>(o =>
        {
            o.Percentage = 0;
            o.IncludedKey = "treatment";
            o.ExcludedKey = "control";
        });
        var sp = services.BuildServiceProvider();

        var provider = new RolloutProvider(
            sp.GetService<IRolloutIdentityProvider>(),
            sp.GetService<IOptions<RolloutOptions>>());
        var context = CreateContext("test-rollout", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("control", result);
    }

    [Scenario("Provider uses identity provider from context service provider")]
    [Fact]
    public async Task Uses_identity_from_context_service_provider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRolloutIdentityProvider>(new TestIdentityProvider("context-user"));
        services.Configure<RolloutOptions>(o =>
        {
            o.Percentage = 100;
            o.IncludedKey = "enabled";
        });
        var sp = services.BuildServiceProvider();

        var provider = new RolloutProvider(); // No constructor injection
        var context = CreateContext("test-rollout", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("enabled", result);
    }

    [Scenario("Provider uses options snapshot from context")]
    [Fact]
    public async Task Uses_options_snapshot_from_context()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRolloutIdentityProvider>(new TestIdentityProvider("user-1"));
        services.Configure<RolloutOptions>(o =>
        {
            o.Percentage = 100;
            o.IncludedKey = "snapshot-value";
        });
        var sp = services.BuildServiceProvider();

        var provider = new RolloutProvider(
            sp.GetService<IRolloutIdentityProvider>());
        var context = CreateContext("test-rollout", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("snapshot-value", result);
    }

    [Scenario("Provider has correct mode identifier")]
    [Fact]
    public Task Provider_has_correct_mode_identifier()
        => Given("a rollout provider", () => new RolloutProvider())
            .Then("mode identifier is 'Rollout'", p => p.ModeIdentifier == "Rollout")
            .AssertPassed();

    [Scenario("Provider generates default selector name")]
    [Fact]
    public Task Provider_generates_default_selector_name()
        => Given("a rollout provider and naming convention", () =>
                (Provider: new RolloutProvider(), Convention: DefaultExperimentNamingConvention.Instance))
            .When("getting default selector name", data =>
                data.Provider.GetDefaultSelectorName(typeof(IFormattable), data.Convention))
            .Then("name starts with Rollout:", name => name.StartsWith("Rollout:"))
            .AssertPassed();

    [Scenario("Provider returns null excluded key when identity fails")]
    [Fact]
    public async Task Returns_null_when_identity_fails()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRolloutIdentityProvider>(new FailingIdentityProvider());
        var sp = services.BuildServiceProvider();

        var provider = new RolloutProvider(sp.GetService<IRolloutIdentityProvider>());
        var context = CreateContext("test-rollout", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result); // Default excluded key is null
    }

    [Scenario("Provider uses seed for consistent allocation")]
    [Fact]
    public async Task Uses_seed_for_consistent_allocation()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRolloutIdentityProvider>(new TestIdentityProvider("consistent-user"));
        services.Configure<RolloutOptions>(o =>
        {
            o.Percentage = 50;
            o.Seed = "test-seed";
            o.IncludedKey = "included";
            o.ExcludedKey = "excluded";
        });
        var sp = services.BuildServiceProvider();

        var provider = new RolloutProvider(
            sp.GetService<IRolloutIdentityProvider>(),
            sp.GetService<IOptions<RolloutOptions>>());
        var context = CreateContext("test-rollout", sp);

        // Multiple calls should return the same result
        var results = new List<string?>();
        for (var i = 0; i < 5; i++)
        {
            results.Add(await provider.SelectTrialKeyAsync(context));
        }

        Assert.Single(results.Distinct());
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

    private sealed class TestIdentityProvider(string identity) : IRolloutIdentityProvider
    {
        public bool TryGetIdentity(out string identity1)
        {
            identity1 = identity;
            return true;
        }
    }

    private sealed class FailingIdentityProvider : IRolloutIdentityProvider
    {
        public bool TryGetIdentity(out string identity)
        {
            identity = null!;
            return false;
        }
    }
}
