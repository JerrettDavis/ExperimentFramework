using ExperimentFramework.Naming;
using ExperimentFramework.OpenFeature;
using ExperimentFramework.Selection;
using ExperimentFramework.Tests.TestInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Tests;

/// <summary>
/// Tests for the OpenFeature package including provider and extension methods.
/// </summary>
public sealed class OpenFeatureTests
{
    #region OpenFeatureProvider Tests

    [Fact]
    public void OpenFeatureProvider_has_correct_mode_identifier()
    {
        var provider = new OpenFeatureProvider();

        Assert.Equal("OpenFeature", provider.ModeIdentifier);
    }

    [Fact]
    public async Task OpenFeatureProvider_SelectTrialKeyAsync_returns_null_on_error()
    {
        var provider = new OpenFeatureProvider();
        var services = new ServiceCollection().BuildServiceProvider();

        var context = new SelectionContext
        {
            ServiceProvider = services,
            SelectorName = "non-existent-flag",
            TrialKeys = ["control", "variant"],
            DefaultKey = "control",
            ServiceType = typeof(ITestService)
        };

        // Without OpenFeature configured, should gracefully return null
        var result = await provider.SelectTrialKeyAsync(context);

        // Result depends on OpenFeature configuration
        // With no provider set up, it returns the default
        Assert.True(result == null || result == "control");
    }

    [Fact]
    public void OpenFeatureProvider_GetDefaultSelectorName_uses_kebab_case()
    {
        var provider = new OpenFeatureProvider();
        var convention = new DefaultExperimentNamingConvention();

        var name = provider.GetDefaultSelectorName(typeof(ITestService), convention);

        Assert.Contains("-", name.ToLower()); // Should be kebab-case
    }

    #endregion

    #region Registration Tests

    [Fact]
    public void AddExperimentOpenFeature_registers_provider()
    {
        var services = new ServiceCollection();
        services.AddExperimentOpenFeature();

        var sp = services.BuildServiceProvider();
        var factory = sp.GetService<ISelectionModeProviderFactory>();

        Assert.NotNull(factory);
        Assert.Equal("OpenFeature", factory!.ModeIdentifier);
    }

    #endregion

    #region Extension Method Tests

    [Fact]
    public void UsingOpenFeature_extension_method_configures_experiment()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddExperimentOpenFeature();
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<IDatabase>(t => t
                .UsingOpenFeature("database-flag")
                .AddControl<LocalDatabase>()
                .AddCondition<CloudDatabase>("cloud"))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();

        // Without OpenFeature provider configured, should use default
        Assert.NotNull(db);
    }

    #endregion

    #region Test Helpers

    private static void RegisterTestServices(IServiceCollection services)
    {
        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();
        services.AddScoped<IDatabase, LocalDatabase>();
    }

    #endregion
}
