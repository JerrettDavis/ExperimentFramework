using ExperimentFramework.Naming;
using ExperimentFramework.Selection;
using ExperimentFramework.StickyRouting;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.StickyRouting.Tests;

/// <summary>
/// Unit tests for <see cref="StickyRoutingProvider"/> and its DI registration helper.
/// </summary>
public sealed class StickyRoutingProviderTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private sealed class StubIdentityProvider(string identity) : IExperimentIdentityProvider
    {
        public bool TryGetIdentity(out string id)
        {
            id = identity;
            return !string.IsNullOrEmpty(identity);
        }
    }

    private sealed class ThrowingIdentityProvider : IExperimentIdentityProvider
    {
        public bool TryGetIdentity(out string id)
        {
            id = string.Empty;
            throw new InvalidOperationException("Simulated identity provider failure");
        }
    }

    private static SelectionContext BuildContext(IServiceProvider sp, params string[] keys) =>
        new()
        {
            ServiceProvider = sp,
            SelectorName = "TestExperiment",
            TrialKeys = keys.ToList(),
            DefaultKey = keys[0],
            ServiceType = typeof(IFakeService)
        };

    private interface IFakeService { }

    // -----------------------------------------------------------------------
    // ModeIdentifier
    // -----------------------------------------------------------------------

    [Fact]
    public void ModeIdentifier_is_StickyRouting()
    {
        var provider = new StickyRoutingProvider();
        Assert.Equal("StickyRouting", provider.ModeIdentifier);
    }

    [Fact]
    public void ModeIdentifier_matches_StickyRoutingModes_constant()
    {
        var provider = new StickyRoutingProvider();
        Assert.Equal(StickyRoutingModes.StickyRouting, provider.ModeIdentifier);
    }

    // -----------------------------------------------------------------------
    // SelectTrialKeyAsync — no identity provider registered
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SelectTrialKeyAsync_returns_null_when_no_identity_provider_registered()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var provider = new StickyRoutingProvider();
        var ctx = BuildContext(sp, "control", "variant");

        var result = await provider.SelectTrialKeyAsync(ctx);

        Assert.Null(result);
    }

    // -----------------------------------------------------------------------
    // SelectTrialKeyAsync — empty identity falls back to null
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SelectTrialKeyAsync_returns_null_when_identity_is_empty_string()
    {
        var services = new ServiceCollection();
        services.AddScoped<IExperimentIdentityProvider>(_ => new StubIdentityProvider(string.Empty));
        var sp = services.BuildServiceProvider();

        var provider = new StickyRoutingProvider();
        var ctx = BuildContext(sp, "control", "variant");

        var result = await provider.SelectTrialKeyAsync(ctx);

        Assert.Null(result);
    }

    // -----------------------------------------------------------------------
    // SelectTrialKeyAsync — happy path
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SelectTrialKeyAsync_returns_a_valid_trial_key_when_identity_available()
    {
        var services = new ServiceCollection();
        services.AddScoped<IExperimentIdentityProvider>(_ => new StubIdentityProvider("user-xyz"));
        var sp = services.BuildServiceProvider();

        var provider = new StickyRoutingProvider();
        var ctx = BuildContext(sp, "control", "variant");

        var result = await provider.SelectTrialKeyAsync(ctx);

        Assert.NotNull(result);
        Assert.Contains(result, ctx.TrialKeys);
    }

    [Fact]
    public async Task SelectTrialKeyAsync_is_deterministic_across_calls()
    {
        var services = new ServiceCollection();
        services.AddScoped<IExperimentIdentityProvider>(_ => new StubIdentityProvider("user-determinism-test"));
        var sp = services.BuildServiceProvider();

        var provider = new StickyRoutingProvider();
        var ctx = BuildContext(sp, "control", "variant-a", "variant-b");

        var first = await provider.SelectTrialKeyAsync(ctx);

        for (var i = 0; i < 10; i++)
        {
            var subsequent = await provider.SelectTrialKeyAsync(ctx);
            Assert.Equal(first, subsequent);
        }
    }

    // -----------------------------------------------------------------------
    // SelectTrialKeyAsync — exception from identity provider is swallowed → null
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SelectTrialKeyAsync_returns_null_when_identity_provider_throws()
    {
        var services = new ServiceCollection();
        services.AddScoped<IExperimentIdentityProvider>(_ => new ThrowingIdentityProvider());
        var sp = services.BuildServiceProvider();

        var provider = new StickyRoutingProvider();
        var ctx = BuildContext(sp, "control", "variant");

        // Should NOT propagate — the provider catches and falls back
        var result = await provider.SelectTrialKeyAsync(ctx);

        Assert.Null(result);
    }

    // -----------------------------------------------------------------------
    // GetDefaultSelectorName
    // -----------------------------------------------------------------------

    [Fact]
    public void GetDefaultSelectorName_returns_non_empty_string()
    {
        var provider = new StickyRoutingProvider();
        var convention = new DefaultExperimentNamingConvention();

        var name = provider.GetDefaultSelectorName(typeof(IFakeService), convention);

        Assert.NotEmpty(name);
    }

    [Fact]
    public void GetDefaultSelectorName_differs_for_different_types()
    {
        var provider = new StickyRoutingProvider();
        var convention = new DefaultExperimentNamingConvention();

        var name1 = provider.GetDefaultSelectorName(typeof(IFakeService), convention);
        var name2 = provider.GetDefaultSelectorName(typeof(IExperimentIdentityProvider), convention);

        Assert.NotEqual(name1, name2);
    }

    // -----------------------------------------------------------------------
    // DI registration via AddExperimentStickyRouting
    // -----------------------------------------------------------------------

    [Fact]
    public void AddExperimentStickyRouting_registers_ISelectionModeProviderFactory()
    {
        var services = new ServiceCollection();
        services.AddExperimentStickyRouting();
        var sp = services.BuildServiceProvider();

        var factory = sp.GetService<ISelectionModeProviderFactory>();

        Assert.NotNull(factory);
    }

    [Fact]
    public void AddExperimentStickyRouting_factory_has_StickyRouting_mode_identifier()
    {
        var services = new ServiceCollection();
        services.AddExperimentStickyRouting();
        var sp = services.BuildServiceProvider();

        var factory = sp.GetRequiredService<ISelectionModeProviderFactory>();

        Assert.Equal(StickyRoutingModes.StickyRouting, factory.ModeIdentifier);
    }
}
