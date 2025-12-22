namespace ExperimentFramework.Naming;

/// <summary>
/// Default naming convention for experiment selectors.
/// </summary>
/// <remarks>
/// This implementation provides sensible defaults:
/// <list type="bullet">
/// <item><description>Feature flags use the service type name directly (e.g., <c>"IMyDatabase"</c>).</description></item>
/// <item><description>Configuration keys use the pattern <c>"Experiments:{ServiceType.Name}"</c>.</description></item>
/// </list>
/// </remarks>
internal sealed class DefaultExperimentNamingConvention : IExperimentNamingConvention
{
    /// <inheritdoc/>
    public string FeatureFlagNameFor(Type serviceType)
        => serviceType.Name;

    /// <inheritdoc/>
    public string VariantFlagNameFor(Type serviceType)
        => serviceType.Name; // Same as boolean for default

    /// <inheritdoc/>
    public string ConfigurationKeyFor(Type serviceType)
        => $"Experiments:{serviceType.Name}";
}
