using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExperimentFramework.Plugins.HotReload;

/// <summary>
/// Background service that enables hot reload for plugins.
/// </summary>
public sealed class PluginReloadService : IHostedService, IDisposable
{
    private readonly IPluginManager _pluginManager;
    private readonly PluginConfigurationOptions _options;
    private readonly ILogger<PluginReloadService> _logger;
    private PluginWatcher? _watcher;

    /// <summary>
    /// Creates a new plugin reload service.
    /// </summary>
    /// <param name="pluginManager">The plugin manager.</param>
    /// <param name="options">Configuration options.</param>
    /// <param name="logger">Logger.</param>
    public PluginReloadService(
        IPluginManager pluginManager,
        IOptions<PluginConfigurationOptions> options,
        ILogger<PluginReloadService> logger)
    {
        _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.EnableHotReload)
        {
            _logger.LogDebug("Hot reload is disabled");
            return Task.CompletedTask;
        }

        _watcher = new PluginWatcher(
            _pluginManager,
            _options.HotReloadDebounceMs,
            _logger as ILogger<PluginWatcher>);

        _watcher.PluginReloadTriggered += OnReloadTriggered;
        _watcher.PluginReloadCompleted += OnReloadCompleted;
        _watcher.PluginReloadFailed += OnReloadFailed;

        // Watch configured discovery paths
        foreach (var path in _options.DiscoveryPaths)
        {
            var directory = path.Contains('*')
                ? Path.GetDirectoryName(path) ?? "."
                : Directory.Exists(path) ? path : Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                _watcher.WatchDirectory(directory);
            }
        }

        // Start watching loaded plugins
        _watcher.StartWatching();

        _logger.LogInformation("Plugin hot reload service started");

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _watcher?.StopWatching();
        _logger.LogInformation("Plugin hot reload service stopped");
        return Task.CompletedTask;
    }

    private void OnReloadTriggered(object? sender, PluginReloadEventArgs e)
    {
        _logger.LogDebug("Plugin reload triggered: {PluginId}", e.PluginId);
    }

    private void OnReloadCompleted(object? sender, PluginReloadEventArgs e)
    {
        _logger.LogInformation("Plugin {PluginId} reloaded successfully", e.PluginId);
    }

    private void OnReloadFailed(object? sender, PluginReloadFailedEventArgs e)
    {
        _logger.LogError(
            e.Exception,
            "Failed to reload plugin {PluginId}: {Message}",
            e.PluginId,
            e.Exception.Message);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_watcher is not null)
        {
            _watcher.PluginReloadTriggered -= OnReloadTriggered;
            _watcher.PluginReloadCompleted -= OnReloadCompleted;
            _watcher.PluginReloadFailed -= OnReloadFailed;
            _watcher.Dispose();
            _watcher = null;
        }
    }
}
