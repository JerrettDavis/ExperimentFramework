using ExperimentFramework.FeatureManagement;
using ExperimentFramework.Tests.TestInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.FeatureManagement;

[Feature("ExperimentBuilderExtensions provides fluent API for variant feature flag configuration")]
public sealed class ExperimentBuilderExtensionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region UsingVariantFeatureFlag Tests

    [Scenario("UsingVariantFeatureFlag configures experiment with explicit feature flag name")]
    [Fact]
    public Task UsingVariantFeatureFlag_with_explicit_name()
        => Given("an experiment framework builder", () => ExperimentFrameworkBuilder.Create())
            .When("defining experiment with UsingVariantFeatureFlag", builder =>
            {
                builder.Define<ITestService>(exp =>
                {
                    exp.UsingVariantFeatureFlag("MyVariantFlag");
                    exp.AddControl<StableService>("control");
                });
                return builder;
            })
            .Then("builder is configured successfully", b => b != null)
            .AssertPassed();

    [Scenario("UsingVariantFeatureFlag configures experiment with null feature flag name")]
    [Fact]
    public Task UsingVariantFeatureFlag_with_null_name()
        => Given("an experiment framework builder", () => ExperimentFrameworkBuilder.Create())
            .When("defining experiment with UsingVariantFeatureFlag(null)", builder =>
            {
                builder.Define<ITestService>(exp =>
                {
                    exp.UsingVariantFeatureFlag(null);
                    exp.AddControl<StableService>("control");
                });
                return builder;
            })
            .Then("builder is configured successfully", b => b != null)
            .AssertPassed();

    [Scenario("UsingVariantFeatureFlag returns builder for chaining")]
    [Fact]
    public async Task UsingVariantFeatureFlag_returns_builder_for_chaining()
    {
        var builder = ExperimentFrameworkBuilder.Create();

        builder.Define<ITestService>(exp =>
        {
            var result = exp.UsingVariantFeatureFlag("TestFlag")
                .AddControl<StableService>("control")
                .AddCondition<ServiceA>("variant-a");

            Assert.NotNull(result);
        });

        await Task.CompletedTask;
    }

    [Scenario("UsingVariantFeatureFlag with multiple trials")]
    [Fact]
    public async Task UsingVariantFeatureFlag_with_multiple_trials()
    {
        var builder = ExperimentFrameworkBuilder.Create();

        builder.Define<IVariantService>(exp =>
        {
            exp.UsingVariantFeatureFlag("MultiVariantFlag")
                .AddControl<ControlVariant>("control")
                .AddCondition<VariantA>("variant-a")
                .AddCondition<VariantB>("variant-b");
        });

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    #endregion

    #region Integration Tests

    [Scenario("UsingVariantFeatureFlag integrates with service collection")]
    [Fact]
    public async Task Integrates_with_service_collection()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:TestVariant"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        services.AddExperimentVariantFeatureFlags();

        services.AddScoped<ControlVariant>();
        services.AddScoped<VariantA>();
        services.AddScoped<IVariantService, ControlVariant>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<IVariantService>(c => c
                .UsingVariantFeatureFlag("TestVariant")
                .AddDefaultTrial<ControlVariant>("control")
                .AddTrial<VariantA>("variant-a"));

        services.AddExperimentFramework(experiments);

        await using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IVariantService>();
        var result = service.GetName();

        // Without variant allocation, falls back to default
        Assert.Equal("ControlVariant", result);
    }

    [Scenario("UsingVariantFeatureFlag uses default naming convention when name is null")]
    [Fact]
    public async Task Uses_default_naming_convention_when_null()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        services.AddExperimentVariantFeatureFlags();

        services.AddScoped<ControlVariant>();
        services.AddScoped<IVariantService, ControlVariant>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<IVariantService>(c => c
                .UsingVariantFeatureFlag() // No name - uses convention
                .AddDefaultTrial<ControlVariant>("control"));

        services.AddExperimentFramework(experiments);

        await using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IVariantService>();
        var result = service.GetName();

        Assert.Equal("ControlVariant", result);
    }

    [Scenario("UsingVariantFeatureFlag works with scoped services")]
    [Fact]
    public async Task Works_with_scoped_services()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:ScopedVariant"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        services.AddExperimentVariantFeatureFlags();

        services.AddScoped<ControlVariant>();
        services.AddScoped<IVariantService, ControlVariant>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<IVariantService>(c => c
                .UsingVariantFeatureFlag("ScopedVariant")
                .AddDefaultTrial<ControlVariant>("control"));

        services.AddExperimentFramework(experiments);

        await using var sp = services.BuildServiceProvider();

        var results = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            using var scope = sp.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IVariantService>();
            results.Add(service.GetName());
        }

        // All scopes should consistently return the same result
        Assert.All(results, r => Assert.Equal("ControlVariant", r));
    }

    #endregion

    #region Edge Cases

    [Scenario("UsingVariantFeatureFlag with empty string feature flag name")]
    [Fact]
    public async Task With_empty_string_feature_flag_name()
    {
        var builder = ExperimentFrameworkBuilder.Create();

        builder.Define<ITestService>(exp =>
        {
            exp.UsingVariantFeatureFlag("");
            exp.AddControl<StableService>("control");
        });

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    [Scenario("UsingVariantFeatureFlag with whitespace feature flag name")]
    [Fact]
    public async Task With_whitespace_feature_flag_name()
    {
        var builder = ExperimentFrameworkBuilder.Create();

        builder.Define<ITestService>(exp =>
        {
            exp.UsingVariantFeatureFlag("   ");
            exp.AddControl<StableService>("control");
        });

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    [Scenario("UsingVariantFeatureFlag with special characters in name")]
    [Fact]
    public async Task With_special_characters_in_name()
    {
        var builder = ExperimentFrameworkBuilder.Create();

        builder.Define<ITestService>(exp =>
        {
            exp.UsingVariantFeatureFlag("Feature:With:Colons");
            exp.AddControl<StableService>("control");
        });

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    [Scenario("UsingVariantFeatureFlag with long feature flag name")]
    [Fact]
    public async Task With_long_feature_flag_name()
    {
        var longName = new string('A', 1000);
        var builder = ExperimentFrameworkBuilder.Create();

        builder.Define<ITestService>(exp =>
        {
            exp.UsingVariantFeatureFlag(longName);
            exp.AddControl<StableService>("control");
        });

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    #endregion

    #region Chaining Tests

    [Scenario("UsingVariantFeatureFlag chains with AddDefaultTrial")]
    [Fact]
    public async Task Chains_with_AddDefaultTrial()
    {
        var builder = ExperimentFrameworkBuilder.Create();

        builder.Define<ITestService>(exp =>
        {
            exp.UsingVariantFeatureFlag("ChainTest")
                .AddDefaultTrial<StableService>("default");
        });

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    [Scenario("UsingVariantFeatureFlag chains with AddTrial")]
    [Fact]
    public async Task Chains_with_AddTrial()
    {
        var builder = ExperimentFrameworkBuilder.Create();

        builder.Define<ITestService>(exp =>
        {
            exp.UsingVariantFeatureFlag("ChainTest")
                .AddDefaultTrial<StableService>("default")
                .AddTrial<ServiceA>("variant");
        });

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    [Scenario("UsingVariantFeatureFlag chains with AddControl and AddCondition")]
    [Fact]
    public async Task Chains_with_AddControl_and_AddCondition()
    {
        var builder = ExperimentFrameworkBuilder.Create();

        builder.Define<ITestService>(exp =>
        {
            exp.UsingVariantFeatureFlag("ChainTest")
                .AddControl<StableService>("control")
                .AddCondition<ServiceA>("treatment-a")
                .AddCondition<ServiceB>("treatment-b");
        });

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    #endregion

    #region Multiple Experiments Tests

    [Scenario("Multiple experiments can use UsingVariantFeatureFlag")]
    [Fact]
    public async Task Multiple_experiments_use_variant_feature_flag()
    {
        var builder = ExperimentFrameworkBuilder.Create();

        builder.Define<ITestService>(exp =>
        {
            exp.UsingVariantFeatureFlag("TestServiceVariant")
                .AddControl<StableService>("control");
        });

        builder.Define<IVariantService>(exp =>
        {
            exp.UsingVariantFeatureFlag("VariantServiceVariant")
                .AddControl<ControlVariant>("control");
        });

        Assert.NotNull(builder);

        await Task.CompletedTask;
    }

    [Scenario("Different experiments can use different feature flag names")]
    [Fact]
    public async Task Different_experiments_different_flag_names()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        services.AddExperimentVariantFeatureFlags();

        services.AddScoped<StableService>();
        services.AddScoped<ControlVariant>();
        services.AddScoped<ITestService, StableService>();
        services.AddScoped<IVariantService, ControlVariant>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<ITestService>(c => c
                .UsingVariantFeatureFlag("TestFlag")
                .AddDefaultTrial<StableService>("control"))
            .Define<IVariantService>(c => c
                .UsingVariantFeatureFlag("VariantFlag")
                .AddDefaultTrial<ControlVariant>("control"));

        services.AddExperimentFramework(experiments);

        await using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();

        var testService = scope.ServiceProvider.GetRequiredService<ITestService>();
        var variantService = scope.ServiceProvider.GetRequiredService<IVariantService>();

        Assert.Equal("StableService", testService.Execute());
        Assert.Equal("ControlVariant", variantService.GetName());
    }

    #endregion

    #region UseDispatchProxy Tests

    [Scenario("UsingVariantFeatureFlag works with UseDispatchProxy")]
    [Fact]
    public async Task Works_with_UseDispatchProxy()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        services.AddExperimentVariantFeatureFlags();

        services.AddScoped<ControlVariant>();
        services.AddScoped<IVariantService, ControlVariant>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<IVariantService>(c => c
                .UsingVariantFeatureFlag("ProxyTest")
                .AddDefaultTrial<ControlVariant>("control"))
            .UseDispatchProxy();

        services.AddExperimentFramework(experiments);

        await using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IVariantService>();

        Assert.NotNull(service);
        Assert.Equal("ControlVariant", service.GetName());
    }

    #endregion
}
