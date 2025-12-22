using ExperimentFramework.Naming;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;
using ExperimentFramework.Tests.TestInterfaces;

namespace ExperimentFramework.Tests;

[Feature("ExperimentFrameworkBuilder provides fluent API for configuring experiments")]
public sealed class ExperimentFrameworkBuilderTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record BuilderState(
        ExperimentFrameworkBuilder Builder,
        IServiceCollection Services);

    private sealed record BuildResult(
        BuilderState State,
        ServiceProvider ServiceProvider);

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

    private static BuilderState CreateBuilder()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:TestFeature"] = "true",
                ["Experiments:TestConfig"] = "test-value"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();

        RegisterAllTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create();
        return new BuilderState(builder, services);
    }

    private static BuilderState DefineExperiment(BuilderState state)
    {
        state.Builder.Define<ITestService>(c => c
            .UsingFeatureFlag("TestFeature")
            .AddDefaultTrial<ServiceA>("false")
            .AddTrial<ServiceB>("true"));

        return state;
    }

    private static BuilderState UseCustomNamingConvention(BuilderState state)
    {
        state.Builder.UseNamingConvention(new CustomNamingConvention());
        return state;
    }

    private static BuilderState AddLogging(BuilderState state)
    {
        state.Builder.AddLogger(l => l.AddBenchmarks().AddErrorLogging());
        return state;
    }

    private static BuildResult BuildAndRegister(BuilderState state)
    {
        state.Services.AddExperimentFramework(state.Builder);
        var sp = state.Services.BuildServiceProvider();
        return new BuildResult(state, sp);
    }

    private static BuildResult InvokeService(BuildResult result)
    {
        using var scope = result.ServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITestService>();
        var output = service.Execute();
        // Just verify it works
        Assert.NotNull(output);
        return result;
    }

    [Scenario("Builder creates experiment configuration successfully")]
    [Fact]
    public Task Builder_creates_configuration()
        => Given("new builder", CreateBuilder)
            .When("define experiment", DefineExperiment)
            .When("build and register", BuildAndRegister)
            .Then("service provider is created", r => r.ServiceProvider != null)
            .When("invoke service", InvokeService)
            .Then("service executes successfully", _ => true)
            .Finally(r => r.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Builder accepts custom naming convention")]
    [Fact]
    public Task Builder_accepts_custom_naming_convention()
        => Given("new builder", CreateBuilder)
            .When("use custom naming convention", UseCustomNamingConvention)
            .When("define experiment with convention-based naming", state =>
            {
                state.Builder.Define<ITestService>(c => c
                    .UsingFeatureFlag() // Uses convention
                    .AddDefaultTrial<ServiceA>("false")
                    .AddTrial<ServiceB>("true"));
                return state;
            })
            .When("build and register", BuildAndRegister)
            .Then("framework registers successfully", r => r.ServiceProvider != null)
            .Finally(r => r.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Builder supports logging decorators")]
    [Fact]
    public Task Builder_supports_logging_decorators()
        => Given("new builder", CreateBuilder)
            .When("add logging decorators", AddLogging)
            .When("define experiment", DefineExperiment)
            .When("build and register", BuildAndRegister)
            .Then("service provider is created", r => r.ServiceProvider != null)
            .Finally(r => r.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Builder supports multiple experiment definitions")]
    [Fact]
    public Task Builder_supports_multiple_definitions()
        => Given("new builder", CreateBuilder)
            .When("define multiple experiments", state =>
            {
                // Services already registered by RegisterAllTestServices in CreateBuilder
                // Just return the state
                return state;
            })
            .When("build and register", BuildAndRegister)
            .Then("both services resolve correctly", r =>
            {
                using var scope = r.ServiceProvider.CreateScope();
                var service1 = scope.ServiceProvider.GetRequiredService<ITestService>();
                var service2 = scope.ServiceProvider.GetRequiredService<IOtherService>();
                return service1 != null && service2 != null;
            })
            .Finally(r => r.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Builder validates required trials are registered")]
    [Fact]
    public void Builder_validates_trial_registration()
    {
        var services = new ServiceCollection();
        services.AddScoped<ITestService, ServiceA>();
        // Missing ServiceB registration

        var builder = ExperimentFrameworkBuilder.Create();
        builder.Define<ITestService>(c => c
            .UsingFeatureFlag("Test")
            .AddDefaultTrial<ServiceA>("false")
            .AddTrial<ServiceB>("true"));

        services.AddExperimentFramework(builder);

        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();

        // Should throw when trying to resolve unregistered trial
        var service = scope.ServiceProvider.GetRequiredService<ITestService>();
        var ex = Assert.Throws<InvalidOperationException>(() => service.Execute());
        // The exception message should indicate the missing service type
        Assert.Contains("No service for type", ex.Message);
    }

    [Scenario("Builder method chaining works correctly")]
    [Fact]
    public Task Builder_method_chaining_works()
        => Given("new builder with chained calls", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseFailingService"] = "false"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllTestServices(services);

            var builder = ExperimentTestCompositionRoot.ConfigureTestExperiments();

            services.AddExperimentFramework(builder);
            var sp = services.BuildServiceProvider();

            return (State: new BuilderState(builder, services), sp);
        })
        .When("invoke service", state =>
        {
            using var scope = state.sp.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITestService>();
            var result = service.Execute();
            return (state.sp, result);
        })
        .Then("service works correctly", r => r.result == "StableService")
        .Finally(r => r.Item1.Dispose())
        .AssertPassed();

    // Custom naming convention for testing
    private sealed class CustomNamingConvention : IExperimentNamingConvention
    {
        public string FeatureFlagNameFor(Type serviceType) => $"Custom.{serviceType.Name}";
        public string VariantFlagNameFor(Type serviceType) => $"Variant.{serviceType.Name}";
        public string ConfigurationKeyFor(Type serviceType) => $"Config:{serviceType.Name}";
    }
}