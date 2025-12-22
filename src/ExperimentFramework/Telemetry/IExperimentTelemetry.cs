namespace ExperimentFramework.Telemetry;

/// <summary>
/// Abstraction for experiment telemetry tracking (tracing, metrics, events).
/// </summary>
/// <remarks>
/// This interface allows pluggable telemetry implementations such as OpenTelemetry,
/// Application Insights, or custom metrics systems.
/// </remarks>
public interface IExperimentTelemetry
{
    /// <summary>
    /// Starts tracking an experiment invocation and returns a telemetry scope.
    /// </summary>
    /// <param name="serviceType">The service interface type being invoked.</param>
    /// <param name="methodName">The method name being called.</param>
    /// <param name="selectorName">The selector name used for trial selection (feature flag or configuration key).</param>
    /// <param name="trialKey">The initially selected trial key.</param>
    /// <param name="candidateKeys">All candidate trial keys considered for this invocation.</param>
    /// <returns>A disposable telemetry scope that tracks the lifetime of the invocation.</returns>
    /// <remarks>
    /// The returned scope should be disposed when the invocation completes, regardless of success or failure.
    /// Use the scope's methods to record success, failure, or fallback events during execution.
    /// </remarks>
    IExperimentTelemetryScope StartInvocation(
        Type serviceType,
        string methodName,
        string selectorName,
        string trialKey,
        IReadOnlyList<string> candidateKeys);
}

/// <summary>
/// Represents an active telemetry scope for tracking an experiment invocation's lifecycle.
/// </summary>
/// <remarks>
/// This scope is created by <see cref="IExperimentTelemetry.StartInvocation"/> and should
/// be disposed when the invocation completes to finalize telemetry data.
/// </remarks>
public interface IExperimentTelemetryScope : IDisposable
{
    /// <summary>
    /// Records that the experiment invocation completed successfully.
    /// </summary>
    void RecordSuccess();

    /// <summary>
    /// Records that the experiment invocation failed with an exception.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    void RecordFailure(Exception exception);

    /// <summary>
    /// Records that execution fell back to a different trial due to an error.
    /// </summary>
    /// <param name="fallbackKey">The trial key that was attempted as a fallback.</param>
    void RecordFallback(string fallbackKey);

    /// <summary>
    /// Records variant-specific metadata when using variant feature flags.
    /// </summary>
    /// <param name="variantName">The name of the variant selected.</param>
    /// <param name="variantSource">The source of the variant (e.g., "snapshot", "manager", "variantManager").</param>
    void RecordVariant(string variantName, string variantSource);
}
