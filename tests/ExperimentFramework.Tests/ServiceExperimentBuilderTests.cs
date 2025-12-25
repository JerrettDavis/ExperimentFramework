using ExperimentFramework.Models;
using ExperimentFramework.Naming;
using ExperimentFramework.Tests.TestInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace ExperimentFramework.Tests;

/// <summary>
/// Tests for ServiceExperimentBuilder fluent API.
/// </summary>
public sealed class ServiceExperimentBuilderTests
{
    private static void RegisterTestServices(IServiceCollection services)
    {
        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();
        services.AddScoped<ControlDatabase>();
        services.AddScoped<ExperimentalDatabase>();
        services.AddScoped<IDatabase, LocalDatabase>();

        services.AddScoped<StableService>();
        services.AddScoped<FailingService>();
        services.AddScoped<ITestService, StableService>();

        services.AddScoped<DefaultTaxProvider>();
        services.AddScoped<OkTaxProvider>();
        services.AddScoped<TxTaxProvider>();
        services.AddScoped<ITaxProvider, DefaultTaxProvider>();

        services.AddScoped<PrimaryService>();
        services.AddScoped<SecondaryService>();
        services.AddScoped<NoopFallbackService>();
        services.AddScoped<IRedirectSpecificService, PrimaryService>();

        services.AddScoped<CloudService>();
        services.AddScoped<LocalCacheService>();
        services.AddScoped<InMemoryCacheService>();
        services.AddScoped<StaticDataService>();
        services.AddScoped<IRedirectOrderedService, CloudService>();
    }

    #region Selection Mode Tests

    [Fact]
    public void UsingFeatureFlag_without_name_uses_convention()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:Database"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<IDatabase>(t => t
                .UsingFeatureFlag() // No explicit name
                .AddControl<LocalDatabase>()
                .AddCondition<CloudDatabase>("true"))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();
        Assert.NotNull(db);
    }

    [Fact]
    public void UsingConfigurationKey_without_name_uses_convention()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Experiments:TaxProvider"] = "OK"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<ITaxProvider>(t => t
                .UsingConfigurationKey() // No explicit key
                .AddControl<DefaultTaxProvider>()
                .AddCondition<OkTaxProvider>("OK"))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var tax = scope.ServiceProvider.GetRequiredService<ITaxProvider>();
        Assert.NotNull(tax);
    }

    [Fact]
    public void UsingCustomMode_throws_when_modeIdentifier_empty()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            var builder = new ServiceExperimentBuilder<IDatabase>();
            builder.UsingCustomMode("", "selector");
        });
    }

    [Fact]
    public void UsingCustomMode_throws_when_modeIdentifier_null()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            var builder = new ServiceExperimentBuilder<IDatabase>();
            builder.UsingCustomMode(null!, "selector");
        });
    }

    #endregion

    #region Error Policy Tests

    [Fact]
    public void OnErrorFallbackToControl_is_alias_for_RedirectAndReplayDefault()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseFailing"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<ITestService>(t => t
                .UsingFeatureFlag("UseFailing")
                .AddControl<StableService>()
                .AddCondition<FailingService>("true")
                .OnErrorFallbackToControl()) // Alias
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITestService>();

        // Should fall back to StableService
        var result = service.Execute();
        Assert.Equal("StableService", result);
    }

    [Fact]
    public void OnErrorFallbackTo_is_alias_for_RedirectAndReplay()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UsePrimary"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        RegisterTestServices(services);
        services.AddScoped<AlwaysFailsService1>();

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<IRedirectSpecificService>(t => t
                .UsingFeatureFlag("UsePrimary")
                .AddControl<SecondaryService>()
                .AddCondition<PrimaryService>("true")
                .OnErrorFallbackTo("control")) // Alias
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRedirectSpecificService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void OnErrorTryInOrder_is_alias_for_RedirectAndReplayOrdered()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseCloud"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<IRedirectOrderedService>(t => t
                .UsingFeatureFlag("UseCloud")
                .AddControl<StaticDataService>()
                .AddCondition<CloudService>("true")
                .AddCondition<LocalCacheService>("local")
                .OnErrorTryInOrder("local", "control")) // Alias
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRedirectOrderedService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void OnErrorTryAny_is_alias_for_RedirectAndReplayAny()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseCloud"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<IRedirectOrderedService>(t => t
                .UsingFeatureFlag("UseCloud")
                .AddControl<StaticDataService>()
                .AddCondition<CloudService>("true")
                .AddCondition<LocalCacheService>("local")
                .OnErrorTryAny()) // Alias
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRedirectOrderedService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void OnErrorThrow_sets_throw_policy()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseFailing"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<ITestService>(t => t
                .UsingFeatureFlag("UseFailing")
                .AddControl<StableService>()
                .AddCondition<FailingService>("true")
                .OnErrorThrow())
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITestService>();

        // Should throw since OnErrorThrow is set
        Assert.ThrowsAny<Exception>(() => service.Execute());
    }

    [Fact]
    public void OnErrorRedirectAndReplay_throws_when_null()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            var builder = new ServiceExperimentBuilder<IDatabase>();
            builder.OnErrorRedirectAndReplay(null!);
        });
    }

    [Fact]
    public void OnErrorRedirectAndReplayOrdered_throws_when_null()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            var builder = new ServiceExperimentBuilder<IDatabase>();
            builder.OnErrorRedirectAndReplayOrdered(null!);
        });
    }

    [Fact]
    public void OnErrorRedirectAndReplayOrdered_throws_when_empty()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            var builder = new ServiceExperimentBuilder<IDatabase>();
            builder.OnErrorRedirectAndReplayOrdered();
        });
    }

    #endregion

    #region Time-Based Activation Tests

    [Fact]
    public void ActiveDuring_sets_both_times()
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
        RegisterTestServices(services);

        // Set active window in the past
        var start = DateTimeOffset.UtcNow.AddHours(-2);
        var end = DateTimeOffset.UtcNow.AddHours(-1);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<IDatabase>(t => t
                .UsingFeatureFlag("TestFeature")
                .AddControl<LocalDatabase>()
                .AddCondition<CloudDatabase>("true")
                .ActiveDuring(start, end))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();

        // Should use control because trial is not active
        Assert.Equal("LocalDatabase", db.GetName());
    }

    [Fact]
    public void ActiveWhen_with_predicate()
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
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<IDatabase>(t => t
                .UsingFeatureFlag("TestFeature")
                .AddControl<LocalDatabase>()
                .AddCondition<CloudDatabase>("true")
                .ActiveWhen(_ => false)) // Always inactive
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();

        // Should use control because predicate returns false
        Assert.Equal("LocalDatabase", db.GetName());
    }

    #endregion

    #region Build Validation Tests

    [Fact]
    public void Build_throws_when_no_trials_configured()
    {
        var builder = new ServiceExperimentBuilder<IDatabase>();
        var convention = new DefaultExperimentNamingConvention();

        // Use reflection to call internal Build method
        var buildMethod = typeof(ServiceExperimentBuilder<IDatabase>)
            .GetMethod("Build", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
            buildMethod!.Invoke(builder, [convention]));

        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    [Fact]
    public void Build_uses_first_trial_as_default_when_not_specified()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:TestFeature"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<IDatabase>(t => t
                .UsingFeatureFlag("TestFeature")
                .AddCondition<LocalDatabase>("false") // First trial becomes default
                .AddCondition<CloudDatabase>("true"))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();

        // Should use LocalDatabase as it's the first trial and flag is "false"
        Assert.Equal("LocalDatabase", db.GetName());
    }

    #endregion

    #region AddTrial and AddVariant Aliases

    [Fact]
    public void AddTrial_is_alias_for_AddCondition()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TaxProvider"] = "OK"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<ITaxProvider>(t => t
                .UsingConfigurationKey("TaxProvider")
                .AddDefaultTrial<DefaultTaxProvider>("Default")
                .AddTrial<OkTaxProvider>("OK")) // Alias for AddCondition
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var tax = scope.ServiceProvider.GetRequiredService<ITaxProvider>();

        Assert.Equal(4.5m, tax.CalculateTax(100)); // OkTaxProvider
    }

    [Fact]
    public void AddDefaultTrial_is_alias_for_AddControl()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseCloud"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<IDatabase>(t => t
                .UsingFeatureFlag("UseCloud")
                .AddDefaultTrial<LocalDatabase>("false") // Alias for AddControl
                .AddTrial<CloudDatabase>("true"))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();

        Assert.Equal("LocalDatabase", db.GetName());
    }

    #endregion
}
