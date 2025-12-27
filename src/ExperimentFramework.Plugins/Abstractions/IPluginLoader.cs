namespace ExperimentFramework.Plugins.Abstractions;

/// <summary>
/// Options for loading a plugin.
/// </summary>
public sealed record PluginLoadOptions
{
    /// <summary>
    /// Gets or sets the isolation mode override.
    /// If null, uses the mode specified in the plugin manifest.
    /// </summary>
    public PluginIsolationMode? IsolationModeOverride { get; init; }

    /// <summary>
    /// Gets or sets additional assemblies to share with the plugin.
    /// These are merged with the manifest's shared assemblies.
    /// </summary>
    public IReadOnlyList<string> AdditionalSharedAssemblies { get; init; } = [];

    /// <summary>
    /// Gets or sets whether to force isolation regardless of manifest settings.
    /// When true, the plugin cannot request shared access.
    /// </summary>
    public bool ForceIsolation { get; init; }

    /// <summary>
    /// Gets or sets whether to enable collectible mode for unloading support.
    /// Default is true.
    /// </summary>
    public bool EnableUnloading { get; init; } = true;

    /// <summary>
    /// Gets or sets custom metadata to associate with the loaded plugin.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Responsible for loading and unloading plugins.
/// </summary>
public interface IPluginLoader
{
    /// <summary>
    /// Loads a plugin from the specified path.
    /// </summary>
    /// <param name="pluginPath">The path to the plugin assembly.</param>
    /// <param name="options">Loading options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded plugin context.</returns>
    Task<IPluginContext> LoadAsync(
        string pluginPath,
        PluginLoadOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unloads a plugin context.
    /// </summary>
    /// <param name="context">The plugin context to unload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UnloadAsync(IPluginContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a plugin can be loaded from the specified path.
    /// </summary>
    /// <param name="pluginPath">The path to check.</param>
    /// <returns>True if the plugin can be loaded; otherwise, false.</returns>
    bool CanLoad(string pluginPath);
}
