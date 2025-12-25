using ExperimentFramework.Selection;

namespace ExperimentFramework.Models;

/// <summary>
/// Captures the rules for selecting conditions and the set of implementations.
/// </summary>
/// <remarks>
/// <para>
/// This is the runtime representation used by the experiment proxy.
/// It is designed to be immutable after creation.
/// </para>
/// <para>
/// <b>Fluent DSL:</b> This class provides multiple equivalent property names to support
/// different team conventions:
/// <list type="bullet">
/// <item><description><c>DefaultKey</c> / <c>ControlKey</c> - The baseline implementation key</description></item>
/// <item><description><c>Trials</c> / <c>Conditions</c> - The map of keys to implementation types</description></item>
/// <item><description><c>FallbackTrialKey</c> / <c>FallbackConditionKey</c> - The fallback key for error handling</description></item>
/// </list>
/// Use whichever terminology best fits your scenario.
/// </para>
/// </remarks>
public sealed class ExperimentRegistration
{
    /// <summary>
    /// Gets the service interface type being proxied.
    /// </summary>
    public required Type ServiceType { get; init; }

    /// <summary>
    /// Gets the selection mode used to choose which condition key to run.
    /// </summary>
    public required SelectionMode Mode { get; init; }

    /// <summary>
    /// Gets the mode identifier string for provider-based selection.
    /// </summary>
    /// <remarks>
    /// This string identifier is used to look up the appropriate
    /// <see cref="ISelectionModeProvider"/> from the <see cref="SelectionModeRegistry"/>.
    /// For built-in modes, this is derived from <see cref="Mode"/>. For custom modes,
    /// this is set directly via <c>UsingCustomMode()</c>.
    /// </remarks>
    public required string ModeIdentifier { get; init; }

    /// <summary>
    /// Gets the selector name used by the chosen <see cref="Mode"/> (feature flag name or configuration key).
    /// </summary>
    public required string SelectorName { get; init; }

    /// <summary>
    /// Gets the key used when no selector value is available or when falling back.
    /// </summary>
    public required string DefaultKey { get; init; }

    /// <summary>
    /// Gets the key identifying the control (baseline) implementation.
    /// </summary>
    /// <remarks>
    /// This property provides the same value as <see cref="DefaultKey"/>.
    /// Use whichever terminology fits your team's conventions.
    /// </remarks>
    public string ControlKey => DefaultKey;

    /// <summary>
    /// Gets the map of condition key to implementation type.
    /// </summary>
    public required IReadOnlyDictionary<string, Type> Trials { get; init; }

    /// <summary>
    /// Gets the map of condition key to implementation type.
    /// </summary>
    /// <remarks>
    /// This property provides the same value as <see cref="Trials"/>.
    /// Use whichever terminology fits your team's conventions.
    /// </remarks>
    public IReadOnlyDictionary<string, Type> Conditions => Trials;

    /// <summary>
    /// Gets the policy used when a chosen condition throws.
    /// </summary>
    public required OnErrorPolicy OnErrorPolicy { get; init; }

    /// <summary>
    /// Gets the condition key to use as a fallback when <see cref="OnErrorPolicy"/> is
    /// <see cref="OnErrorPolicy.RedirectAndReplay"/>.
    /// </summary>
    public string? FallbackTrialKey { get; init; }

    /// <summary>
    /// Gets the condition key to use as a fallback when <see cref="OnErrorPolicy"/> is
    /// <see cref="OnErrorPolicy.RedirectAndReplay"/>.
    /// </summary>
    /// <remarks>
    /// This property provides the same value as <see cref="FallbackTrialKey"/>.
    /// Use whichever terminology fits your team's conventions.
    /// </remarks>
    public string? FallbackConditionKey => FallbackTrialKey;

    /// <summary>
    /// Gets an ordered list of condition keys to attempt as fallbacks when <see cref="OnErrorPolicy"/> is
    /// <see cref="OnErrorPolicy.RedirectAndReplayOrdered"/>.
    /// </summary>
    public IReadOnlyList<string>? OrderedFallbackKeys { get; init; }

    /// <summary>
    /// Gets the name of the parent experiment this trial belongs to, if any.
    /// </summary>
    public string? ExperimentName { get; init; }

    /// <summary>
    /// Gets the time from which this trial becomes active.
    /// </summary>
    /// <remarks>
    /// If set, the trial will fall back to the control implementation before this time.
    /// </remarks>
    public DateTimeOffset? StartTime { get; init; }

    /// <summary>
    /// Gets the time after which this trial becomes inactive.
    /// </summary>
    /// <remarks>
    /// If set, the trial will fall back to the control implementation after this time.
    /// </remarks>
    public DateTimeOffset? EndTime { get; init; }

    /// <summary>
    /// Gets the custom predicate that determines whether this trial is active.
    /// </summary>
    /// <remarks>
    /// If the predicate returns false, the trial falls back to the control implementation.
    /// </remarks>
    public Func<IServiceProvider, bool>? ActivationPredicate { get; init; }

    /// <summary>
    /// Returns a debug-friendly representation of the registration.
    /// </summary>
    public override string ToString()
        => $"{ServiceType.Name} ({Mode}) selector='{SelectorName}' control='{ControlKey}' conditions=[{string.Join(",", Conditions.Keys)}]";
}

