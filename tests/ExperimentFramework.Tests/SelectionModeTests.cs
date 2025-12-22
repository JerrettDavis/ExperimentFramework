using ExperimentFramework.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;
using ExperimentFramework.Tests.TestInterfaces;

namespace ExperimentFramework.Tests;

[Feature("Selection modes determine how trials are chosen at runtime")]
public sealed class SelectionModeTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record TestState(
        ServiceProvider ServiceProvider);

    private sealed record InvocationResult<T>(
        TestState State,
        T Result);

    // Helper to register all test services required by composition root
    private static void RegisterAllTestServices(IServiceCollection services)
    {
        // ITestService implementations
        services.AddScoped<StableService>();
        services.AddScoped<FailingService>();
        services.AddScoped<UnstableService>();
        services.AddScoped<AlsoFailingService>();
        services.AddScoped<ServiceA>();
        services.AddScoped<ServiceB>();

        // IDatabase implementations
        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();
        services.AddScoped<ControlDatabase>();
        services.AddScoped<ExperimentalDatabase>();

        // ITaxProvider implementations
        services.AddScoped<DefaultTaxProvider>();
        services.AddScoped<OkTaxProvider>();
        services.AddScoped<TxTaxProvider>();

        // IVariantService implementations
        services.AddScoped<ControlVariant>();
        services.AddScoped<ControlImpl>();
        services.AddScoped<VariantA>();
        services.AddScoped<VariantB>();

        // IMyService implementations
        services.AddScoped<MyServiceV1>();
        services.AddScoped<MyServiceV2>();

        // IOtherService implementations
        services.AddScoped<ServiceC>();
        services.AddScoped<ServiceD>();

        // IVariantTestService implementations
        services.AddScoped<ControlService>();
        services.AddScoped<VariantAService>();
        services.AddScoped<VariantBService>();

        // IAsyncService implementations
        services.AddScoped<AsyncServiceV1>();
        services.AddScoped<AsyncServiceV2>();

        // IGenericRepository implementations
        services.AddScoped<GenericRepositoryV1<TestEntity>>();
        services.AddScoped<GenericRepositoryV2<TestEntity>>();

        // INestedGenericService implementations
        services.AddScoped<NestedGenericServiceV1>();
        services.AddScoped<NestedGenericServiceV2>();

        // Default registrations
        services.AddScoped<ITestService, StableService>();
        services.AddScoped<IDatabase, LocalDatabase>();
        services.AddScoped<ITaxProvider, DefaultTaxProvider>();
        services.AddScoped<IVariantService, ControlVariant>();
        services.AddScoped<IMyService, MyServiceV1>();
        services.AddScoped<IOtherService, ServiceC>();
        services.AddScoped<IVariantTestService, ControlService>();
        services.AddScoped<IAsyncService, AsyncServiceV1>();
        services.AddScoped<IGenericRepository<TestEntity>, GenericRepositoryV1<TestEntity>>();
        services.AddScoped<INestedGenericService, NestedGenericServiceV1>();
    }

    private static TestState SetupBooleanFeatureFlag(bool featureEnabled)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseCloudDb"] = featureEnabled.ToString()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();

        RegisterAllTestServices(services);

        var experiments = ExperimentTestCompositionRoot.ConfigureTestExperiments();

        services.AddExperimentFramework(experiments);

        return new TestState(services.BuildServiceProvider());
    }

    private static TestState SetupConfigurationValue(string configValue)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TaxProvider"] = configValue
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);

        RegisterAllTestServices(services);

        var experiments = ExperimentTestCompositionRoot.ConfigureTestExperiments();

        services.AddExperimentFramework(experiments);

        return new TestState(services.BuildServiceProvider());
    }

    private static TestState SetupStickyRouting(string identity)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UserVariant"] = "false" // Fallback if no identity
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        services.AddScoped<IExperimentIdentityProvider>(_ => new TestIdentityProvider(identity));

        RegisterAllTestServices(services);

        var experiments = ExperimentTestCompositionRoot.ConfigureTestExperiments();

        services.AddExperimentFramework(experiments);

        return new TestState(services.BuildServiceProvider());
    }

    private static InvocationResult<string> InvokeDatabase(TestState state)
    {
        using var scope = state.ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();
        var result = db.GetName();
        return new InvocationResult<string>(state, result);
    }

    private static InvocationResult<decimal> InvokeTaxProvider(TestState state)
    {
        using var scope = state.ServiceProvider.CreateScope();
        var tax = scope.ServiceProvider.GetRequiredService<ITaxProvider>();
        var result = tax.CalculateTax(100m);
        return new InvocationResult<decimal>(state, result);
    }

    private static InvocationResult<string> InvokeTestService(TestState state)
    {
        using var scope = state.ServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IVariantService>();
        var result = service.GetName();
        return new InvocationResult<string>(state, result);
    }

    [Scenario("Boolean feature flag routes to control when disabled")]
    [Fact]
    public Task Boolean_flag_false_routes_to_control()
        => Given("feature flag disabled", () => SetupBooleanFeatureFlag(false))
            .When("invoke database service", InvokeDatabase)
            .Then("local database is used", r => r.Result == "LocalDatabase")
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Boolean feature flag routes to experiment when enabled")]
    [Fact]
    public Task Boolean_flag_true_routes_to_experiment()
        => Given("feature flag enabled", () => SetupBooleanFeatureFlag(true))
            .When("invoke database service", InvokeDatabase)
            .Then("cloud database is used", r => r.Result == "CloudDatabase")
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Configuration value routes to default when empty")]
    [Fact]
    public Task Config_empty_routes_to_default()
        => Given("empty configuration value", () => SetupConfigurationValue(""))
            .When("invoke tax provider", InvokeTaxProvider)
            .Then("default tax provider is used", r => r.Result == 0m)
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Configuration value routes to OK provider")]
    [Fact]
    public Task Config_OK_routes_to_OK_provider()
        => Given("configuration value set to OK", () => SetupConfigurationValue("OK"))
            .When("invoke tax provider", InvokeTaxProvider)
            .Then("OK tax provider is used", r => r.Result == 4.5m)
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Configuration value routes to TX provider")]
    [Fact]
    public Task Config_TX_routes_to_TX_provider()
        => Given("configuration value set to TX", () => SetupConfigurationValue("TX"))
            .When("invoke tax provider", InvokeTaxProvider)
            .Then("TX tax provider is used", r => r.Result == 6.25m)
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Sticky routing provides consistent routing for same identity")]
    [Fact]
    public Task Sticky_routing_consistent_for_same_identity()
        => Given("sticky routing with identity", () => SetupStickyRouting("user-123"))
            .When("invoke service first time", InvokeTestService)
            .Then("variant is selected", r => !string.IsNullOrEmpty(r.Result))
            .When("invoke service second time", r => InvokeTestService(r.State))
            .Then("same variant selected", r =>
            {
                var firstCall = r.Result;
                var secondCall = InvokeTestService(r.State).Result;
                return firstCall == secondCall;
            })
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Sticky routing distributes different identities")]
    [Fact]
    public Task Sticky_routing_distributes_identities()
        => Given("multiple identities", () => Enumerable.Range(1, 60).Select(i => $"user-{i}").ToList())
            .When("invoke service for each identity", identities =>
            {
                var results = new List<string>();
                foreach (var identity in identities)
                {
                    var state = SetupStickyRouting(identity);
                    var result = InvokeTestService(state);
                    results.Add(result.Result);
                    state.ServiceProvider.Dispose();
                }
                return results;
            })
            .Then("all variants are used", results =>
            {
                var variants = results.Distinct().ToList();
                return variants.Contains("ControlVariant") &&
                       variants.Contains("VariantA") &&
                       variants.Contains("VariantB");
            })
            .AssertPassed();

    // Test identity provider for sticky routing tests
    private sealed class TestIdentityProvider(string userId) : IExperimentIdentityProvider
    {
        public bool TryGetIdentity(out string identity)
        {
            identity = userId;
            return !string.IsNullOrEmpty(identity);
        }
    }
}
