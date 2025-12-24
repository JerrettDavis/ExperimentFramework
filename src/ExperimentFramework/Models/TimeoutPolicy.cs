namespace ExperimentFramework.Models;

/// <summary>
/// Defines timeout behavior for experiment trials.
/// </summary>
public sealed class TimeoutPolicy
{
    /// <summary>
    /// Gets the timeout duration for trial execution.
    /// </summary>
    public required TimeSpan Timeout { get; init; }

    /// <summary>
    /// Gets the action to take when a timeout occurs.
    /// </summary>
    public required TimeoutAction OnTimeout { get; init; }

    /// <summary>
    /// Gets the specific trial key to fallback to on timeout (when OnTimeout is FallbackToSpecificTrial).
    /// </summary>
    public string? FallbackTrialKey { get; init; }
}

/// <summary>
/// Actions to take when a trial times out.
/// </summary>
public enum TimeoutAction
{
    /// <summary>
    /// Throw a TimeoutException.
    /// </summary>
    ThrowException,

    /// <summary>
    /// Fallback to the default trial.
    /// </summary>
    FallbackToDefault,

    /// <summary>
    /// Fallback to a specific trial.
    /// </summary>
    FallbackToSpecificTrial
}
