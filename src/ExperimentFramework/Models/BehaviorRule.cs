namespace ExperimentFramework.Models;

/// <summary>
/// Encapsulates the rules for HOW a trial behaves during execution.
/// </summary>
/// <remarks>
/// <para>
/// Behavior rules control:
/// <list type="bullet">
/// <item><description>Error handling and fallback strategies</description></item>
/// <item><description>Timeout enforcement</description></item>
/// <item><description>Retry policies (future)</description></item>
/// <item><description>Circuit breaker configuration (future)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class BehaviorRule
{
    /// <summary>
    /// Gets the policy used when a chosen condition throws an exception.
    /// </summary>
    public required OnErrorPolicy OnErrorPolicy { get; init; }

    /// <summary>
    /// Gets the condition key to use as a fallback when <see cref="OnErrorPolicy"/> is
    /// <see cref="OnErrorPolicy.RedirectAndReplay"/>.
    /// </summary>
    public string? FallbackConditionKey { get; init; }

    /// <summary>
    /// Gets an ordered list of condition keys to attempt as fallbacks when <see cref="OnErrorPolicy"/> is
    /// <see cref="OnErrorPolicy.RedirectAndReplayOrdered"/>.
    /// </summary>
    public IReadOnlyList<string>? OrderedFallbackKeys { get; init; }

    /// <summary>
    /// Gets the maximum duration allowed for trial execution before timing out.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Gets the action to take when a timeout occurs.
    /// </summary>
    public TimeoutAction TimeoutAction { get; init; } = TimeoutAction.ThrowException;

    /// <summary>
    /// Gets the condition key to fall back to on timeout when <see cref="TimeoutAction"/> is
    /// <see cref="TimeoutAction.FallbackToSpecificTrial"/>.
    /// </summary>
    public string? TimeoutFallbackConditionKey { get; init; }

    /// <summary>
    /// Creates a behavior rule from ExperimentRegistration properties.
    /// </summary>
    internal static BehaviorRule FromRegistration(
        OnErrorPolicy onErrorPolicy,
        string? fallbackTrialKey = null,
        IReadOnlyList<string>? orderedFallbackKeys = null)
        => new()
        {
            OnErrorPolicy = onErrorPolicy,
            FallbackConditionKey = fallbackTrialKey,
            OrderedFallbackKeys = orderedFallbackKeys
        };

    /// <summary>
    /// Creates a default behavior rule that throws on error.
    /// </summary>
    public static BehaviorRule Default => new()
    {
        OnErrorPolicy = OnErrorPolicy.Throw
    };
}
