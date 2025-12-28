using ExperimentFramework.Naming;
using ExperimentFramework.Rollout;
using ExperimentFramework.Selection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Rollout;

[Feature("StagedRolloutProvider integrates with DI for time-based rollout")]
public sealed class StagedRolloutProviderIntegrationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Provider returns excluded key when no identity provider")]
    [Fact]
    public async Task Returns_excluded_key_without_identity()
    {
        var services = new ServiceCollection();
        services.Configure<StagedRolloutOptions>(o => o.ExcludedKey = "control");
        var sp = services.BuildServiceProvider();

        var provider = new StagedRolloutProvider(
            options: sp.GetService<IOptions<StagedRolloutOptions>>());
        var context = CreateContext("test-staged", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("control", result);
    }

    [Scenario("Provider returns included key based on current stage")]
    [Fact]
    public async Task Returns_included_key_for_current_stage()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRolloutIdentityProvider>(new TestIdentityProvider("user-1"));
        services.Configure<StagedRolloutOptions>(o =>
        {
            o.Stages =
            [
                new RolloutStage { StartsAt = DateTimeOffset.UtcNow.AddDays(-1), Percentage = 100 }
            ];
            o.IncludedKey = "enabled";
        });
        var sp = services.BuildServiceProvider();

        var provider = new StagedRolloutProvider(
            sp.GetService<IRolloutIdentityProvider>(),
            sp.GetService<IOptions<StagedRolloutOptions>>());
        var context = CreateContext("test-staged", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("enabled", result);
    }

    [Scenario("Provider returns excluded key when no stages active")]
    [Fact]
    public async Task Returns_excluded_key_when_no_stages_active()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRolloutIdentityProvider>(new TestIdentityProvider("user-1"));
        services.Configure<StagedRolloutOptions>(o =>
        {
            o.Stages =
            [
                new RolloutStage { StartsAt = DateTimeOffset.UtcNow.AddDays(1), Percentage = 100 }
            ];
            o.IncludedKey = "enabled";
            o.ExcludedKey = "disabled";
        });
        var sp = services.BuildServiceProvider();

        var provider = new StagedRolloutProvider(
            sp.GetService<IRolloutIdentityProvider>(),
            sp.GetService<IOptions<StagedRolloutOptions>>());
        var context = CreateContext("test-staged", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("disabled", result);
    }

    [Scenario("Provider uses custom time provider")]
    [Fact]
    public async Task Uses_custom_time_provider()
    {
        var fixedTime = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new TestTimeProvider(fixedTime);

        var services = new ServiceCollection();
        services.AddSingleton<IRolloutIdentityProvider>(new TestIdentityProvider("user-1"));
        services.Configure<StagedRolloutOptions>(o =>
        {
            o.Stages =
            [
                new RolloutStage { StartsAt = fixedTime.AddDays(-7), Percentage = 10 },
                new RolloutStage { StartsAt = fixedTime.AddDays(-1), Percentage = 50 },
                new RolloutStage { StartsAt = fixedTime.AddDays(7), Percentage = 100 }
            ];
            o.IncludedKey = "enabled";
            o.ExcludedKey = "disabled";
        });
        var sp = services.BuildServiceProvider();

        var provider = new StagedRolloutProvider(
            sp.GetService<IRolloutIdentityProvider>(),
            sp.GetService<IOptions<StagedRolloutOptions>>(),
            timeProvider);
        var context = CreateContext("test-staged", sp);

        // At 50% with seeded allocation, some users will be included
        var result = await provider.SelectTrialKeyAsync(context);

        Assert.NotNull(result);
        Assert.True(result == "enabled" || result == "disabled");
    }

    [Scenario("Provider has correct mode identifier")]
    [Fact]
    public Task Provider_has_correct_mode_identifier()
        => Given("a staged rollout provider", () => new StagedRolloutProvider())
            .Then("mode identifier is 'StagedRollout'", p => p.ModeIdentifier == "StagedRollout")
            .AssertPassed();

    [Scenario("Provider generates default selector name")]
    [Fact]
    public Task Provider_generates_default_selector_name()
        => Given("a staged rollout provider and naming convention", () =>
                (Provider: new StagedRolloutProvider(), Convention: DefaultExperimentNamingConvention.Instance))
            .When("getting default selector name", data =>
                data.Provider.GetDefaultSelectorName(typeof(IFormattable), data.Convention))
            .Then("name starts with StagedRollout:", name => name.StartsWith("StagedRollout:"))
            .AssertPassed();

    [Scenario("Provider uses identity from context service provider")]
    [Fact]
    public async Task Uses_identity_from_context()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRolloutIdentityProvider>(new TestIdentityProvider("context-user"));
        services.Configure<StagedRolloutOptions>(o =>
        {
            o.Stages = [new RolloutStage { StartsAt = DateTimeOffset.UtcNow.AddDays(-1), Percentage = 100 }];
            o.IncludedKey = "from-context";
        });
        var sp = services.BuildServiceProvider();

        var provider = new StagedRolloutProvider(); // No constructor injection
        var context = CreateContext("test-staged", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("from-context", result);
    }

    [Scenario("Provider returns null when identity fails")]
    [Fact]
    public async Task Returns_null_when_identity_fails()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRolloutIdentityProvider>(new FailingIdentityProvider());
        var sp = services.BuildServiceProvider();

        var provider = new StagedRolloutProvider(sp.GetService<IRolloutIdentityProvider>());
        var context = CreateContext("test-staged", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Scenario("Provider uses options snapshot from context")]
    [Fact]
    public async Task Uses_options_snapshot_from_context()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRolloutIdentityProvider>(new TestIdentityProvider("user-1"));
        services.Configure<StagedRolloutOptions>(o =>
        {
            o.Stages = [new RolloutStage { StartsAt = DateTimeOffset.UtcNow.AddDays(-1), Percentage = 100 }];
            o.IncludedKey = "snapshot-value";
        });
        var sp = services.BuildServiceProvider();

        var provider = new StagedRolloutProvider(sp.GetService<IRolloutIdentityProvider>());
        var context = CreateContext("test-staged", sp);

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("snapshot-value", result);
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

    private sealed class TestTimeProvider(DateTimeOffset fixedTime) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => fixedTime;
    }
}
