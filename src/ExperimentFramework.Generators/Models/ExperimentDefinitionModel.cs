using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ExperimentFramework.Generators.Models;

/// <summary>
/// Represents the parsed definition of an experiment from source code analysis.
/// </summary>
internal sealed class ExperimentDefinitionModel(
    INamedTypeSymbol serviceType,
    SelectionModeModel selectionMode,
    string selectorName,
    string defaultKey,
    ImmutableDictionary<string, INamedTypeSymbol> trials,
    ErrorPolicyModel errorPolicy,
    string? fallbackTrialKey = null,
    ImmutableArray<string>? orderedFallbackKeys = null,
    string? modeIdentifier = null
)
{
    /// <summary>
    /// The service interface type symbol (e.g., IMyDatabase).
    /// </summary>
    public INamedTypeSymbol ServiceType { get; } = serviceType;

    /// <summary>
    /// The selection mode for this experiment (BooleanFeatureFlag, ConfigurationValue, etc.).
    /// </summary>
    public SelectionModeModel SelectionMode { get; } = selectionMode;

    /// <summary>
    /// The selector name (feature flag name or configuration key).
    /// </summary>
    public string SelectorName { get; } = selectorName;

    /// <summary>
    /// The default trial key.
    /// </summary>
    public string DefaultKey { get; } = defaultKey;

    /// <summary>
    /// Mapping of trial keys to implementation type symbols.
    /// </summary>
    public ImmutableDictionary<string, INamedTypeSymbol> Trials { get; } = trials;

    /// <summary>
    /// The error policy for this experiment (Throw, RedirectAndReplayDefault, etc.).
    /// </summary>
    public ErrorPolicyModel ErrorPolicy { get; } = errorPolicy;

    /// <summary>
    /// The fallback trial key for RedirectAndReplay policy.
    /// </summary>
    public string? FallbackTrialKey { get; } = fallbackTrialKey;

    /// <summary>
    /// The ordered list of fallback trial keys for RedirectAndReplayOrdered policy.
    /// </summary>
    public ImmutableArray<string> OrderedFallbackKeys { get; } = orderedFallbackKeys ?? ImmutableArray<string>.Empty;

    /// <summary>
    /// The custom mode identifier for Custom selection mode.
    /// </summary>
    /// <remarks>
    /// When <see cref="SelectionMode"/> is <see cref="SelectionModeModel.Custom"/>,
    /// this property contains the provider identifier used to look up the selection
    /// logic at runtime.
    /// </remarks>
    public string? ModeIdentifier { get; } = modeIdentifier;
}

/// <summary>
/// Represents the selection mode for an experiment.
/// </summary>
/// <remarks>
/// Only built-in modes are defined here. External modes use <see cref="Custom"/>
/// and delegate to runtime providers.
/// </remarks>
internal enum SelectionModeModel
{
    /// <summary>Boolean feature flag evaluation via Microsoft.FeatureManagement.</summary>
    BooleanFeatureFlag,

    /// <summary>Configuration-based selection via IConfiguration.</summary>
    ConfigurationValue,

    /// <summary>Custom provider-based selection. Delegates to runtime SelectionModeRegistry.</summary>
    Custom
}

/// <summary>
/// Represents the error handling policy for an experiment.
/// </summary>
internal enum ErrorPolicyModel
{
    Throw,
    RedirectAndReplayDefault,
    RedirectAndReplayAny,
    RedirectAndReplay,
    RedirectAndReplayOrdered
}

/// <summary>
/// Represents all experiments discovered in a compilation unit.
/// </summary>
internal sealed class ExperimentDefinitionCollection(
    ImmutableArray<ExperimentDefinitionModel> definitions,
    Location? compositionRootLocation = null
)
{
    /// <summary>
    /// All experiment definitions discovered.
    /// </summary>
    public ImmutableArray<ExperimentDefinitionModel> Definitions { get; } = definitions;

    /// <summary>
    /// The location in source code where the composition root was found (for diagnostics).
    /// </summary>
    public Location? CompositionRootLocation { get; } = compositionRootLocation;
}
