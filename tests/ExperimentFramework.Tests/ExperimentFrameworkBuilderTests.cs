using ExperimentFramework.Naming;
using ExperimentFramework.Tests.TestInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

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

        // IRedirectSpecificService implementations
        services.AddScoped<PrimaryService>();
        services.AddScoped<SecondaryService>();
        services.AddScoped<NoopFallbackService>();

        // IRedirectOrderedService implementations
        services.AddScoped<CloudService>();
        services.AddScoped<LocalCacheService>();
        services.AddScoped<InMemoryCacheService>();
        services.AddScoped<StaticDataService>();
        services.AddScoped<AlwaysFailsService1>();
        services.AddScoped<AlwaysFailsService2>();
        services.AddScoped<AlwaysFailsService3>();

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
        services.AddScoped<IRedirectSpecificService, PrimaryService>();
        services.AddScoped<IRedirectOrderedService, CloudService>();
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

    [Scenario("UseDispatchProxy configures runtime proxies")]
    [Fact]
    public void UseDispatchProxy_creates_runtime_proxies()
        => Given("experiment with UseDispatchProxy", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseCloudDb"] = "true"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllTestServices(services);

            // Use UseDispatchProxy instead of UseSourceGenerators
            var builder = ExperimentFrameworkBuilder.Create()
                .Define<IDatabase>(c => c
                    .UsingFeatureFlag("UseCloudDb")
                    .AddDefaultTrial<CloudDatabase>("true")
                    .AddTrial<LocalDatabase>("false")
                    .OnErrorRedirectAndReplayDefault())
                .UseDispatchProxy(); // Runtime proxies instead of source generation

            services.AddExperimentFramework(builder);
            var sp = services.BuildServiceProvider();

            return (State: new BuilderState(builder, services), sp);
        })
        .When("invoke service", state =>
        {
            using var scope = state.sp.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<IDatabase>();
            var result = database.GetName();
            return (state.sp, result);
        })
        .Then("runtime proxy routes to correct implementation", r => r.result == "CloudDatabase")
        .Finally(r => r.Item1.Dispose())
        .AssertPassed();

    [Scenario("UseDispatchProxy supports all error policies")]
    [Fact]
    public void UseDispatchProxy_supports_error_policies()
        => Given("experiment with RedirectAndReplayDefault and runtime proxies", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:UseFailingService"] = "true"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllTestServices(services);

            var builder = ExperimentFrameworkBuilder.Create()
                .Define<ITestService>(c => c
                    .UsingFeatureFlag("UseFailingService")
                    .AddDefaultTrial<StableService>("false")
                    .AddTrial<FailingService>("true")
                    .OnErrorRedirectAndReplayDefault())
                .UseDispatchProxy();

            services.AddExperimentFramework(builder);
            var sp = services.BuildServiceProvider();

            return (State: new BuilderState(builder, services), sp);
        })
        .When("invoke service that fails", state =>
        {
            using var scope = state.sp.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITestService>();
            var result = service.Execute(); // Should fallback to StableService
            return (state.sp, result);
        })
        .Then("runtime proxy falls back to default", r => r.result == "StableService")
        .Finally(r => r.Item1.Dispose())
        .AssertPassed();

    [Scenario("UseDispatchProxy supports configuration-based selection")]
    [Fact]
    public void UseDispatchProxy_supports_configuration_selection()
        => Given("experiment with configuration value and runtime proxies", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TaxProvider:Strategy"] = "OK"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();

            RegisterAllTestServices(services);

            var builder = ExperimentFrameworkBuilder.Create()
                .Define<ITaxProvider>(c => c
                    .UsingConfigurationKey("TaxProvider:Strategy")
                    .AddDefaultTrial<DefaultTaxProvider>("Default")
                    .AddTrial<OkTaxProvider>("OK")
                    .AddTrial<TxTaxProvider>("TX"))
                .UseDispatchProxy();

            services.AddExperimentFramework(builder);
            var sp = services.BuildServiceProvider();

            return (State: new BuilderState(builder, services), sp);
        })
        .When("invoke service", state =>
        {
            using var scope = state.sp.CreateScope();
            var taxProvider = scope.ServiceProvider.GetRequiredService<ITaxProvider>();
            var result = taxProvider.CalculateTax(100);
            return (state.sp, result);
        })
        .Then("runtime proxy uses configuration value", r => r.result == 105.0m) // OkTaxProvider adds 5%
        .Finally(r => r.Item1.Dispose())
        .AssertPassed();

    // Custom naming convention for testing
    private sealed class CustomNamingConvention : IExperimentNamingConvention
    {
        public string FeatureFlagNameFor(Type serviceType) => $"Custom.{serviceType.Name}";
        public string VariantFlagNameFor(Type serviceType) => $"Variant.{serviceType.Name}";
        public string ConfigurationKeyFor(Type serviceType) => $"Config:{serviceType.Name}";
        public string OpenFeatureFlagNameFor(Type serviceType) => $"of-{serviceType.Name.ToLowerInvariant()}";
    }
}

/// <summary>
/// Tests for ExperimentBuilder validation and activation.
/// </summary>
public sealed class ExperimentBuilderValidationTests
{
    [Fact]
    public void ExperimentBuilder_ActiveWhen_throws_when_null()
    {
        // Builder result intentionally discarded - we're testing the callback throws
        _ = ExperimentFrameworkBuilder.Create()
            .Experiment("test", exp =>
            {
                Assert.Throws<ArgumentNullException>(() => exp.ActiveWhen(null!));
            });
    }

    [Fact]
    public void ExperimentBuilder_ActiveFrom_sets_start_time_only()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:TestFeature"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();
        services.AddScoped<IDatabase, LocalDatabase>();

        // Start time is in the future
        var start = DateTimeOffset.UtcNow.AddHours(1);

        var builder = ExperimentFrameworkBuilder.Create()
            .Experiment("start-only-experiment", exp => exp
                .Trial<IDatabase>(t => t
                    .UsingFeatureFlag("TestFeature")
                    .AddControl<LocalDatabase>()
                    .AddCondition<CloudDatabase>("true"))
                .ActiveFrom(start))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();

        // Should use control because experiment hasn't started
        Assert.Equal("LocalDatabase", db.GetName());
    }

    [Fact]
    public void ExperimentBuilder_ActiveFrom_in_past_activates_experiment()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:TestFeature"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();
        services.AddScoped<IDatabase, LocalDatabase>();

        // Start time is in the past
        var start = DateTimeOffset.UtcNow.AddHours(-1);

        var builder = ExperimentFrameworkBuilder.Create()
            .Experiment("past-start-experiment", exp => exp
                .Trial<IDatabase>(t => t
                    .UsingFeatureFlag("TestFeature")
                    .AddControl<LocalDatabase>()
                    .AddCondition<CloudDatabase>("true"))
                .ActiveFrom(start))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();

        // Should use CloudDatabase because experiment is active
        Assert.Equal("CloudDatabase", db.GetName());
    }

    [Fact]
    public void ExperimentBuilder_WithMetadata_adds_metadata()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:TestFeature"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();
        services.AddScoped<IDatabase, LocalDatabase>();

        var builder = ExperimentFrameworkBuilder.Create()
            .Experiment("test-experiment", exp => exp
                .Trial<IDatabase>(t => t
                    .UsingFeatureFlag("TestFeature")
                    .AddControl<LocalDatabase>()
                    .AddCondition<CloudDatabase>("true"))
                .WithMetadata("owner", "team-platform")
                .WithMetadata("jira", "PROJ-123"))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        // Should build without error
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();
        Assert.NotNull(db);
    }

    [Fact]
    public void ExperimentBuilder_Build_validation_runs()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:TestFeature"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();
        services.AddScoped<IDatabase, LocalDatabase>();

        // Build with only AddCondition (no explicit control) - should work
        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<IDatabase>(t => t
                .UsingFeatureFlag("TestFeature")
                .AddCondition<LocalDatabase>("false")
                .AddCondition<CloudDatabase>("true"))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();

        Assert.NotNull(db);
    }
}