namespace ExperimentFramework.Plugins.Abstractions;

/// <summary>
/// Represents the manifest metadata for a plugin.
/// </summary>
public interface IPluginManifest
{
    /// <summary>
    /// Gets the manifest schema version.
    /// </summary>
    string ManifestVersion { get; }

    /// <summary>
    /// Gets the unique identifier for the plugin.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name of the plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the semantic version of the plugin.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the optional description of the plugin.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the isolation configuration for the plugin.
    /// </summary>
    PluginIsolationConfig Isolation { get; }

    /// <summary>
    /// Gets the service registrations declared by the plugin.
    /// </summary>
    IReadOnlyList<PluginServiceRegistration> Services { get; }

    /// <summary>
    /// Gets the lifecycle configuration for the plugin.
    /// </summary>
    PluginLifecycleConfig Lifecycle { get; }
}

/// <summary>
/// Configuration for plugin isolation behavior.
/// </summary>
public sealed record PluginIsolationConfig
{
    /// <summary>
    /// Gets or sets the isolation mode.
    /// </summary>
    public PluginIsolationMode Mode { get; init; } = PluginIsolationMode.Shared;

    /// <summary>
    /// Gets or sets the list of assembly names to share with the host when using Shared isolation.
    /// </summary>
    public IReadOnlyList<string> SharedAssemblies { get; init; } = [];
}

/// <summary>
/// Represents a service registration declared in the plugin manifest.
/// </summary>
public sealed record PluginServiceRegistration
{
    /// <summary>
    /// Gets or sets the interface type name that this service implements.
    /// </summary>
    public required string Interface { get; init; }

    /// <summary>
    /// Gets or sets the implementations of this interface.
    /// </summary>
    public IReadOnlyList<PluginImplementation> Implementations { get; init; } = [];
}

/// <summary>
/// Represents a single implementation within a plugin.
/// </summary>
public sealed record PluginImplementation
{
    /// <summary>
    /// Gets or sets the fully qualified type name of the implementation.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets or sets the optional alias for referencing this implementation.
    /// </summary>
    public string? Alias { get; init; }
}

/// <summary>
/// Configuration for plugin lifecycle behavior.
/// </summary>
public sealed record PluginLifecycleConfig
{
    /// <summary>
    /// Gets or sets whether the plugin supports hot reload.
    /// </summary>
    public bool SupportsHotReload { get; init; } = true;

    /// <summary>
    /// Gets or sets whether the plugin requires the host to restart on unload.
    /// </summary>
    public bool RequiresRestartOnUnload { get; init; }
}
