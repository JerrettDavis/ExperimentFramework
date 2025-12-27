using ExperimentFramework.Plugins.Abstractions;

namespace ExperimentFramework.Plugins.Configuration;

/// <summary>
/// Configuration options for the plugin system.
/// </summary>
public sealed class PluginConfigurationOptions
{
    /// <summary>
    /// Gets or sets the paths to search for plugins.
    /// Supports file paths, directory paths, and glob patterns.
    /// </summary>
    public List<string> DiscoveryPaths { get; set; } = [];

    /// <summary>
    /// Gets or sets the default isolation mode for plugins.
    /// </summary>
    public PluginIsolationMode DefaultIsolationMode { get; set; } = PluginIsolationMode.Shared;

    /// <summary>
    /// Gets or sets the default assemblies to share with plugins.
    /// </summary>
    public List<string> DefaultSharedAssemblies { get; set; } = [];

    /// <summary>
    /// Gets or sets whether to enable hot reload support.
    /// </summary>
    public bool EnableHotReload { get; set; }

    /// <summary>
    /// Gets or sets the debounce interval for hot reload in milliseconds.
    /// </summary>
    public int HotReloadDebounceMs { get; set; } = 500;

    /// <summary>
    /// Gets or sets whether to auto-discover and load plugins on startup.
    /// </summary>
    public bool AutoLoadOnStartup { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to force isolation for all plugins regardless of manifest settings.
    /// </summary>
    public bool ForceIsolation { get; set; }

    /// <summary>
    /// Gets or sets whether to enable collectible mode for plugin unloading.
    /// </summary>
    public bool EnableUnloading { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate manifests strictly.
    /// When false, plugins with warnings can still be loaded.
    /// </summary>
    public bool StrictManifestValidation { get; set; }
}

/// <summary>
/// Configuration model for plugins section in YAML/JSON configuration.
/// </summary>
public sealed record PluginsConfig
{
    /// <summary>
    /// Gets or sets the discovery configuration.
    /// </summary>
    public PluginDiscoveryConfig? Discovery { get; init; }

    /// <summary>
    /// Gets or sets the default settings for plugins.
    /// </summary>
    public PluginDefaultsConfig? Defaults { get; init; }

    /// <summary>
    /// Gets or sets the hot reload configuration.
    /// </summary>
    public PluginHotReloadConfig? HotReload { get; init; }

    /// <summary>
    /// Converts to <see cref="PluginConfigurationOptions"/>.
    /// </summary>
    public PluginConfigurationOptions ToOptions()
    {
        var options = new PluginConfigurationOptions();

        if (Discovery?.Paths is not null)
        {
            options.DiscoveryPaths.AddRange(Discovery.Paths);
        }

        if (Defaults is not null)
        {
            options.DefaultIsolationMode = Defaults.IsolationMode?.ToLowerInvariant() switch
            {
                "full" => PluginIsolationMode.Full,
                "none" => PluginIsolationMode.None,
                _ => PluginIsolationMode.Shared
            };

            if (Defaults.SharedAssemblies is not null)
            {
                options.DefaultSharedAssemblies.AddRange(Defaults.SharedAssemblies);
            }
        }

        if (HotReload is not null)
        {
            options.EnableHotReload = HotReload.Enabled ?? false;
            options.HotReloadDebounceMs = HotReload.DebounceMs ?? 500;
        }

        return options;
    }
}

/// <summary>
/// Configuration for plugin discovery.
/// </summary>
public sealed record PluginDiscoveryConfig
{
    /// <summary>
    /// Gets or sets the paths to search for plugins.
    /// </summary>
    public List<string>? Paths { get; init; }
}

/// <summary>
/// Configuration for plugin defaults.
/// </summary>
public sealed record PluginDefaultsConfig
{
    /// <summary>
    /// Gets or sets the default isolation mode.
    /// </summary>
    public string? IsolationMode { get; init; }

    /// <summary>
    /// Gets or sets the default shared assemblies.
    /// </summary>
    public List<string>? SharedAssemblies { get; init; }
}

/// <summary>
/// Configuration for plugin hot reload.
/// </summary>
public sealed record PluginHotReloadConfig
{
    /// <summary>
    /// Gets or sets whether hot reload is enabled.
    /// </summary>
    public bool? Enabled { get; init; }

    /// <summary>
    /// Gets or sets the debounce interval in milliseconds.
    /// </summary>
    public int? DebounceMs { get; init; }
}
