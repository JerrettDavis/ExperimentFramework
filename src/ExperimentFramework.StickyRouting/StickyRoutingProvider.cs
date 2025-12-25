using ExperimentFramework.Naming;
using ExperimentFramework.Selection;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.StickyRouting;

/// <summary>
/// Well-known mode identifier for sticky routing selection.
/// </summary>
public static class StickyRoutingModes
{
    /// <summary>
    /// Mode identifier for sticky routing selection.
    /// </summary>
    public const string StickyRouting = "StickyRouting";
}

/// <summary>
/// Selection mode provider that uses identity-based consistent hashing for trial selection.
/// </summary>
/// <remarks>
/// <para>
/// This provider uses <see cref="IExperimentIdentityProvider"/> to get a user/session identity,
/// then applies consistent hashing to deterministically select a trial key.
/// </para>
/// <para>
/// The same identity will always route to the same trial, ensuring a consistent user experience.
/// </para>
/// </remarks>
[SelectionMode(StickyRoutingModes.StickyRouting)]
public sealed class StickyRoutingProvider : ISelectionModeProvider
{
    /// <inheritdoc />
    public string ModeIdentifier => StickyRoutingModes.StickyRouting;

    /// <inheritdoc />
    public ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
    {
        // Try to get the identity provider
        var identityProvider = context.ServiceProvider.GetService<IExperimentIdentityProvider>();
        if (identityProvider != null)
        {
            try
            {
                if (identityProvider.TryGetIdentity(out var identity) && !string.IsNullOrEmpty(identity))
                {
                    var selectedKey = StickyTrialRouter.SelectTrial(
                        identity,
                        context.SelectorName,
                        context.TrialKeys);
                    return ValueTask.FromResult<string?>(selectedKey);
                }
            }
            catch
            {
                // Fall through to default
            }
        }

        // Fall back to default
        return ValueTask.FromResult<string?>(null);
    }

    /// <inheritdoc />
    public string GetDefaultSelectorName(Type serviceType, IExperimentNamingConvention convention)
        => convention.FeatureFlagNameFor(serviceType);
}
