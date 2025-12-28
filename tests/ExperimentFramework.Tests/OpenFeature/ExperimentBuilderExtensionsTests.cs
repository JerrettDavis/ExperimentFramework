using ExperimentFramework.OpenFeature;
using ExperimentFramework.Tests.TestInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.OpenFeature;

[Feature("ExperimentBuilderExtensions provide UsingOpenFeature configuration")]
public sealed class ExperimentBuilderExtensionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("UsingOpenFeature extension configures experiment with flag key")]
    [Fact]
    public async Task UsingOpenFeature_with_flag_key()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddExperimentOpenFeature();
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<IDatabase>(t => t
                .UsingOpenFeature("my-database-flag")
                .AddControl<LocalDatabase>()
                .AddCondition<CloudDatabase>("cloud"))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();

        Assert.NotNull(db);

        await Task.CompletedTask;
    }

    [Scenario("UsingOpenFeature extension works without explicit flag key")]
    [Fact]
    public async Task UsingOpenFeature_without_flag_key()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddExperimentOpenFeature();
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<ITestService>(t => t
                .UsingOpenFeature() // No explicit flag key
                .AddControl<StableService>()
                .AddCondition<ServiceA>("a"))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITestService>();

        Assert.NotNull(service);

        await Task.CompletedTask;
    }

    [Scenario("UsingOpenFeature with multiple trials")]
    [Fact]
    public async Task UsingOpenFeature_with_multiple_trials()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddExperimentOpenFeature();
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<IVariantService>(t => t
                .UsingOpenFeature("variant-experiment")
                .AddControl<ControlVariant>()
                .AddCondition<VariantA>("variant-a")
                .AddCondition<VariantB>("variant-b"))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IVariantService>();

        Assert.NotNull(service);

        await Task.CompletedTask;
    }

    [Scenario("UsingOpenFeature can be combined with other configuration")]
    [Fact]
    public async Task UsingOpenFeature_with_decorators()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddExperimentOpenFeature();
        services.AddLogging();
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<IDatabase>(t => t
                .UsingOpenFeature("db-flag")
                .AddControl<LocalDatabase>()
                .AddCondition<CloudDatabase>("cloud"))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();

        Assert.NotNull(db);

        await Task.CompletedTask;
    }

    [Scenario("UsingOpenFeature allows method chaining")]
    [Fact]
    public Task UsingOpenFeature_allows_chaining()
        => Given("a service experiment builder", () =>
            {
                var builder = ExperimentFrameworkBuilder.Create();
                return builder;
            })
            .When("using OpenFeature with chaining", frameworkBuilder =>
            {
                var trialBuilder = frameworkBuilder.Trial<ITestService>(t => t
                    .UsingOpenFeature("test-flag")
                    .AddControl<StableService>()
                    .AddCondition<ServiceA>("a")
                    .AddCondition<ServiceB>("b"));
                return trialBuilder;
            })
            .Then("builder is returned for chaining", builder => builder != null)
            .AssertPassed();

    [Scenario("UsingOpenFeature with null flag key uses naming convention")]
    [Fact]
    public async Task UsingOpenFeature_with_null_uses_convention()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddExperimentOpenFeature();
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<ITaxProvider>(t => t
                .UsingOpenFeature(null) // Explicitly passing null
                .AddControl<DefaultTaxProvider>()
                .AddCondition<OkTaxProvider>("ok"))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<ITaxProvider>();

        Assert.NotNull(provider);

        await Task.CompletedTask;
    }

    [Scenario("Multiple experiments can use OpenFeature")]
    [Fact]
    public async Task Multiple_experiments_use_open_feature()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddExperimentOpenFeature();
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<IDatabase>(t => t
                .UsingOpenFeature("db-flag")
                .AddControl<LocalDatabase>()
                .AddCondition<CloudDatabase>("cloud"))
            .Trial<ITestService>(t => t
                .UsingOpenFeature("test-flag")
                .AddControl<StableService>()
                .AddCondition<ServiceA>("a"))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();
        var testService = scope.ServiceProvider.GetRequiredService<ITestService>();

        Assert.NotNull(db);
        Assert.NotNull(testService);

        await Task.CompletedTask;
    }

    [Scenario("UsingOpenFeature with empty string flag key")]
    [Fact]
    public async Task UsingOpenFeature_with_empty_flag_key()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddExperimentOpenFeature();
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<IMyService>(t => t
                .UsingOpenFeature("") // Empty string
                .AddControl<MyServiceV1>()
                .AddCondition<MyServiceV2>("v2"))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IMyService>();

        Assert.NotNull(service);

        await Task.CompletedTask;
    }

    [Scenario("UsingOpenFeature resolves default trial when no provider")]
    [Fact]
    public async Task UsingOpenFeature_resolves_default_without_provider()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddExperimentOpenFeature();
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<IDatabase>(t => t
                .UsingOpenFeature("db-experiment")
                .AddControl<LocalDatabase>()
                .AddCondition<CloudDatabase>("cloud"))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();

        // Without OpenFeature provider configured, should use default (control)
        var name = db.GetName();
        Assert.Equal("LocalDatabase", name);

        await Task.CompletedTask;
    }

    #region Helper Methods

    private static void RegisterTestServices(IServiceCollection services)
    {
        // Database implementations - register interface with default implementation
        services.AddScoped<IDatabase, LocalDatabase>();
        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();

        // Test service implementations - register interface with default implementation
        services.AddScoped<ITestService, StableService>();
        services.AddScoped<StableService>();
        services.AddScoped<ServiceA>();
        services.AddScoped<ServiceB>();

        // Variant implementations - register interface with default implementation
        services.AddScoped<IVariantService, ControlVariant>();
        services.AddScoped<ControlVariant>();
        services.AddScoped<VariantA>();
        services.AddScoped<VariantB>();

        // Tax provider implementations - register interface with default implementation
        services.AddScoped<ITaxProvider, DefaultTaxProvider>();
        services.AddScoped<DefaultTaxProvider>();
        services.AddScoped<OkTaxProvider>();

        // MyService implementations - register interface with default implementation
        services.AddScoped<IMyService, MyServiceV1>();
        services.AddScoped<MyServiceV1>();
        services.AddScoped<MyServiceV2>();
    }

    #endregion
}
