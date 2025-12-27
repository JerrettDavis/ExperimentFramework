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

    // Security Options

    /// <summary>
    /// Gets or sets the allowed directories for plugin loading.
    /// When non-empty, only plugins from these directories (or subdirectories) can be loaded.
    /// Paths are normalized and compared case-insensitively.
    /// </summary>
    public List<string> AllowedPluginDirectories { get; set; } = [];

    /// <summary>
    /// Gets or sets whether to require assemblies to be signed with Authenticode.
    /// When true, unsigned assemblies will fail to load.
    /// </summary>
    public bool RequireSignedAssemblies { get; set; }

    /// <summary>
    /// Gets or sets the trusted publisher certificate thumbprints.
    /// When non-empty, only assemblies signed by these publishers can be loaded.
    /// Thumbprints should be uppercase hex strings without separators.
    /// </summary>
    public List<string> TrustedPublisherThumbprints { get; set; } = [];

    /// <summary>
    /// Gets or sets whether to allow UNC paths for plugin loading.
    /// Default is false for security.
    /// </summary>
    public bool AllowUncPaths { get; set; }

    /// <summary>
    /// Gets or sets the maximum manifest file size in bytes.
    /// Default is 1MB. Set to 0 to disable the limit.
    /// </summary>
    public int MaxManifestSizeBytes { get; set; } = 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum JSON depth for manifest parsing.
    /// Default is 32. Helps prevent stack overflow attacks.
    /// </summary>
    public int MaxManifestJsonDepth { get; set; } = 32;

    /// <summary>
    /// Gets or sets whether to enable audit logging for plugin operations.
    /// When enabled, load/unload/reload operations are logged at Information level.
    /// </summary>
    public bool EnableAuditLogging { get; set; }
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
    /// Gets or sets the security configuration.
    /// </summary>
    public PluginSecurityConfig? Security { get; init; }

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

        if (Security is not null)
        {
            if (Security.AllowedDirectories is not null)
            {
                options.AllowedPluginDirectories.AddRange(Security.AllowedDirectories);
            }
            options.RequireSignedAssemblies = Security.RequireSignedAssemblies ?? false;
            if (Security.TrustedThumbprints is not null)
            {
                options.TrustedPublisherThumbprints.AddRange(Security.TrustedThumbprints);
            }
            options.AllowUncPaths = Security.AllowUncPaths ?? false;
            options.MaxManifestSizeBytes = Security.MaxManifestSizeBytes ?? 1024 * 1024;
            options.EnableAuditLogging = Security.EnableAuditLogging ?? false;
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

/// <summary>
/// Configuration for plugin security settings.
/// </summary>
public sealed record PluginSecurityConfig
{
    /// <summary>
    /// Gets or sets the allowed directories for plugin loading.
    /// </summary>
    public List<string>? AllowedDirectories { get; init; }

    /// <summary>
    /// Gets or sets whether to require signed assemblies.
    /// </summary>
    public bool? RequireSignedAssemblies { get; init; }

    /// <summary>
    /// Gets or sets the trusted publisher certificate thumbprints.
    /// </summary>
    public List<string>? TrustedThumbprints { get; init; }

    /// <summary>
    /// Gets or sets whether to allow UNC paths.
    /// </summary>
    public bool? AllowUncPaths { get; init; }

    /// <summary>
    /// Gets or sets the maximum manifest size in bytes.
    /// </summary>
    public int? MaxManifestSizeBytes { get; init; }

    /// <summary>
    /// Gets or sets whether to enable audit logging.
    /// </summary>
    public bool? EnableAuditLogging { get; init; }
}
