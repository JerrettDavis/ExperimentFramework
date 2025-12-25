namespace ExperimentFramework.Models;

/// <summary>
/// Encapsulates the rules for WHEN a trial should be activated.
/// </summary>
/// <remarks>
/// <para>
/// Selection rules combine multiple activation criteria:
/// <list type="bullet">
/// <item><description>Selection mode (feature flag, configuration, sticky routing, etc.)</description></item>
/// <item><description>Time-based activation (start and end times)</description></item>
/// <item><description>Custom activation predicates (delegate-based rules)</description></item>
/// </list>
/// </para>
/// <para>
/// All criteria must be satisfied for the trial to be active. If any criterion fails,
/// the trial falls back to its control implementation.
/// </para>
/// </remarks>
public sealed class SelectionRule
{
    /// <summary>
    /// Gets the selection mode used to choose which condition to run.
    /// </summary>
    public required SelectionMode Mode { get; init; }

    /// <summary>
    /// Gets the selector name used by the chosen <see cref="Mode"/> (feature flag name or configuration key).
    /// </summary>
    public required string SelectorName { get; init; }

    /// <summary>
    /// Gets the time from which the trial becomes active.
    /// If null, the trial is active from the beginning of time.
    /// </summary>
    public DateTimeOffset? StartTime { get; init; }

    /// <summary>
    /// Gets the time after which the trial becomes inactive.
    /// If null, the trial remains active indefinitely.
    /// </summary>
    public DateTimeOffset? EndTime { get; init; }

    /// <summary>
    /// Gets an optional custom predicate that determines whether the trial is active.
    /// </summary>
    /// <remarks>
    /// The predicate receives the service provider for accessing services during evaluation.
    /// Return true to allow the trial to activate, false to fall back to control.
    /// </remarks>
    public Func<IServiceProvider, bool>? ActivationPredicate { get; init; }

    /// <summary>
    /// Gets an optional percentage allocation for this trial (0.0 to 1.0).
    /// </summary>
    /// <remarks>
    /// Used in conjunction with sticky routing to allocate a percentage of users to this trial.
    /// </remarks>
    public double? PercentageAllocation { get; init; }

    /// <summary>
    /// Gets an optional list of user segments that should receive this trial.
    /// </summary>
    public IReadOnlyList<string>? UserSegments { get; init; }

    /// <summary>
    /// Creates a selection rule from ExperimentRegistration properties.
    /// </summary>
    internal static SelectionRule FromRegistration(SelectionMode mode, string selectorName)
        => new()
        {
            Mode = mode,
            SelectorName = selectorName
        };
}
