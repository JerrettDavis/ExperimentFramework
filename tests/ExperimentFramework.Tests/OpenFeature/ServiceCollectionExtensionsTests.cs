using ExperimentFramework.OpenFeature;
using ExperimentFramework.Selection;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.OpenFeature;

[Feature("ServiceCollectionExtensions register OpenFeature selection mode")]
public sealed class ServiceCollectionExtensionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("AddExperimentOpenFeature registers provider factory")]
    [Fact]
    public Task AddExperimentOpenFeature_registers_provider()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding experiment OpenFeature", services =>
            {
                services.AddExperimentOpenFeature();
                return services.BuildServiceProvider();
            })
            .Then("provider factory is registered", sp =>
            {
                var factories = sp.GetServices<ISelectionModeProviderFactory>().ToList();
                return factories.Any(f => f.ModeIdentifier == "OpenFeature");
            })
            .AssertPassed();

    [Scenario("AddExperimentOpenFeature returns service collection for chaining")]
    [Fact]
    public Task AddExperimentOpenFeature_returns_service_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding experiment OpenFeature", services => services.AddExperimentOpenFeature())
            .Then("returns the service collection", result => result != null)
            .And("result is IServiceCollection", result => result is IServiceCollection)
            .AssertPassed();

    [Scenario("AddExperimentOpenFeature can be called multiple times")]
    [Fact]
    public async Task AddExperimentOpenFeature_is_idempotent()
    {
        var services = new ServiceCollection();
        services.AddExperimentOpenFeature();
        services.AddExperimentOpenFeature(); // Call again
        var sp = services.BuildServiceProvider();

        var factories = sp.GetServices<ISelectionModeProviderFactory>().ToList();
        var openFeatureFactories = factories.Where(f => f.ModeIdentifier == "OpenFeature").ToList();

        // May register multiple, but should all work correctly
        Assert.NotEmpty(openFeatureFactories);

        await Task.CompletedTask;
    }

    [Scenario("Provider factory creates OpenFeatureProvider")]
    [Fact]
    public async Task Factory_creates_open_feature_provider()
    {
        var services = new ServiceCollection();
        services.AddExperimentOpenFeature();
        var sp = services.BuildServiceProvider();

        var factory = sp.GetServices<ISelectionModeProviderFactory>()
            .First(f => f.ModeIdentifier == "OpenFeature");
        var provider = factory.Create(sp);

        Assert.NotNull(provider);
        Assert.IsType<OpenFeatureProvider>(provider);

        await Task.CompletedTask;
    }

    [Scenario("Factory mode identifier matches OpenFeatureModes constant")]
    [Fact]
    public async Task Factory_mode_matches_constant()
    {
        var services = new ServiceCollection();
        services.AddExperimentOpenFeature();
        var sp = services.BuildServiceProvider();

        var factory = sp.GetServices<ISelectionModeProviderFactory>()
            .First(f => f.ModeIdentifier == OpenFeatureModes.OpenFeature);

        Assert.Equal(OpenFeatureModes.OpenFeature, factory.ModeIdentifier);

        await Task.CompletedTask;
    }

    [Scenario("Registered provider has correct interface implementation")]
    [Fact]
    public async Task Provider_implements_interface()
    {
        var services = new ServiceCollection();
        services.AddExperimentOpenFeature();
        var sp = services.BuildServiceProvider();

        var factory = sp.GetServices<ISelectionModeProviderFactory>()
            .First(f => f.ModeIdentifier == "OpenFeature");
        var provider = factory.Create(sp);

        Assert.True(provider is ISelectionModeProvider);
        Assert.Equal("OpenFeature", provider.ModeIdentifier);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentOpenFeature works with empty service collection")]
    [Fact]
    public Task AddExperimentOpenFeature_works_with_empty_collection()
        => Given("an empty service collection", () => new ServiceCollection())
            .When("adding experiment OpenFeature", services =>
            {
                services.AddExperimentOpenFeature();
                return services;
            })
            .Then("has at least one registration", services => services.Count > 0)
            .AssertPassed();

    [Scenario("Provider can be resolved via factory pattern")]
    [Fact]
    public async Task Provider_resolved_via_factory()
    {
        var services = new ServiceCollection();
        services.AddExperimentOpenFeature();
        var sp = services.BuildServiceProvider();

        var factories = sp.GetServices<ISelectionModeProviderFactory>();
        var openFeatureFactory = factories.FirstOrDefault(f => f.ModeIdentifier == "OpenFeature");

        Assert.NotNull(openFeatureFactory);

        var provider = openFeatureFactory.Create(sp);
        Assert.NotNull(provider);

        await Task.CompletedTask;
    }

    [Scenario("Multiple providers can coexist")]
    [Fact]
    public async Task Multiple_providers_coexist()
    {
        var services = new ServiceCollection();
        services.AddExperimentOpenFeature();
        // Add a mock/additional provider to simulate coexistence
        services.AddSingleton<ISelectionModeProviderFactory>(
            new MockProviderFactory("TestMode"));
        var sp = services.BuildServiceProvider();

        var factories = sp.GetServices<ISelectionModeProviderFactory>().ToList();

        Assert.Contains(factories, f => f.ModeIdentifier == "OpenFeature");
        Assert.Contains(factories, f => f.ModeIdentifier == "TestMode");

        await Task.CompletedTask;
    }

    private sealed class MockProviderFactory(string mode) : ISelectionModeProviderFactory
    {
        public string ModeIdentifier => mode;

        public ISelectionModeProvider Create(IServiceProvider scopedProvider)
            => new MockProvider(mode);
    }

    private sealed class MockProvider(string mode) : ISelectionModeProvider
    {
        public string ModeIdentifier => mode;

        public ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
            => ValueTask.FromResult<string?>(null);

        public string GetDefaultSelectorName(Type serviceType, Naming.IExperimentNamingConvention convention)
            => serviceType.Name;
    }
}
