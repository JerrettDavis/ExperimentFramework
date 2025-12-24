using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ExperimentFramework.Generators.Models;

/// <summary>
/// Represents the parsed definition of an experiment from source code analysis.
/// </summary>
internal sealed class ExperimentDefinitionModel
{
    public ExperimentDefinitionModel(
        INamedTypeSymbol serviceType,
        SelectionModeModel selectionMode,
        string selectorName,
        string defaultKey,
        ImmutableDictionary<string, INamedTypeSymbol> trials,
        ErrorPolicyModel errorPolicy,
        string? fallbackTrialKey = null,
        ImmutableArray<string>? orderedFallbackKeys = null)
    {
        ServiceType = serviceType;
        SelectionMode = selectionMode;
        SelectorName = selectorName;
        DefaultKey = defaultKey;
        Trials = trials;
        ErrorPolicy = errorPolicy;
        FallbackTrialKey = fallbackTrialKey;
        OrderedFallbackKeys = orderedFallbackKeys ?? ImmutableArray<string>.Empty;
    }

    /// <summary>
    /// The service interface type symbol (e.g., IMyDatabase).
    /// </summary>
    public INamedTypeSymbol ServiceType { get; }

    /// <summary>
    /// The selection mode for this experiment (BooleanFeatureFlag, ConfigurationValue, etc.).
    /// </summary>
    public SelectionModeModel SelectionMode { get; }

    /// <summary>
    /// The selector name (feature flag name or configuration key).
    /// </summary>
    public string SelectorName { get; }

    /// <summary>
    /// The default trial key.
    /// </summary>
    public string DefaultKey { get; }

    /// <summary>
    /// Mapping of trial keys to implementation type symbols.
    /// </summary>
    public ImmutableDictionary<string, INamedTypeSymbol> Trials { get; }

    /// <summary>
    /// The error policy for this experiment (Throw, RedirectAndReplayDefault, etc.).
    /// </summary>
    public ErrorPolicyModel ErrorPolicy { get; }

    /// <summary>
    /// The fallback trial key for RedirectAndReplay policy.
    /// </summary>
    public string? FallbackTrialKey { get; }

    /// <summary>
    /// The ordered list of fallback trial keys for RedirectAndReplayOrdered policy.
    /// </summary>
    public ImmutableArray<string> OrderedFallbackKeys { get; }
}

/// <summary>
/// Represents the selection mode for an experiment.
/// </summary>
internal enum SelectionModeModel
{
    BooleanFeatureFlag,
    ConfigurationValue,
    VariantFeatureFlag,
    StickyRouting,
    OpenFeature
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
internal sealed class ExperimentDefinitionCollection
{
    public ExperimentDefinitionCollection(
        ImmutableArray<ExperimentDefinitionModel> definitions,
        Location? compositionRootLocation = null)
    {
        Definitions = definitions;
        CompositionRootLocation = compositionRootLocation;
    }

    /// <summary>
    /// All experiment definitions discovered.
    /// </summary>
    public ImmutableArray<ExperimentDefinitionModel> Definitions { get; }

    /// <summary>
    /// The location in source code where the composition root was found (for diagnostics).
    /// </summary>
    public Location? CompositionRootLocation { get; }
}
