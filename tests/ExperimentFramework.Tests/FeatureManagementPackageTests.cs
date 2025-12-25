using ExperimentFramework.FeatureManagement;
using ExperimentFramework.Naming;
using ExperimentFramework.Selection;
using ExperimentFramework.Tests.TestInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace ExperimentFramework.Tests;

/// <summary>
/// Tests for the FeatureManagement package including VariantFeatureFlagProvider and extension methods.
/// </summary>
public sealed class FeatureManagementPackageTests
{
    #region VariantFeatureFlagProvider Tests

    [Fact]
    public void VariantFeatureFlagProvider_has_correct_mode_identifier()
    {
        var provider = new VariantFeatureFlagProvider();

        Assert.Equal("VariantFeatureFlag", provider.ModeIdentifier);
    }

    [Fact]
    public async Task VariantFeatureFlagProvider_SelectTrialKeyAsync_returns_null_without_variant_manager()
    {
        var provider = new VariantFeatureFlagProvider();
        var services = new ServiceCollection().BuildServiceProvider();

        var context = new SelectionContext
        {
            ServiceProvider = services,
            SelectorName = "test-variant",
            TrialKeys = ["control", "variant"],
            DefaultKey = "control",
            ServiceType = typeof(ITestService)
        };

        var result = await provider.SelectTrialKeyAsync(context);

        Assert.Null(result);
    }

    [Fact]
    public void VariantFeatureFlagProvider_GetDefaultSelectorName_uses_convention()
    {
        var provider = new VariantFeatureFlagProvider();
        var convention = new DefaultExperimentNamingConvention();

        var name = provider.GetDefaultSelectorName(typeof(ITestService), convention);

        Assert.NotEmpty(name);
    }

    #endregion

    #region Registration Tests

    [Fact]
    public void AddExperimentVariantFeatureFlags_registers_provider()
    {
        var services = new ServiceCollection();
        services.AddExperimentVariantFeatureFlags();

        var sp = services.BuildServiceProvider();
        var factory = sp.GetService<ISelectionModeProviderFactory>();

        Assert.NotNull(factory);
        Assert.Equal("VariantFeatureFlag", factory!.ModeIdentifier);
    }

    #endregion

    #region Extension Method Tests

    [Fact]
    public void UsingVariantFeatureFlag_extension_method_configures_experiment()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        services.AddExperimentVariantFeatureFlags();
        RegisterTestServices(services);

        var builder = ExperimentFrameworkBuilder.Create()
            .Trial<IVariantService>(t => t
                .UsingVariantFeatureFlag("VariantFlag")
                .AddControl<ControlVariant>()
                .AddCondition<VariantA>("variant-a")
                .AddCondition<VariantB>("variant-b"))
            .UseDispatchProxy();

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IVariantService>();

        // Without variant configured, should use default
        var result = service.GetName();
        Assert.Equal("ControlVariant", result);
    }

    #endregion

    #region Test Helpers

    private static void RegisterTestServices(IServiceCollection services)
    {
        services.AddScoped<ControlVariant>();
        services.AddScoped<VariantA>();
        services.AddScoped<VariantB>();
        services.AddScoped<IVariantService, ControlVariant>();
    }

    #endregion
}
