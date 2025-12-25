using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.OpenFeature;

/// <summary>
/// Extension methods for registering OpenFeature support.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenFeature selection mode support to the experiment framework.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the <see cref="OpenFeatureProvider"/> which enables
    /// the <c>.UsingOpenFeature()</c> selection mode.
    /// </para>
    /// <para>
    /// You must also configure the OpenFeature provider:
    /// <code>
    /// await Api.Instance.SetProviderAsync(new MyFeatureFlagProvider());
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddExperimentOpenFeature();
    /// services.AddExperimentFramework(builder);
    ///
    /// // Configure OpenFeature provider
    /// await Api.Instance.SetProviderAsync(new LaunchDarklyProvider("sdk-key"));
    /// </code>
    /// </example>
    public static IServiceCollection AddExperimentOpenFeature(this IServiceCollection services)
        => services.AddSelectionModeProvider<OpenFeatureProvider>();
}
