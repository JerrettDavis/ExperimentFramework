using ExperimentFramework.Naming;

namespace ExperimentFramework.Selection;

/// <summary>
/// Provides trial key selection logic for a specific selection mode.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to create a custom selection mode that can be used
/// with ExperimentFramework. The framework will call <see cref="SelectTrialKeyAsync"/>
/// on each method invocation to determine which trial implementation to use.
/// </para>
/// <para>
/// Selection mode providers are created via <see cref="ISelectionModeProviderFactory"/>
/// to enable scoped dependency resolution.
/// </para>
/// </remarks>
public interface ISelectionModeProvider
{
    /// <summary>
    /// Gets the unique identifier for this selection mode.
    /// </summary>
    /// <remarks>
    /// This identifier is used to match providers to experiment configurations.
    /// Examples: "BooleanFeatureFlag", "OpenFeature", "Redis"
    /// </remarks>
    string ModeIdentifier { get; }

    /// <summary>
    /// Selects the trial key based on the current context.
    /// </summary>
    /// <param name="context">
    /// The selection context containing service provider, selector name, and available trial keys.
    /// </param>
    /// <returns>
    /// The selected trial key, or <c>null</c> to fall back to the default key.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Implementations should handle failures gracefully by returning <c>null</c>
    /// rather than throwing exceptions. This allows the framework to fall back
    /// to the default (control) implementation.
    /// </para>
    /// <para>
    /// The returned key must match one of the keys in <see cref="SelectionContext.TrialKeys"/>,
    /// or the framework will use the default key.
    /// </para>
    /// </remarks>
    ValueTask<string?> SelectTrialKeyAsync(SelectionContext context);

    /// <summary>
    /// Gets the default selector name for a service type using the naming convention.
    /// </summary>
    /// <param name="serviceType">The service interface type.</param>
    /// <param name="convention">The naming convention to use.</param>
    /// <returns>The default selector name for this mode.</returns>
    /// <remarks>
    /// This is called when no explicit selector name is provided in the fluent configuration.
    /// </remarks>
    string GetDefaultSelectorName(Type serviceType, IExperimentNamingConvention convention);
}
