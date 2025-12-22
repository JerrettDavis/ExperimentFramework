namespace ExperimentFramework.Telemetry;

/// <summary>
/// No-op telemetry implementation that performs no tracking (default when telemetry is not configured).
/// </summary>
/// <remarks>
/// This implementation provides zero overhead when telemetry is not needed.
/// It is registered as the default telemetry provider in the DI container.
/// </remarks>
internal sealed class NoopExperimentTelemetry : IExperimentTelemetry
{
    /// <summary>
    /// Singleton instance of the no-op telemetry implementation.
    /// </summary>
    public static readonly NoopExperimentTelemetry Instance = new();

    private NoopExperimentTelemetry() { }

    /// <inheritdoc/>
    public IExperimentTelemetryScope StartInvocation(
        Type serviceType,
        string methodName,
        string selectorName,
        string trialKey,
        IReadOnlyList<string> candidateKeys)
        => NoopScope.Instance;

    /// <summary>
    /// No-op telemetry scope implementation.
    /// </summary>
    private sealed class NoopScope : IExperimentTelemetryScope
    {
        public static readonly NoopScope Instance = new();

        private NoopScope() { }

        public void RecordSuccess() { }
        public void RecordFailure(Exception exception) { }
        public void RecordFallback(string fallbackKey) { }
        public void RecordVariant(string variantName, string variantSource) { }
        public void Dispose() { }
    }
}
