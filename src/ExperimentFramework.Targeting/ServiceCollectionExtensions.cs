using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Targeting.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExperimentFramework.Targeting;

/// <summary>
/// Extension methods for registering targeting support.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds targeting-based selection mode support to the experiment framework.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for targeting options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the <see cref="TargetingProvider"/> which enables
    /// the <c>.UsingTargeting()</c> selection mode.
    /// </para>
    /// <para>
    /// You must also register an <see cref="ITargetingContextProvider"/> implementation.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddExperimentTargeting(
        this IServiceCollection services,
        Action<TargetingOptions>? configure = null)
    {
        var options = new TargetingOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.AddSelectionModeProvider<TargetingProvider>();

        return services;
    }

    /// <summary>
    /// Adds targeting configuration with in-memory rule storage.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for targeting rules.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddExperimentTargetingRules(
        this IServiceCollection services,
        Action<InMemoryTargetingConfiguration> configure)
    {
        var config = new InMemoryTargetingConfiguration();
        configure(config);

        services.TryAddSingleton<ITargetingConfigurationProvider>(config);

        return services;
    }

    /// <summary>
    /// Adds targeting configuration handlers to the experiment framework.
    /// This enables the 'targeting' selection mode type in configuration files.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExperimentTargetingConfiguration(this IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigurationSelectionModeHandler, TargetingSelectionModeHandler>());
        return services;
    }
}
