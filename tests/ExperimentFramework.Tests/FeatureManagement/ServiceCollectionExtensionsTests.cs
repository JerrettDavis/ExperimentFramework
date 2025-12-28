using ExperimentFramework.FeatureManagement;
using ExperimentFramework.Selection;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.FeatureManagement;

[Feature("ServiceCollectionExtensions registers variant feature flag support")]
public sealed class ServiceCollectionExtensionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region AddExperimentVariantFeatureFlags Tests

    [Scenario("AddExperimentVariantFeatureFlags registers ISelectionModeProviderFactory")]
    [Fact]
    public Task Registers_selection_mode_provider_factory()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding experiment variant feature flags", services =>
            {
                services.AddExperimentVariantFeatureFlags();
                return services.BuildServiceProvider();
            })
            .Then("ISelectionModeProviderFactory is registered", sp =>
            {
                var factory = sp.GetService<ISelectionModeProviderFactory>();
                return factory != null;
            })
            .AssertPassed();

    [Scenario("Registered factory has correct mode identifier")]
    [Fact]
    public Task Factory_has_correct_mode_identifier()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding experiment variant feature flags", services =>
            {
                services.AddExperimentVariantFeatureFlags();
                return services.BuildServiceProvider();
            })
            .Then("factory mode identifier is 'VariantFeatureFlag'", sp =>
            {
                var factory = sp.GetService<ISelectionModeProviderFactory>();
                return factory?.ModeIdentifier == VariantFeatureFlagModes.VariantFeatureFlag;
            })
            .AssertPassed();

    [Scenario("Factory creates VariantFeatureFlagProvider")]
    [Fact]
    public Task Factory_creates_correct_provider()
        => Given("a service collection with variant feature flags registered", () =>
            {
                var services = new ServiceCollection();
                services.AddExperimentVariantFeatureFlags();
                return services.BuildServiceProvider();
            })
            .When("creating provider from factory", sp =>
            {
                var factory = sp.GetRequiredService<ISelectionModeProviderFactory>();
                return factory.Create(sp);
            })
            .Then("provider is VariantFeatureFlagProvider", provider => provider is VariantFeatureFlagProvider)
            .AssertPassed();

    [Scenario("AddExperimentVariantFeatureFlags returns service collection for chaining")]
    [Fact]
    public Task Returns_service_collection_for_chaining()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding experiment variant feature flags", services => services.AddExperimentVariantFeatureFlags())
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("AddExperimentVariantFeatureFlags can be called multiple times")]
    [Fact]
    public Task Can_be_called_multiple_times()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding experiment variant feature flags twice", services =>
            {
                services.AddExperimentVariantFeatureFlags();
                services.AddExperimentVariantFeatureFlags();
                return services.BuildServiceProvider();
            })
            .Then("service provider is built successfully", sp => sp != null)
            .AssertPassed();

    [Scenario("Multiple registrations result in multiple factories")]
    [Fact]
    public async Task Multiple_registrations_create_multiple_factories()
    {
        var services = new ServiceCollection();
        services.AddExperimentVariantFeatureFlags();
        services.AddExperimentVariantFeatureFlags();
        var sp = services.BuildServiceProvider();

        var factories = sp.GetServices<ISelectionModeProviderFactory>().ToList();

        // Each call adds a new factory registration
        Assert.Equal(2, factories.Count);
        Assert.All(factories, f => Assert.Equal(VariantFeatureFlagModes.VariantFeatureFlag, f.ModeIdentifier));

        await Task.CompletedTask;
    }

    #endregion

    #region Integration with Other Registrations

    [Scenario("Works alongside other selection mode providers")]
    [Fact]
    public async Task Works_with_other_providers()
    {
        var services = new ServiceCollection();
        services.AddExperimentVariantFeatureFlags();
        // Simulate adding another provider would go here
        // For now, just verify our registration doesn't interfere
        var sp = services.BuildServiceProvider();

        var factories = sp.GetServices<ISelectionModeProviderFactory>().ToList();

        Assert.NotEmpty(factories);
        Assert.Contains(factories, f => f.ModeIdentifier == VariantFeatureFlagModes.VariantFeatureFlag);

        await Task.CompletedTask;
    }

    [Scenario("Created provider has correct ModeIdentifier")]
    [Fact]
    public Task Created_provider_has_correct_mode_identifier()
        => Given("a configured service provider", () =>
            {
                var services = new ServiceCollection();
                services.AddExperimentVariantFeatureFlags();
                return services.BuildServiceProvider();
            })
            .When("creating provider from factory", sp =>
            {
                var factory = sp.GetRequiredService<ISelectionModeProviderFactory>();
                return factory.Create(sp);
            })
            .Then("provider ModeIdentifier is 'VariantFeatureFlag'",
                provider => provider.ModeIdentifier == VariantFeatureFlagModes.VariantFeatureFlag)
            .AssertPassed();

    #endregion

    #region Factory Creation Tests

    [Scenario("Factory can be resolved from scoped provider")]
    [Fact]
    public async Task Factory_resolved_from_scoped_provider()
    {
        var services = new ServiceCollection();
        services.AddExperimentVariantFeatureFlags();
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var factory = scope.ServiceProvider.GetService<ISelectionModeProviderFactory>();

        Assert.NotNull(factory);
        Assert.Equal(VariantFeatureFlagModes.VariantFeatureFlag, factory.ModeIdentifier);

        await Task.CompletedTask;
    }

    [Scenario("Provider created by factory is new instance each time")]
    [Fact]
    public async Task Provider_is_new_instance_each_time()
    {
        var services = new ServiceCollection();
        services.AddExperimentVariantFeatureFlags();
        var sp = services.BuildServiceProvider();

        var factory = sp.GetRequiredService<ISelectionModeProviderFactory>();
        var provider1 = factory.Create(sp);
        var provider2 = factory.Create(sp);

        Assert.NotSame(provider1, provider2);

        await Task.CompletedTask;
    }

    #endregion

    #region Null Handling Tests

    [Scenario("Extension method throws on null service collection")]
    [Fact]
    public void Throws_on_null_service_collection()
    {
        IServiceCollection? services = null;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            services!.AddExperimentVariantFeatureFlags());

        Assert.Equal("services", exception.ParamName);
    }

    #endregion

    #region ServiceDescriptor Tests

    [Scenario("Registration uses singleton lifetime")]
    [Fact]
    public void Registration_uses_singleton_lifetime()
    {
        var services = new ServiceCollection();
        services.AddExperimentVariantFeatureFlags();

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ISelectionModeProviderFactory));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Scenario("Registration uses correct implementation type")]
    [Fact]
    public void Registration_uses_correct_implementation_type()
    {
        var services = new ServiceCollection();
        services.AddExperimentVariantFeatureFlags();

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ISelectionModeProviderFactory));

        Assert.NotNull(descriptor);
        Assert.Equal(typeof(SelectionModeProviderFactory<VariantFeatureFlagProvider>), descriptor.ImplementationType);
    }

    #endregion
}
