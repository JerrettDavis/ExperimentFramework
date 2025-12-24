namespace ExperimentFramework.Models;
/// <summary>
/// Describes how the framework selects a trial key at runtime.
/// </summary>
public enum SelectionMode
{
    /// <summary>
    /// Uses a boolean feature flag to choose between keys "true" and "false".
    /// </summary>
    BooleanFeatureFlag,

    /// <summary>
    /// Uses a configuration value (string) as the trial key.
    /// </summary>
    ConfigurationValue,

    /// <summary>
    /// Uses IVariantFeatureManager to select a trial based on variant name.
    /// </summary>
    /// <remarks>
    /// Requires Microsoft.FeatureManagement package with variant support.
    /// Falls back to default trial if variant manager is unavailable.
    /// </remarks>
    VariantFeatureFlag,

    /// <summary>
    /// Uses sticky routing based on user/session identity hash for deterministic trial selection.
    /// </summary>
    /// <remarks>
    /// Requires <c>IExperimentIdentityProvider</c> to be registered in DI.
    /// Falls back to feature flag selection if identity provider is unavailable.
    /// </remarks>
    StickyRouting,

    /// <summary>
    /// Uses OpenFeature to select a trial based on flag evaluation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Requires the OpenFeature SDK to be configured with a provider.
    /// Uses <c>GetStringValueAsync</c> for multi-variant flags or <c>GetBooleanValueAsync</c> for boolean flags.
    /// </para>
    /// <para>
    /// Falls back to default trial if OpenFeature is unavailable or flag evaluation fails.
    /// </para>
    /// </remarks>
    OpenFeature
}
