using ExperimentFramework.Naming;
using ExperimentFramework.Selection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace ExperimentFramework.FeatureManagement;

/// <summary>
/// Well-known mode identifier for variant feature flag selection.
/// </summary>
public static class VariantFeatureFlagModes
{
    /// <summary>
    /// Mode identifier for variant feature flag selection.
    /// </summary>
    public const string VariantFeatureFlag = "VariantFeatureFlag";
}

/// <summary>
/// Selection mode provider that uses IVariantFeatureManager for multi-variant feature flags.
/// </summary>
/// <remarks>
/// <para>
/// This provider integrates with Microsoft.FeatureManagement's variant feature manager,
/// which allows feature flags to return named variants instead of just true/false.
/// </para>
/// <para>
/// The variant name returned by the feature manager is used as the trial key.
/// If no variant is returned, falls back to the default trial.
/// </para>
/// </remarks>
[SelectionMode(VariantFeatureFlagModes.VariantFeatureFlag)]
public sealed class VariantFeatureFlagProvider : ISelectionModeProvider
{
    /// <inheritdoc />
    public string ModeIdentifier => VariantFeatureFlagModes.VariantFeatureFlag;

    /// <inheritdoc />
    public async ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
    {
        // Try to get the variant feature manager
        var variantManager = context.ServiceProvider.GetService<IVariantFeatureManager>();
        if (variantManager == null)
            return null;
        
        try
        {
            var variant = await variantManager.GetVariantAsync(context.SelectorName, CancellationToken.None);
            if (variant != null && !string.IsNullOrEmpty(variant.Name))
            {
                return variant.Name;
            }
        }
        catch
        {
            // Fall through to default
        }

        // Fall back to default
        return null;
    }

    /// <inheritdoc />
    public string GetDefaultSelectorName(Type serviceType, IExperimentNamingConvention convention)
        => convention.VariantFlagNameFor(serviceType);
}
