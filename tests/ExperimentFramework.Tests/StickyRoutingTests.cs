using ExperimentFramework.Naming;
using ExperimentFramework.Selection;
using ExperimentFramework.StickyRouting;
using ExperimentFramework.Tests.TestInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests;

[Feature("Sticky routing provides deterministic trial selection based on user identity")]
public sealed class StickyRoutingTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record RoutingState(
        string Identity,
        string SelectorName,
        List<string> TrialKeys);

    private sealed record RoutingResult(
        RoutingState State,
        string SelectedTrial);

    private static RoutingState CreateState(string identity, string selectorName, params string[] trialKeys)
        => new(identity, selectorName, [.. trialKeys]);

    private static RoutingResult SelectTrial(RoutingState state)
    {
        var selected = StickyTrialRouter.SelectTrial(state.Identity, state.SelectorName, state.TrialKeys);
        return new RoutingResult(state, selected);
    }

    [Scenario("Same identity and selector always returns same trial")]
    [Fact]
    public Task Same_identity_returns_same_trial()
        => Given("user identity and two trials", () => CreateState("user-123", "MyExperiment", "trial-a", "trial-b"))
            .When("select trial first time", SelectTrial)
            .Then("trial is selected", r => !string.IsNullOrEmpty(r.SelectedTrial))
            .When("select trial second time", r => SelectTrial(r.State))
            .Then("same trial selected", r => r.SelectedTrial == SelectTrial(r.State).SelectedTrial)
            .When("select trial third time", r => SelectTrial(r.State))
            .Then("same trial selected again", r => r.SelectedTrial == SelectTrial(r.State).SelectedTrial)
            .AssertPassed();

    [Scenario("Different identities distribute across trials")]
    [Fact]
    public Task Different_identities_distribute_trials()
        => Given("100 different user identities", () => Enumerable.Range(1, 100).Select(i => $"user-{i}").ToList())
            .When("select trials for all users", identities =>
            {
                var selections = identities
                    .Select(id => StickyTrialRouter.SelectTrial(id, "MyExperiment", ["trial-a", "trial-b"]))
                    .ToList();
                return selections;
            })
            .Then("both trials are selected by at least one user", selections =>
            {
                var trialCounts = selections.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
                return trialCounts.ContainsKey("trial-a") && trialCounts.ContainsKey("trial-b");
            })
            .And("distribution is reasonably balanced", selections =>
            {
                var trialCounts = selections.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
                var aCount = trialCounts.GetValueOrDefault("trial-a", 0);
                var bCount = trialCounts.GetValueOrDefault("trial-b", 0);
                // Should be roughly 50/50, allow 30-70 split for randomness
                return aCount >= 30 && aCount <= 70 && bCount >= 30 && bCount <= 70;
            })
            .AssertPassed();

    [Scenario("Changing selector name changes trial assignment")]
    [Fact]
    public Task Different_selector_changes_assignment()
        => Given("same identity with different selectors", () => ("user-123", "Experiment1", "Experiment2"))
            .When("select trial for first selector", state =>
            {
                var trial1 = StickyTrialRouter.SelectTrial(state.Item1, state.Item2, ["trial-a", "trial-b"]);
                return (state, trial1);
            })
            .When("select trial for second selector", r =>
            {
                var trial2 = StickyTrialRouter.SelectTrial(r.state.Item1, r.state.Item3, ["trial-a", "trial-b"]);
                return (r.state, r.trial1, trial2);
            })
            .Then("trials can be different", _ =>
            {
                // Not guaranteed to be different, but possible. Test that selector is used in hash.
                // We'll verify that at least sometimes they differ across many identities.
                var sameCount = 0;
                for (var i = 1; i <= 50; i++)
                {
                    var id = $"user-{i}";
                    var t1 = StickyTrialRouter.SelectTrial(id, "Exp1", ["a", "b"]);
                    var t2 = StickyTrialRouter.SelectTrial(id, "Exp2", ["a", "b"]);
                    if (t1 == t2) sameCount++;
                }
                // Should not all be the same (would indicate selector not used)
                return sameCount < 50;
            })
            .AssertPassed();

    [Scenario("Trial keys are sorted alphabetically for determinism")]
    [Fact]
    public Task Trial_keys_sorted_alphabetically()
        => Given("unsorted trial keys", () => CreateState("user-123", "MyExperiment", "zebra", "alpha", "beta"))
            .When("select trial", SelectTrial)
            .Then("selected trial is deterministic regardless of input order", _ =>
            {
                var result1 = StickyTrialRouter.SelectTrial("user-123", "MyExperiment", ["zebra", "alpha", "beta"]);
                var result2 = StickyTrialRouter.SelectTrial("user-123", "MyExperiment", ["beta", "zebra", "alpha"]);
                var result3 = StickyTrialRouter.SelectTrial("user-123", "MyExperiment", ["alpha", "beta", "zebra"]);
                return result1 == result2 && result2 == result3;
            })
            .AssertPassed();

    [Scenario("Single trial always returns that trial")]
    [Fact]
    public Task Single_trial_always_selected()
        => Given("single trial", () => CreateState("user-123", "MyExperiment", "only-trial"))
            .When("select trial", SelectTrial)
            .Then("only trial is selected", r => r.SelectedTrial == "only-trial")
            .And("works for different identities", _ =>
            {
                var r1 = StickyTrialRouter.SelectTrial("user-1", "MyExperiment", ["only-trial"]);
                var r2 = StickyTrialRouter.SelectTrial("user-2", "MyExperiment", ["only-trial"]);
                return r1 == "only-trial" && r2 == "only-trial";
            })
            .AssertPassed();

    [Scenario("Empty trial list throws exception")]
    [Fact]
    public void Empty_trial_list_throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            StickyTrialRouter.SelectTrial("user-123", "MyExperiment", []));

        Assert.Equal("No trial keys available for sticky routing.", exception.Message);
    }

    [Scenario("Three trials distribute users evenly")]
    [Fact]
    public Task Three_trials_distribute_evenly()
        => Given("150 users with three trials", () => Enumerable.Range(1, 150).Select(i => $"user-{i}").ToList())
            .When("select trials for all users", identities =>
            {
                var selections = identities
                    .Select(id => StickyTrialRouter.SelectTrial(id, "ThreeWayTest", ["control", "variant-a", "variant-b"]))
                    .ToList();
                return selections;
            })
            .Then("all three trials are used", selections =>
            {
                var trialCounts = selections.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
                return trialCounts.ContainsKey("control") &&
                       trialCounts.ContainsKey("variant-a") &&
                       trialCounts.ContainsKey("variant-b");
            })
            .And("distribution is reasonably balanced", selections =>
            {
                var trialCounts = selections.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
                var controlCount = trialCounts.GetValueOrDefault("control", 0);
                var aCount = trialCounts.GetValueOrDefault("variant-a", 0);
                var bCount = trialCounts.GetValueOrDefault("variant-b", 0);
                // Each should be roughly 33%, allow 20-47% for randomness
                return controlCount >= 30 && controlCount <= 70 &&
                       aCount >= 30 && aCount <= 70 &&
                       bCount >= 30 && bCount <= 70;
            })
            .AssertPassed();
}

/// <summary>
/// Tests for the StickyRoutingProvider and registration infrastructure.
/// </summary>
public sealed class StickyRoutingProviderTests
{
    private sealed class TestIdentityProvider(string identity) : IExperimentIdentityProvider
    {
        public bool TryGetIdentity(out string id)
        {
            id = identity;
            return !string.IsNullOrEmpty(identity);
        }
    }

    #region StickyRoutingProvider Tests

    [Fact]
    public void StickyRoutingProvider_has_correct_mode_identifier()
    {
        var provider = new StickyRoutingProvider();

        Assert.Equal("StickyRouting", provider.ModeIdentifier);
    }

    [Fact]
    public async Task StickyRoutingProvider_SelectTrialKeyAsync_returns_null_without_identity_provider()
    {
        var provider = new StickyRoutingProvider();
        var services = new ServiceCollection().BuildServiceProvider();

        var context = new SelectionContext
        {
            ServiceProvider = services,
            SelectorName = "test",
            TrialKeys = ["control", "variant"],
            DefaultKey = "control",
            ServiceType = typeof(ITestService)
        };

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Fact]
    public async Task StickyRoutingProvider_SelectTrialKeyAsync_returns_key_with_identity()
    {
        var provider = new StickyRoutingProvider();
        var services = new ServiceCollection();
        services.AddScoped<IExperimentIdentityProvider>(_ => new TestIdentityProvider("user-123"));
        var sp = services.BuildServiceProvider();

        var context = new SelectionContext
        {
            ServiceProvider = sp,
            SelectorName = "test",
            TrialKeys = ["control", "variant"],
            DefaultKey = "control",
            ServiceType = typeof(ITestService)
        };

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.NotNull(result);
        Assert.Contains(result, context.TrialKeys);
    }

    [Fact]
    public void StickyRoutingProvider_GetDefaultSelectorName_uses_convention()
    {
        var provider = new StickyRoutingProvider();
        var convention = new DefaultExperimentNamingConvention();

        var name = provider.GetDefaultSelectorName(typeof(ITestService), convention);

        Assert.NotEmpty(name);
    }

    #endregion

    #region Registration Tests

    [Fact]
    public void AddExperimentStickyRouting_registers_provider()
    {
        var services = new ServiceCollection();
        services.AddExperimentStickyRouting();

        var sp = services.BuildServiceProvider();
        var factory = sp.GetService<ISelectionModeProviderFactory>();

        Assert.NotNull(factory);
        Assert.Equal("StickyRouting", factory!.ModeIdentifier);
    }

    #endregion

    #region Extension Method Tests

    [Fact]
    public void UsingStickyRouting_extension_method_works()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddExperimentStickyRouting();
        services.AddScoped<IExperimentIdentityProvider>(_ => new TestIdentityProvider("user-123"));
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<IVariantService>(t => t
                .UsingStickyRouting("test-experiment")
                .AddControl<ControlVariant>()
                .AddCondition<VariantA>("variant-a")
                .AddCondition<VariantB>("variant-b"))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IVariantService>();
        var result = service.GetName();

        Assert.Contains(result, new[] { "ControlVariant", "VariantA", "VariantB" });
    }

    #endregion

    #region Test Helpers

    private static void RegisterTestServices(IServiceCollection services)
    {
        services.AddScoped<ControlVariant>();
        services.AddScoped<VariantA>();
        services.AddScoped<VariantB>();
        services.AddScoped<IVariantService, ControlVariant>();
    }

    #endregion

    #region Additional Provider Tests

    [Fact]
    public async Task StickyRoutingProvider_returns_null_when_identity_not_available()
    {
        var services = new ServiceCollection();
        services.AddScoped<IExperimentIdentityProvider>(
            _ => new TestIdentityProvider("")); // Empty identity
        var sp = services.BuildServiceProvider();

        var provider = new StickyRoutingProvider();
        var context = new SelectionContext
        {
            ServiceProvider = sp,
            SelectorName = "test",
            TrialKeys = new List<string> { "control", "variant" },
            DefaultKey = "control",
            ServiceType = typeof(IDatabase)
        };

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Fact]
    public void StickyRoutingProvider_GetDefaultSelectorName_with_different_types()
    {
        var provider = new StickyRoutingProvider();
        var convention = new DefaultExperimentNamingConvention();

        var name1 = provider.GetDefaultSelectorName(typeof(IDatabase), convention);
        var name2 = provider.GetDefaultSelectorName(typeof(ITestService), convention);

        Assert.NotEqual(name1, name2);
        Assert.Contains("Database", name1);
        Assert.Contains("TestService", name2);
    }

    #endregion
}
