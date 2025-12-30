using ExperimentFramework.Tests.TestInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Xunit;

namespace ExperimentFramework.Tests;

/// <summary>
/// Tests for shared experiment selection mode functionality where multiple trials use the same selection mode configuration.
/// </summary>
public sealed class SharedExperimentKeyTests
{
    /// <summary>
    /// Test interface for cache implementations.
    /// </summary>
    public interface ICache
    {
        string GetName();
    }

    public class InMemoryCache : ICache
    {
        public string GetName() => "InMemoryCache";
    }

    public class RedisCache : ICache
    {
        public string GetName() => "RedisCache";
    }

    [Fact]
    public void ExperimentBuilder_UsingFeatureFlag_applies_to_all_trials()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseCloudDb"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        
        // Register Database implementations
        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();
        services.AddScoped<IDatabase, LocalDatabase>();
        
        // Register Cache implementations
        services.AddScoped<InMemoryCache>();
        services.AddScoped<RedisCache>();
        services.AddScoped<ICache, InMemoryCache>();

        // Configure experiment with shared feature flag
        var builder = ExperimentFrameworkBuilder.Create()
            .Experiment("q1-2025-cloud-migration", exp => exp
                .UsingFeatureFlag("UseCloudDb")
                .Trial<IDatabase>(t => t
                    .AddControl<LocalDatabase>()
                    .AddCondition<CloudDatabase>("true"))
                .Trial<ICache>(t => t
                    .AddControl<InMemoryCache>()
                    .AddCondition<RedisCache>("true")))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        // Act
        using var scope = sp.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<IDatabase>();
        var cache = scope.ServiceProvider.GetRequiredService<ICache>();

        // Assert - Both should use the condition implementations since the flag is true
        Assert.Equal("CloudDatabase", database.GetName());
        Assert.Equal("RedisCache", cache.GetName());
    }

    [Fact]
    public void ExperimentBuilder_UsingFeatureFlag_falls_back_to_control_when_disabled()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseCloudDb"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        
        // Register implementations
        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();
        services.AddScoped<IDatabase, LocalDatabase>();
        services.AddScoped<InMemoryCache>();
        services.AddScoped<RedisCache>();
        services.AddScoped<ICache, InMemoryCache>();

        var builder = ExperimentFrameworkBuilder.Create()
            .Experiment("q1-2025-cloud-migration", exp => exp
                .UsingFeatureFlag("UseCloudDb")
                .Trial<IDatabase>(t => t
                    .AddControl<LocalDatabase>()
                    .AddCondition<CloudDatabase>("true"))
                .Trial<ICache>(t => t
                    .AddControl<InMemoryCache>()
                    .AddCondition<RedisCache>("true")))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        // Act
        using var scope = sp.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<IDatabase>();
        var cache = scope.ServiceProvider.GetRequiredService<ICache>();

        // Assert - Both should use control implementations since flag is false
        Assert.Equal("LocalDatabase", database.GetName());
        Assert.Equal("InMemoryCache", cache.GetName());
    }

    [Fact]
    public void ExperimentBuilder_UsingConfigurationKey_applies_to_all_trials()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Experiments:Environment"] = "cloud"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        
        // Register implementations
        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();
        services.AddScoped<IDatabase, LocalDatabase>();
        services.AddScoped<InMemoryCache>();
        services.AddScoped<RedisCache>();
        services.AddScoped<ICache, InMemoryCache>();

        var builder = ExperimentFrameworkBuilder.Create()
            .Experiment("multi-service-migration", exp => exp
                .UsingConfigurationKey("Experiments:Environment")
                .Trial<IDatabase>(t => t
                    .AddControl<LocalDatabase>()
                    .AddCondition<CloudDatabase>("cloud"))
                .Trial<ICache>(t => t
                    .AddControl<InMemoryCache>()
                    .AddCondition<RedisCache>("cloud")))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        // Act
        using var scope = sp.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<IDatabase>();
        var cache = scope.ServiceProvider.GetRequiredService<ICache>();

        // Assert - Both should use cloud implementations
        Assert.Equal("CloudDatabase", database.GetName());
        Assert.Equal("RedisCache", cache.GetName());
    }

    [Fact]
    public void ExperimentBuilder_UsingCustomMode_applies_to_all_trials()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        
        // Register implementations
        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();
        services.AddScoped<IDatabase, LocalDatabase>();

        var builder = ExperimentFrameworkBuilder.Create()
            .Experiment("custom-mode-experiment", exp => exp
                .UsingCustomMode("CustomMode", "custom-selector")
                .Trial<IDatabase>(t => t
                    .AddControl<LocalDatabase>()
                    .AddCondition<CloudDatabase>("cloud")))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        // Act - This should not throw, even if the custom mode isn't registered
        // It will fall back to control
        using var scope = sp.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<IDatabase>();

        // Assert - Should fall back to control since custom mode isn't registered
        Assert.Equal("LocalDatabase", database.GetName());
    }

    [Fact]
    public void ExperimentBuilder_UsingCustomMode_throws_when_mode_identifier_is_null()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            ExperimentFrameworkBuilder.Create()
                .Experiment("test", exp => exp
                    .UsingCustomMode(null!, "selector"));
        });
    }

    [Fact]
    public void ExperimentBuilder_UsingCustomMode_throws_when_mode_identifier_is_empty()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            ExperimentFrameworkBuilder.Create()
                .Experiment("test", exp => exp
                    .UsingCustomMode("", "selector"));
        });
    }

    [Fact]
    public void ExperimentBuilder_trial_level_selection_overrides_experiment_level()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseCloudDb"] = "true",
                ["FeatureManagement:UseTaxCalculation"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        
        // Register implementations
        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();
        services.AddScoped<IDatabase, LocalDatabase>();
        services.AddScoped<DefaultTaxProvider>();
        services.AddScoped<OkTaxProvider>();
        services.AddScoped<ITaxProvider, DefaultTaxProvider>();

        var builder = ExperimentFrameworkBuilder.Create()
            .Experiment("mixed-config-experiment", exp => exp
                .UsingFeatureFlag("UseCloudDb")
                .Trial<IDatabase>(t => t
                    .AddControl<LocalDatabase>()
                    .AddCondition<CloudDatabase>("true"))
                // Trial-level config overrides experiment-level
                .Trial<ITaxProvider>(t => t
                    .UsingFeatureFlag("UseTaxCalculation")
                    .AddControl<DefaultTaxProvider>()
                    .AddCondition<OkTaxProvider>("true")))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        // Act
        using var scope = sp.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<IDatabase>();
        var taxProvider = scope.ServiceProvider.GetRequiredService<ITaxProvider>();

        // Assert
        // Database uses experiment-level flag (UseCloudDb=true)
        Assert.Equal("CloudDatabase", database.GetName());
        // TaxProvider uses trial-level flag (UseTaxCalculation=false)
        Assert.Equal(0m, taxProvider.CalculateTax(100m));
    }

    [Fact]
    public void ExperimentBuilder_combines_shared_selection_with_activation_time()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseCloudDb"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        
        // Register implementations
        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();
        services.AddScoped<IDatabase, LocalDatabase>();

        // Start time is in the future
        var start = DateTimeOffset.UtcNow.AddHours(1);

        var builder = ExperimentFrameworkBuilder.Create()
            .Experiment("future-experiment", exp => exp
                .UsingFeatureFlag("UseCloudDb")
                .Trial<IDatabase>(t => t
                    .AddControl<LocalDatabase>()
                    .AddCondition<CloudDatabase>("true"))
                .ActiveFrom(start))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        // Act
        using var scope = sp.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<IDatabase>();

        // Assert - Should use control because experiment hasn't started yet
        Assert.Equal("LocalDatabase", database.GetName());
    }
}
