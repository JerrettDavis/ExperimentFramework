using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.FeatureManagement;

/// <summary>
/// Extension methods for registering variant feature flag support.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds variant feature flag selection mode support to the experiment framework.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the <see cref="VariantFeatureFlagProvider"/> which enables
    /// the <c>.UsingVariantFeatureFlag()</c> selection mode.
    /// </para>
    /// <para>
    /// You must also register Microsoft.FeatureManagement with variant support:
    /// <code>
    /// services.AddFeatureManagement();
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddExperimentVariantFeatureFlags();
    /// services.AddFeatureManagement();
    /// services.AddExperimentFramework(builder);
    /// </code>
    /// </example>
    public static IServiceCollection AddExperimentVariantFeatureFlags(this IServiceCollection services)
        => services.AddSelectionModeProvider<VariantFeatureFlagProvider>();
}
