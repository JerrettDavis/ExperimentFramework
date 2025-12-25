using ExperimentFramework.Naming;
using ExperimentFramework.Selection;
using OpenFeature;

namespace ExperimentFramework.OpenFeature;

/// <summary>
/// Well-known mode identifier for OpenFeature selection.
/// </summary>
public static class OpenFeatureModes
{
    /// <summary>
    /// Mode identifier for OpenFeature selection.
    /// </summary>
    public const string OpenFeature = "OpenFeature";
}

/// <summary>
/// Selection mode provider that uses the OpenFeature SDK for feature flag evaluation.
/// </summary>
/// <remarks>
/// <para>
/// This provider integrates with the vendor-neutral OpenFeature SDK, allowing you to use
/// any supported feature flag backend (LaunchDarkly, Split, Flagsmith, etc.).
/// </para>
/// <para>
/// The provider evaluates a string flag using the OpenFeature API and uses the result
/// as the trial key. If evaluation fails, falls back to the default trial.
/// </para>
/// </remarks>
[SelectionMode(OpenFeatureModes.OpenFeature)]
public sealed class OpenFeatureProvider : ISelectionModeProvider
{
    /// <inheritdoc />
    public string ModeIdentifier => OpenFeatureModes.OpenFeature;

    /// <inheritdoc />
    public async ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
    {
        try
        {
            // Use the OpenFeature API to get the client
            var client = Api.Instance.GetClient();

            // Evaluate as a string flag - the result is the trial key
            var result = await client.GetStringValueAsync(
                context.SelectorName,
                context.DefaultKey,
                context: null);

            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
        }
        catch
        {
            // Fall through to default
        }

        return null;
    }

    /// <inheritdoc />
    public string GetDefaultSelectorName(Type serviceType, IExperimentNamingConvention convention)
        => convention.OpenFeatureFlagNameFor(serviceType);
}
