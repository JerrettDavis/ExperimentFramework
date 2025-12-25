using ExperimentFramework.Models;

namespace ExperimentFramework.Activation;

/// <summary>
/// Evaluates whether an experiment or trial is currently active based on time bounds and predicates.
/// </summary>
/// <remarks>
/// <para>
/// The activation evaluator checks:
/// <list type="bullet">
/// <item><description>Time bounds (start and end times)</description></item>
/// <item><description>Custom activation predicates</description></item>
/// </list>
/// </para>
/// <para>
/// A trial is considered active if:
/// <list type="number">
/// <item><description>Current time is after the start time (if specified)</description></item>
/// <item><description>Current time is before the end time (if specified)</description></item>
/// <item><description>The activation predicate returns true (if specified)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ActivationEvaluator
{
    private readonly IExperimentTimeProvider _timeProvider;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="ActivationEvaluator"/>.
    /// </summary>
    /// <param name="timeProvider">The time provider to use for time-based checks.</param>
    /// <param name="serviceProvider">The service provider for predicate evaluation.</param>
    public ActivationEvaluator(IExperimentTimeProvider timeProvider, IServiceProvider serviceProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ActivationEvaluator"/> using the system time provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider for predicate evaluation.</param>
    public ActivationEvaluator(IServiceProvider serviceProvider)
        : this(SystemTimeProvider.Instance, serviceProvider)
    {
    }

    /// <summary>
    /// Evaluates whether a trial is currently active based on its registration.
    /// </summary>
    /// <param name="registration">The experiment registration to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the trial is active and conditions can be selected;
    /// <c>false</c> if the trial should fall back to the control implementation.
    /// </returns>
    public bool IsActive(ExperimentRegistration registration)
    {
        if (registration == null)
            throw new ArgumentNullException(nameof(registration));

        return IsActiveForTime(registration.StartTime, registration.EndTime)
            && IsActiveForPredicate(registration.ActivationPredicate);
    }

    /// <summary>
    /// Evaluates whether a trial is currently active based on its selection rule.
    /// </summary>
    /// <param name="rule">The selection rule to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the trial is active and conditions can be selected;
    /// <c>false</c> if the trial should fall back to the control implementation.
    /// </returns>
    public bool IsActive(SelectionRule rule)
    {
        if (rule == null)
            throw new ArgumentNullException(nameof(rule));

        return IsActiveForTime(rule.StartTime, rule.EndTime)
            && IsActiveForPredicate(rule.ActivationPredicate);
    }

    /// <summary>
    /// Evaluates whether an experiment is currently active based on its time bounds and predicate.
    /// </summary>
    /// <param name="experiment">The experiment to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the experiment is active;
    /// <c>false</c> if all trials should fall back to their control implementations.
    /// </returns>
    public bool IsActive(Experiment experiment)
    {
        if (experiment == null)
            throw new ArgumentNullException(nameof(experiment));

        return IsActiveForTime(experiment.StartTime, experiment.EndTime)
            && IsActiveForPredicate(experiment.ActivationPredicate);
    }

    /// <summary>
    /// Evaluates time-based activation.
    /// </summary>
    /// <param name="startTime">The start time, or null if no start time constraint.</param>
    /// <param name="endTime">The end time, or null if no end time constraint.</param>
    /// <returns><c>true</c> if the current time is within the valid range.</returns>
    private bool IsActiveForTime(DateTimeOffset? startTime, DateTimeOffset? endTime)
    {
        var now = _timeProvider.UtcNow;

        if (startTime.HasValue && now < startTime.Value)
            return false;

        if (endTime.HasValue && now > endTime.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Evaluates predicate-based activation.
    /// </summary>
    /// <param name="predicate">The activation predicate, or null if no predicate constraint.</param>
    /// <returns><c>true</c> if no predicate is specified or the predicate returns true.</returns>
    private bool IsActiveForPredicate(Func<IServiceProvider, bool>? predicate)
    {
        if (predicate == null)
            return true;

        try
        {
            return predicate(_serviceProvider);
        }
        catch
        {
            // If the predicate throws, treat the trial as inactive for safety
            return false;
        }
    }
}
