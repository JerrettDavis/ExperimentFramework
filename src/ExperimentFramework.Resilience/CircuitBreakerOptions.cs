namespace ExperimentFramework.Resilience;

/// <summary>
/// Options for configuring circuit breaker behavior.
/// </summary>
public sealed class CircuitBreakerOptions
{
    /// <summary>
    /// Gets or sets the number of consecutive failures before opening the circuit.
    /// Default is 5.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the minimum number of requests in the sampling duration before evaluating circuit state.
    /// Default is 10.
    /// </summary>
    public int MinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Gets or sets the duration of the sampling window for tracking failures.
    /// Default is 10 seconds.
    /// </summary>
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets how long the circuit stays open before transitioning to half-open.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the failure ratio (0.0 to 1.0) threshold for opening the circuit.
    /// If set, this overrides FailureThreshold. Default is null (not used).
    /// </summary>
    public double? FailureRatioThreshold { get; set; }

    /// <summary>
    /// Gets or sets the action to take when the circuit is open.
    /// </summary>
    public CircuitBreakerAction OnCircuitOpen { get; set; } = CircuitBreakerAction.ThrowException;

    /// <summary>
    /// Gets or sets the specific trial key to fallback to when circuit is open (when OnCircuitOpen is FallbackToSpecificTrial).
    /// </summary>
    public string? FallbackTrialKey { get; set; }
}

/// <summary>
/// Actions to take when the circuit breaker is open.
/// </summary>
public enum CircuitBreakerAction
{
    /// <summary>
    /// Throw a CircuitBreakerOpenException.
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
