namespace ExperimentFramework.Selection;

/// <summary>
/// Context provided to selection mode providers for trial key selection.
/// </summary>
/// <remarks>
/// This context contains all information needed by a provider to determine
/// which trial key should be selected for a given invocation.
/// </remarks>
public sealed class SelectionContext
{
    /// <summary>
    /// Gets the scoped service provider for resolving dependencies.
    /// </summary>
    public required IServiceProvider ServiceProvider { get; init; }

    /// <summary>
    /// Gets the selector name (e.g., feature flag name, configuration key).
    /// </summary>
    public required string SelectorName { get; init; }

    /// <summary>
    /// Gets the list of registered trial keys for this experiment.
    /// </summary>
    public required IReadOnlyList<string> TrialKeys { get; init; }

    /// <summary>
    /// Gets the default (control) key to use when selection fails or is unavailable.
    /// </summary>
    public required string DefaultKey { get; init; }

    /// <summary>
    /// Gets the service interface type being experimented on.
    /// </summary>
    public required Type ServiceType { get; init; }
}
