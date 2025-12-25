using ExperimentFramework.Selection;

namespace ExperimentFramework.Models;

/// <summary>
/// Describes how the framework selects a trial key at runtime.
/// </summary>
/// <remarks>
/// <para>
/// The core library provides two built-in selection modes:
/// <list type="bullet">
/// <item><description><see cref="BooleanFeatureFlag"/> - Uses Microsoft.FeatureManagement for boolean flag evaluation.</description></item>
/// <item><description><see cref="ConfigurationValue"/> - Uses IConfiguration for string-based selection.</description></item>
/// </list>
/// </para>
/// <para>
/// Additional selection modes are available through extension packages:
/// <list type="bullet">
/// <item><description><c>ExperimentFramework.OpenFeature</c> - OpenFeature SDK integration.</description></item>
/// <item><description><c>ExperimentFramework.FeatureManagement</c> - Variant feature flag support.</description></item>
/// <item><description><c>ExperimentFramework.StickyRouting</c> - Identity-based sticky routing.</description></item>
/// </list>
/// </para>
/// </remarks>
public enum SelectionMode
{
    /// <summary>
    /// Uses a boolean feature flag to choose between keys "true" and "false".
    /// </summary>
    /// <remarks>
    /// Requires Microsoft.FeatureManagement to be configured. Prefers IFeatureManagerSnapshot
    /// for request-scoped consistency when available.
    /// </remarks>
    BooleanFeatureFlag,

    /// <summary>
    /// Uses a configuration value (string) as the trial key.
    /// </summary>
    /// <remarks>
    /// Reads the trial key directly from IConfiguration using the selector name as the key path.
    /// </remarks>
    ConfigurationValue,

    /// <summary>
    /// Uses a custom selection mode provider registered via <c>UsingCustomMode()</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Custom modes allow external packages to extend the framework with new selection
    /// strategies without modifying the core library.
    /// </para>
    /// <para>
    /// The actual selection logic is determined by the <see cref="ISelectionModeProvider"/>
    /// registered with the corresponding mode identifier in the <see cref="SelectionModeRegistry"/>.
    /// </para>
    /// </remarks>
    Custom
}

/// <summary>
/// Extension methods for <see cref="SelectionMode"/>.
/// </summary>
public static class SelectionModeExtensions
{
    /// <summary>
    /// Converts a <see cref="SelectionMode"/> enum value to its string identifier
    /// for use with <see cref="SelectionModeRegistry"/>.
    /// </summary>
    /// <param name="mode">The selection mode.</param>
    /// <returns>The corresponding mode identifier string.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="mode"/> is <see cref="SelectionMode.Custom"/>,
    /// which requires an explicit mode identifier.
    /// </exception>
    public static string ToModeIdentifier(this SelectionMode mode) => mode switch
    {
        SelectionMode.BooleanFeatureFlag => SelectionModes.BooleanFeatureFlag,
        SelectionMode.ConfigurationValue => SelectionModes.ConfigurationValue,
        SelectionMode.Custom => throw new InvalidOperationException(
            "Custom mode requires an explicit ModeIdentifier. Use UsingCustomMode() instead."),
        _ => SelectionModes.BooleanFeatureFlag
    };
}
