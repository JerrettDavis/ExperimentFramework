namespace ExperimentFramework.Decorators;

/// <summary>
/// Represents the immutable invocation metadata for a single experiment-proxied call.
/// </summary>
/// <param name="ServiceType">
/// The service interface type being invoked (e.g., <c>typeof(IMyService)</c>).
/// </param>
/// <param name="MethodName">
/// The name of the method on <paramref name="ServiceType"/> being invoked.
/// </param>
/// <param name="TrialKey">
/// The resolved trial key selected for this invocation (e.g. a feature-flag value, configuration value, or other selector).
/// </param>
/// <param name="Arguments">
/// The arguments passed to the invoked method in their original order.
/// </param>
/// <remarks>
/// <para>
/// This record is intended to be passed through the decorator pipeline so that decorators can emit logs,
/// metrics, traces, or other telemetry with consistent dimensions.
/// </para>
/// <para>
/// <see cref="InvocationContext"/> is immutable. Decorators should treat it as read-only and rely on
/// <paramref name="Arguments"/> for call input inspection.
/// </para>
/// </remarks>
public sealed record InvocationContext(
    Type ServiceType,
    string MethodName,
    string TrialKey,
    object?[] Arguments
);