namespace ExperimentFramework.Models;

/// <summary>
/// Represents a named experiment that can contain multiple trials across different service interfaces.
/// </summary>
/// <remarks>
/// <para>
/// An experiment is a logical grouping of related trials. It enables:
/// <list type="bullet">
/// <item><description>Grouping trials that are part of the same feature rollout</description></item>
/// <item><description>Applying shared activation rules (time bounds, predicates) to all trials</description></item>
/// <item><description>Managing experiment lifecycle as a single unit</description></item>
/// </list>
/// </para>
/// <para>
/// Example scenarios:
/// <list type="bullet">
/// <item><description>A "Q1 Migration" experiment with trials for IDatabase, ICache, and ILogger</description></item>
/// <item><description>A "New Payment Flow" experiment with trials for IPaymentProcessor and IFraudDetector</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class Experiment
{
    /// <summary>
    /// Gets the unique name identifying this experiment.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the trials contained in this experiment.
    /// </summary>
    public required IReadOnlyList<Trial> Trials { get; init; }

    /// <summary>
    /// Gets the time from which the experiment becomes active.
    /// </summary>
    /// <remarks>
    /// If set, all trials in the experiment will be inactive before this time,
    /// falling back to their control implementations.
    /// </remarks>
    public DateTimeOffset? StartTime { get; init; }

    /// <summary>
    /// Gets the time after which the experiment becomes inactive.
    /// </summary>
    /// <remarks>
    /// If set, all trials in the experiment will be inactive after this time,
    /// falling back to their control implementations.
    /// </remarks>
    public DateTimeOffset? EndTime { get; init; }

    /// <summary>
    /// Gets an optional custom predicate that determines whether the experiment is active.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The predicate receives the service provider for accessing services during evaluation.
    /// Return true to allow the experiment's trials to activate, false to fall back to controls.
    /// </para>
    /// <para>
    /// This predicate is evaluated in addition to any trial-specific predicates.
    /// Both must return true for a trial's conditions to be selected.
    /// </para>
    /// </remarks>
    public Func<IServiceProvider, bool>? ActivationPredicate { get; init; }

    /// <summary>
    /// Gets optional metadata associated with this experiment.
    /// </summary>
    /// <remarks>
    /// Can be used to store additional information such as:
    /// <list type="bullet">
    /// <item><description>Owner/team information</description></item>
    /// <item><description>JIRA ticket references</description></item>
    /// <item><description>Success criteria</description></item>
    /// <item><description>Rollback instructions</description></item>
    /// </list>
    /// </remarks>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Returns a debug-friendly representation of the experiment.
    /// </summary>
    public override string ToString()
    {
        var trialSummary = string.Join(", ", Trials.Select(t => t.ServiceType.Name));
        return $"Experiment '{Name}' [{trialSummary}]";
    }
}
