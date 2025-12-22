namespace ExperimentFramework.Models;
/// <summary>
/// Captures the rules for selecting trials and the set of trials.
/// </summary>
/// <remarks>
/// This is the runtime representation used by the experiment proxy.
/// It is designed to be immutable after creation.
/// </remarks>
public sealed class ExperimentRegistration
{
    /// <summary>
    /// Gets the service interface type being proxied.
    /// </summary>
    public required Type ServiceType { get; init; }

    /// <summary>
    /// Gets the selection mode used to choose which trial key to run.
    /// </summary>
    public required SelectionMode Mode { get; init; }

    /// <summary>
    /// Gets the selector name used by the chosen <see cref="Mode"/> (feature flag name or configuration key).
    /// </summary>
    public required string SelectorName { get; init; } // feature flag name or configuration key

    /// <summary>
    /// Gets the key used when no selector value is available or when falling back.
    /// </summary>
    public required string DefaultKey { get; init; }

    /// <summary>
    /// Gets the map of trial key to implementation type.
    /// </summary>
    public required IReadOnlyDictionary<string, Type> Trials { get; init; }

    /// <summary>
    /// Gets the policy used when a chosen trial throws.
    /// </summary>
    public required OnErrorPolicy OnErrorPolicy { get; init; }

    /// <summary>
    /// Returns a debug-friendly representation of the registration.
    /// </summary>
    public override string ToString()
        => $"{ServiceType.Name} ({Mode}) selector='{SelectorName}' default='{DefaultKey}' trials=[{string.Join(",", Trials.Keys)}]";
}

