namespace ExperimentFramework.Plugins.Abstractions;

/// <summary>
/// Event arguments for plugin events.
/// </summary>
public sealed class PluginEventArgs(IPluginContext context) : EventArgs
{
    /// <summary>
    /// Gets the plugin context associated with this event.
    /// </summary>
    public IPluginContext Context { get; } = context;
}

/// <summary>
/// Event arguments for plugin load failures.
/// </summary>
public sealed class PluginLoadFailedEventArgs(string pluginPath, Exception exception) : EventArgs
{
    /// <summary>
    /// Gets the path of the plugin that failed to load.
    /// </summary>
    public string PluginPath { get; } = pluginPath;

    /// <summary>
    /// Gets the exception that caused the load failure.
    /// </summary>
    public Exception Exception { get; } = exception;
}

/// <summary>
/// Central manager for coordinating plugin operations.
/// </summary>
public interface IPluginManager : IAsyncDisposable
{
    /// <summary>
    /// Raised when a plugin is successfully loaded.
    /// </summary>
    event EventHandler<PluginEventArgs>? PluginLoaded;

    /// <summary>
    /// Raised when a plugin is unloaded.
    /// </summary>
    event EventHandler<PluginEventArgs>? PluginUnloaded;

    /// <summary>
    /// Raised when a plugin fails to load.
    /// </summary>
    event EventHandler<PluginLoadFailedEventArgs>? PluginLoadFailed;

    /// <summary>
    /// Gets all currently loaded plugins.
    /// </summary>
    IReadOnlyList<IPluginContext> GetLoadedPlugins();

    /// <summary>
    /// Gets a loaded plugin by its ID.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <returns>The plugin context if found; otherwise, null.</returns>
    IPluginContext? GetPlugin(string pluginId);

    /// <summary>
    /// Checks if a plugin with the specified ID is loaded.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <returns>True if loaded; otherwise, false.</returns>
    bool IsLoaded(string pluginId);

    /// <summary>
    /// Loads a plugin from the specified path.
    /// </summary>
    /// <param name="path">The path to the plugin assembly.</param>
    /// <param name="options">Optional loading options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded plugin context.</returns>
    Task<IPluginContext> LoadAsync(
        string path,
        PluginLoadOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unloads a plugin by its ID.
    /// </summary>
    /// <param name="pluginId">The plugin ID to unload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UnloadAsync(string pluginId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reloads a plugin by unloading and loading it again.
    /// </summary>
    /// <param name="pluginId">The plugin ID to reload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new plugin context after reload.</returns>
    Task<IPluginContext> ReloadAsync(string pluginId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers and loads all plugins from configured discovery paths.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded plugin contexts.</returns>
    Task<IReadOnlyList<IPluginContext>> DiscoverAndLoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a type from any loaded plugin by its reference string.
    /// </summary>
    /// <param name="typeReference">The type reference (e.g., "plugin:PluginId/alias" or "plugin:PluginId/Full.Type.Name").</param>
    /// <returns>The type if found; otherwise, null.</returns>
    Type? ResolveType(string typeReference);
}
