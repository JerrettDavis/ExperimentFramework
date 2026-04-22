namespace ExperimentFramework.Dashboard.Abstractions;

/// <summary>
/// Provides plugin management operations for the dashboard.
/// </summary>
/// <remarks>
/// Implementations bridge the dashboard API to the underlying plugin system
/// (e.g., <c>IPluginManager</c> from ExperimentFramework.Plugins).
/// Register an implementation if plugin management is required.
/// </remarks>
public interface IPluginManagementService
{
    /// <summary>
    /// Gets all currently loaded plugins.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of plugin descriptors.</returns>
    Task<IReadOnlyList<PluginDescriptor>> GetLoadedPluginsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers and loads all plugins from configured discovery paths.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The discovered plugin descriptors.</returns>
    Task<IReadOnlyList<PluginDescriptor>> DiscoverPluginsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reloads all currently loaded plugins.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The reloaded plugin descriptors.</returns>
    Task<IReadOnlyList<PluginDescriptor>> ReloadAllPluginsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Describes a loaded plugin.
/// </summary>
public sealed class PluginDescriptor
{
    /// <summary>
    /// Gets or sets the plugin identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets or sets the plugin display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the plugin version.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets or sets the plugin description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the health state of the plugin (e.g., "Healthy", "Warning", "Error").
    /// </summary>
    public string HealthState { get; init; } = "Healthy";

    /// <summary>
    /// Gets or sets the path from which the plugin was loaded.
    /// </summary>
    public string? PluginPath { get; init; }

    /// <summary>
    /// Gets or sets the isolation mode used for this plugin.
    /// </summary>
    public string? IsolationMode { get; init; }

    /// <summary>
    /// Gets or sets the service registrations provided by the plugin.
    /// </summary>
    public IReadOnlyList<PluginServiceInfo>? Services { get; init; }
}

/// <summary>
/// Describes a service registration provided by a plugin.
/// </summary>
public sealed class PluginServiceInfo
{
    /// <summary>
    /// Gets or sets the interface type name.
    /// </summary>
    public required string InterfaceName { get; init; }

    /// <summary>
    /// Gets or sets the implementation type name.
    /// </summary>
    public required string ImplementationType { get; init; }

    /// <summary>
    /// Gets or sets the optional alias for this implementation.
    /// </summary>
    public string? Alias { get; init; }
}
