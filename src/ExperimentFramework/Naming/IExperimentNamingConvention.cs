namespace ExperimentFramework.Naming;

/// <summary>
/// Defines naming conventions for experiment selectors (feature flags and configuration keys).
/// </summary>
/// <remarks>
/// This abstraction allows customization of how selector names are derived from service types
/// when explicit names are not provided via builder methods.
/// </remarks>
public interface IExperimentNamingConvention
{
    /// <summary>
    /// Returns the feature flag name to use for boolean feature flag selection.
    /// </summary>
    /// <param name="serviceType">The service interface type (e.g., <c>typeof(IMyDatabase)</c>).</param>
    /// <returns>The feature flag name (e.g., <c>"IMyDatabase"</c>).</returns>
    string FeatureFlagNameFor(Type serviceType);

    /// <summary>
    /// Returns the variant feature flag name to use for variant-based selection.
    /// </summary>
    /// <param name="serviceType">The service interface type (e.g., <c>typeof(IMyDatabase)</c>).</param>
    /// <returns>The variant feature flag name (e.g., <c>"IMyDatabase"</c>).</returns>
    string VariantFlagNameFor(Type serviceType);

    /// <summary>
    /// Returns the configuration key to use for configuration-based selection.
    /// </summary>
    /// <param name="serviceType">The service interface type (e.g., <c>typeof(IMyTaxProvider)</c>).</param>
    /// <returns>The configuration key (e.g., <c>"Experiments:IMyTaxProvider"</c>).</returns>
    string ConfigurationKeyFor(Type serviceType);
}
