namespace ExperimentFramework.Configuration.Models;

/// <summary>
/// Configuration for a decorator in the pipeline.
/// </summary>
public sealed class DecoratorConfig
{
    /// <summary>
    /// Decorator type: "logging", "timeout", "metrics", "killSwitch",
    /// "circuitBreaker", "outcomeCollection", or "custom".
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// For custom decorators, the fully qualified type name
    /// implementing <see cref="ExperimentFramework.Decorators.IExperimentDecoratorFactory"/>.
    /// </summary>
    public string? TypeName { get; set; }

    /// <summary>
    /// Decorator-specific options as key-value pairs.
    /// </summary>
    public Dictionary<string, object>? Options { get; set; }
}

/// <summary>
/// Options for the logging decorator.
/// </summary>
public sealed class LoggingDecoratorOptions
{
    /// <summary>
    /// Whether to enable benchmark/duration logging.
    /// </summary>
    public bool Benchmarks { get; set; }

    /// <summary>
    /// Whether to enable error logging.
    /// </summary>
    public bool ErrorLogging { get; set; }
}

/// <summary>
/// Options for the timeout decorator.
/// </summary>
public sealed class TimeoutDecoratorOptions
{
    /// <summary>
    /// Timeout duration (e.g., "00:00:05" for 5 seconds).
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Action on timeout: "throw", "fallbackToDefault", or "fallbackToSpecificTrial".
    /// </summary>
    public string OnTimeout { get; set; } = "fallbackToDefault";

    /// <summary>
    /// Trial key to fallback to when using "fallbackToSpecificTrial".
    /// </summary>
    public string? FallbackTrialKey { get; set; }
}

/// <summary>
/// Options for the circuit breaker decorator (requires ExperimentFramework.Resilience).
/// </summary>
public sealed class CircuitBreakerDecoratorOptions
{
    /// <summary>
    /// Number of consecutive failures before opening the circuit.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Minimum number of requests in the sampling duration before the circuit can open.
    /// </summary>
    public int MinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Duration for tracking failure rate.
    /// </summary>
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Duration the circuit stays open before allowing a probe request.
    /// </summary>
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Alternative to FailureThreshold: failure ratio (0.0 to 1.0) to trigger open.
    /// </summary>
    public double? FailureRatioThreshold { get; set; }

    /// <summary>
    /// Action when circuit is open: "throw", "fallbackToDefault", or "fallbackToSpecificTrial".
    /// </summary>
    public string OnCircuitOpen { get; set; } = "throw";

    /// <summary>
    /// Trial key to fallback to when using "fallbackToSpecificTrial".
    /// </summary>
    public string? FallbackTrialKey { get; set; }
}

/// <summary>
/// Options for the outcome collection decorator (requires ExperimentFramework.Data).
/// </summary>
public sealed class OutcomeCollectionDecoratorOptions
{
    /// <summary>
    /// Whether to automatically generate outcome IDs.
    /// </summary>
    public bool AutoGenerateIds { get; set; } = true;

    /// <summary>
    /// Whether to automatically set timestamps on outcomes.
    /// </summary>
    public bool AutoSetTimestamps { get; set; } = true;

    /// <summary>
    /// Whether to collect invocation duration as an outcome.
    /// </summary>
    public bool CollectDuration { get; set; } = true;

    /// <summary>
    /// Whether to collect error information as an outcome.
    /// </summary>
    public bool CollectErrors { get; set; } = true;

    /// <summary>
    /// Metric name for duration outcomes.
    /// </summary>
    public string DurationMetricName { get; set; } = "duration_seconds";

    /// <summary>
    /// Metric name for error outcomes.
    /// </summary>
    public string ErrorMetricName { get; set; } = "error";

    /// <summary>
    /// Metric name for success outcomes.
    /// </summary>
    public string SuccessMetricName { get; set; } = "success";

    /// <summary>
    /// Whether to enable batching of outcome writes.
    /// </summary>
    public bool EnableBatching { get; set; }

    /// <summary>
    /// Maximum batch size before flushing.
    /// </summary>
    public int MaxBatchSize { get; set; } = 100;

    /// <summary>
    /// Interval for flushing batched outcomes.
    /// </summary>
    public TimeSpan BatchFlushInterval { get; set; } = TimeSpan.FromSeconds(5);
}
