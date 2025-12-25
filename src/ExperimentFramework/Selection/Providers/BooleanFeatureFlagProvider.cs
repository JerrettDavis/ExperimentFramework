using ExperimentFramework.Naming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace ExperimentFramework.Selection.Providers;

/// <summary>
/// Selection mode provider that uses IFeatureManager for boolean feature flag evaluation.
/// </summary>
/// <remarks>
/// Returns "true" or "false" based on whether the feature flag is enabled.
/// Prefers IFeatureManagerSnapshot for request-scoped consistency when available.
/// </remarks>
internal sealed class BooleanFeatureFlagProvider : ISelectionModeProvider
{
    /// <inheritdoc />
    public string ModeIdentifier => SelectionModes.BooleanFeatureFlag;

    /// <inheritdoc />
    public async ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
    {
        // Prefer snapshot for request-scoped consistency
        var snapshot = context.ServiceProvider.GetService<IFeatureManagerSnapshot>();
        if (snapshot != null)
        {
            try
            {
                var enabled = await snapshot.IsEnabledAsync(context.SelectorName);
                return enabled ? "true" : "false";
            }
            catch
            {
                // Fall through to IFeatureManager
            }
        }

        // Fall back to IFeatureManager
        var manager = context.ServiceProvider.GetService<IFeatureManager>();
        if (manager != null)
        {
            try
            {
                var enabled = await manager.IsEnabledAsync(context.SelectorName);
                return enabled ? "true" : "false";
            }
            catch
            {
                // Fall back to default
                return null;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public string GetDefaultSelectorName(Type serviceType, IExperimentNamingConvention convention)
        => convention.FeatureFlagNameFor(serviceType);
}

/// <summary>
/// Factory for creating BooleanFeatureFlagProvider instances.
/// </summary>
internal sealed class BooleanFeatureFlagProviderFactory : ISelectionModeProviderFactory
{
    /// <inheritdoc />
    public string ModeIdentifier => SelectionModes.BooleanFeatureFlag;

    /// <inheritdoc />
    public ISelectionModeProvider Create(IServiceProvider scopedProvider)
        => new BooleanFeatureFlagProvider();
}
