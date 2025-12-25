namespace ExperimentFramework.Selection;

/// <summary>
/// Built-in selection mode identifiers.
/// </summary>
/// <remarks>
/// External packages can define their own mode identifiers as string constants.
/// These built-in modes are provided by the core framework.
/// </remarks>
public static class SelectionModes
{
    /// <summary>
    /// Selection based on boolean feature flag evaluation.
    /// Returns "true" or "false" based on IFeatureManager.IsEnabledAsync().
    /// </summary>
    public const string BooleanFeatureFlag = "BooleanFeatureFlag";

    /// <summary>
    /// Selection based on IConfiguration string value.
    /// Returns the configuration value directly as the trial key.
    /// </summary>
    public const string ConfigurationValue = "ConfigurationValue";

    /// <summary>
    /// Selection based on IVariantFeatureManager variant evaluation.
    /// Requires ExperimentFramework.VariantFeatureFlag package.
    /// </summary>
    public const string VariantFeatureFlag = "VariantFeatureFlag";

    /// <summary>
    /// Deterministic selection based on user/session identity hash.
    /// Requires ExperimentFramework.StickyRouting package.
    /// </summary>
    public const string StickyRouting = "StickyRouting";

    /// <summary>
    /// Selection based on OpenFeature SDK flag evaluation.
    /// Requires ExperimentFramework.OpenFeature package.
    /// </summary>
    public const string OpenFeature = "OpenFeature";
}
