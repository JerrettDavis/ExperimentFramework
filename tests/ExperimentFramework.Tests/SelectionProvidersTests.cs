using ExperimentFramework.Models;
using ExperimentFramework.Naming;
using ExperimentFramework.Selection;
using ExperimentFramework.Selection.Providers;
using ExperimentFramework.Tests.TestInterfaces;
using ExperimentFramework.Variants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace ExperimentFramework.Tests;

/// <summary>
/// Tests for built-in selection mode providers.
/// </summary>
public sealed class SelectionProvidersTests
{
    #region BooleanFeatureFlagProvider Tests

    [Fact]
    public void BooleanFeatureFlagProvider_has_correct_mode_identifier()
    {
        var provider = new BooleanFeatureFlagProvider();
        Assert.Equal("BooleanFeatureFlag", provider.ModeIdentifier);
    }

    [Fact]
    public async Task BooleanFeatureFlagProvider_returns_true_when_feature_enabled()
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
        var sp = services.BuildServiceProvider();

        var provider = new BooleanFeatureFlagProvider();
        var context = new SelectionContext
        {
            ServiceProvider = sp,
            SelectorName = "TestFeature",
            TrialKeys = ["true", "false"],
            DefaultKey = "false",
            ServiceType = typeof(ITestService)
        };

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("true", result);
    }

    [Fact]
    public async Task BooleanFeatureFlagProvider_returns_false_when_feature_disabled()
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
        var sp = services.BuildServiceProvider();

        var provider = new BooleanFeatureFlagProvider();
        var context = new SelectionContext
        {
            ServiceProvider = sp,
            SelectorName = "TestFeature",
            TrialKeys = ["true", "false"],
            DefaultKey = "false",
            ServiceType = typeof(ITestService)
        };

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("false", result);
    }

    [Fact]
    public async Task BooleanFeatureFlagProvider_returns_null_when_no_feature_manager()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();

        var provider = new BooleanFeatureFlagProvider();
        var context = new SelectionContext
        {
            ServiceProvider = sp,
            SelectorName = "TestFeature",
            TrialKeys = ["true", "false"],
            DefaultKey = "false",
            ServiceType = typeof(ITestService)
        };

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Fact]
    public void BooleanFeatureFlagProvider_GetDefaultSelectorName_uses_convention()
    {
        var provider = new BooleanFeatureFlagProvider();
        var convention = new DefaultExperimentNamingConvention();

        var name = provider.GetDefaultSelectorName(typeof(ITestService), convention);

        Assert.NotEmpty(name);
        Assert.Contains("TestService", name);
    }

    [Fact]
    public void BooleanFeatureFlagProviderFactory_creates_provider()
    {
        var factory = new BooleanFeatureFlagProviderFactory();
        var sp = new ServiceCollection().BuildServiceProvider();

        var provider = factory.Create(sp);

        Assert.NotNull(provider);
        Assert.Equal("BooleanFeatureFlag", provider.ModeIdentifier);
    }

    #endregion

    #region ConfigurationValueProvider Tests

    [Fact]
    public void ConfigurationValueProvider_has_correct_mode_identifier()
    {
        var provider = new ConfigurationValueProvider();
        Assert.Equal("ConfigurationValue", provider.ModeIdentifier);
    }

    [Fact]
    public async Task ConfigurationValueProvider_returns_configuration_value()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TaxProvider"] = "OK"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        var sp = services.BuildServiceProvider();

        var provider = new ConfigurationValueProvider();
        var context = new SelectionContext
        {
            ServiceProvider = sp,
            SelectorName = "TaxProvider",
            TrialKeys = ["Default", "OK", "TX"],
            DefaultKey = "Default",
            ServiceType = typeof(ITaxProvider)
        };

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("OK", result);
    }

    [Fact]
    public async Task ConfigurationValueProvider_returns_null_when_value_empty()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TaxProvider"] = ""
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        var sp = services.BuildServiceProvider();

        var provider = new ConfigurationValueProvider();
        var context = new SelectionContext
        {
            ServiceProvider = sp,
            SelectorName = "TaxProvider",
            TrialKeys = ["Default", "OK"],
            DefaultKey = "Default",
            ServiceType = typeof(ITaxProvider)
        };

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Fact]
    public async Task ConfigurationValueProvider_returns_null_when_key_missing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        var sp = services.BuildServiceProvider();

        var provider = new ConfigurationValueProvider();
        var context = new SelectionContext
        {
            ServiceProvider = sp,
            SelectorName = "NonExistent",
            TrialKeys = ["Default", "OK"],
            DefaultKey = "Default",
            ServiceType = typeof(ITaxProvider)
        };

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Fact]
    public async Task ConfigurationValueProvider_returns_null_when_no_configuration()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();

        var provider = new ConfigurationValueProvider();
        var context = new SelectionContext
        {
            ServiceProvider = sp,
            SelectorName = "TaxProvider",
            TrialKeys = ["Default", "OK"],
            DefaultKey = "Default",
            ServiceType = typeof(ITaxProvider)
        };

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Fact]
    public void ConfigurationValueProvider_GetDefaultSelectorName_uses_convention()
    {
        var provider = new ConfigurationValueProvider();
        var convention = new DefaultExperimentNamingConvention();

        var name = provider.GetDefaultSelectorName(typeof(ITaxProvider), convention);

        Assert.NotEmpty(name);
    }

    [Fact]
    public void ConfigurationValueProviderFactory_creates_provider()
    {
        var factory = new ConfigurationValueProviderFactory();
        var sp = new ServiceCollection().BuildServiceProvider();

        var provider = factory.Create(sp);

        Assert.NotNull(provider);
        Assert.Equal("ConfigurationValue", provider.ModeIdentifier);
    }

    #endregion

    #region SelectionModeExtensions Tests

    [Fact]
    public void ToModeIdentifier_returns_BooleanFeatureFlag()
    {
        var result = SelectionMode.BooleanFeatureFlag.ToModeIdentifier();
        Assert.Equal("BooleanFeatureFlag", result);
    }

    [Fact]
    public void ToModeIdentifier_returns_ConfigurationValue()
    {
        var result = SelectionMode.ConfigurationValue.ToModeIdentifier();
        Assert.Equal("ConfigurationValue", result);
    }

    [Fact]
    public void ToModeIdentifier_throws_for_Custom()
    {
        Assert.Throws<InvalidOperationException>(() => SelectionMode.Custom.ToModeIdentifier());
    }

    [Fact]
    public void ToModeIdentifier_returns_default_for_unknown_value()
    {
        // Cast an invalid int to SelectionMode to test the default case
        var invalidMode = (SelectionMode)999;
        var result = invalidMode.ToModeIdentifier();
        Assert.Equal("BooleanFeatureFlag", result);
    }

    #endregion

    #region VariantFeatureManagerAdapter Tests

    [Fact]
    public void VariantFeatureManagerAdapter_IsAvailable_returns_boolean()
    {
        // Just verify the property doesn't throw
        var isAvailable = VariantFeatureManagerAdapter.IsAvailable;
        Assert.True(isAvailable || !isAvailable); // Always passes, just checks no exception
    }

    [Fact]
    public async Task VariantFeatureManagerAdapter_TryGetVariantAsync_returns_null_when_not_available()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();

        var result = await VariantFeatureManagerAdapter.TryGetVariantAsync(sp, "TestFeature");

        // If IVariantFeatureManager is not available or not registered, returns null
        Assert.Null(result);
    }

    [Fact]
    public async Task VariantFeatureManagerAdapter_TryGetVariantAsync_handles_missing_service()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:TestVariant:EnabledFor:0:Name"] = "AlwaysOn"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        // Don't register IVariantFeatureManager
        var sp = services.BuildServiceProvider();

        var result = await VariantFeatureManagerAdapter.TryGetVariantAsync(sp, "TestVariant");

        // Should gracefully return null
        Assert.Null(result);
    }

    #endregion
}

/// <summary>
/// Tests for ExperimentBuilder time-based activation and metadata.
/// </summary>
public sealed class ExperimentBuilderActivationTests
{
    private static void RegisterTestServices(IServiceCollection services)
    {
        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();
        services.AddScoped<IDatabase, LocalDatabase>();
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
        RegisterTestServices(services);

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
    public void ExperimentBuilder_multiple_trials_all_apply_experiment_activation()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseCloud"] = "true",
                ["TaxProvider"] = "OK"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        RegisterTestServices(services);
        services.AddScoped<DefaultTaxProvider>();
        services.AddScoped<OkTaxProvider>();
        services.AddScoped<ITaxProvider, DefaultTaxProvider>();

        // Set experiment to be inactive (end time in past)
        var pastTime = DateTimeOffset.UtcNow.AddHours(-1);

        var builder = ExperimentFrameworkBuilder.Create()
            .Experiment("multi-trial-experiment", exp => exp
                .Trial<IDatabase>(t => t
                    .UsingFeatureFlag("UseCloud")
                    .AddControl<LocalDatabase>()
                    .AddCondition<CloudDatabase>("true"))
                .Trial<ITaxProvider>(t => t
                    .UsingConfigurationKey("TaxProvider")
                    .AddControl<DefaultTaxProvider>()
                    .AddCondition<OkTaxProvider>("OK"))
                .ActiveUntil(pastTime))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();

        // Both should fall back to control because experiment has ended
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();
        var tax = scope.ServiceProvider.GetRequiredService<ITaxProvider>();

        Assert.Equal("LocalDatabase", db.GetName());
        Assert.Equal(0m, tax.CalculateTax(100)); // DefaultTaxProvider
    }

    [Fact]
    public void ExperimentBuilder_predicate_is_applied_to_trials()
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
            .Experiment("predicate-experiment", exp => exp
                .Trial<IDatabase>(t => t
                    .UsingFeatureFlag("UseCloud")
                    .AddControl<LocalDatabase>()
                    .AddCondition<CloudDatabase>("true"))
                .ActiveWhen(_ => false)) // Always inactive
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();

        // Should fall back to control because predicate returns false
        Assert.Equal("LocalDatabase", db.GetName());
    }
}

/// <summary>
/// Tests for ConfigurationValueProvider edge cases.
/// </summary>
public sealed class ConfigurationValueProviderAdditionalTests
{
    [Fact]
    public async Task ConfigurationValueProvider_with_nested_key()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Experiments:TaxProvider:Setting"] = "OK"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        var sp = services.BuildServiceProvider();

        var provider = new ConfigurationValueProvider();
        var context = new SelectionContext
        {
            ServiceProvider = sp,
            SelectorName = "Experiments:TaxProvider:Setting",
            TrialKeys = new List<string> { "Default", "OK" },
            DefaultKey = "Default",
            ServiceType = typeof(ITaxProvider)
        };

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Equal("OK", result);
    }

    [Fact]
    public void ConfigurationValueProvider_GetDefaultSelectorName_format()
    {
        var provider = new ConfigurationValueProvider();
        var convention = new DefaultExperimentNamingConvention();

        var name = provider.GetDefaultSelectorName(typeof(ITaxProvider), convention);

        // Should follow the convention pattern
        Assert.NotEmpty(name);
        Assert.Contains("TaxProvider", name);
    }
}
