using System.Collections.Concurrent;
using ExperimentFramework.Plugins.Abstractions;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Plugins.HotReload;

/// <summary>
/// Watches plugin files for changes and triggers reloads.
/// </summary>
public sealed class PluginWatcher : IDisposable
{
    private readonly IPluginManager _pluginManager;
    private readonly ILogger<PluginWatcher> _logger;
    private readonly TimeSpan _debounceInterval;
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly ConcurrentDictionary<string, DateTime> _lastChangeTime = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pendingReloads = new();
    private bool _disposed;

    /// <summary>
    /// Creates a new plugin watcher.
    /// </summary>
    /// <param name="pluginManager">The plugin manager.</param>
    /// <param name="debounceMs">Debounce interval in milliseconds.</param>
    /// <param name="logger">Logger.</param>
    public PluginWatcher(
        IPluginManager pluginManager,
        int debounceMs = 500,
        ILogger<PluginWatcher>? logger = null)
    {
        _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
        _debounceInterval = TimeSpan.FromMilliseconds(debounceMs);
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PluginWatcher>.Instance;
    }

    /// <summary>
    /// Event raised when a plugin reload is triggered.
    /// </summary>
    public event EventHandler<PluginReloadEventArgs>? PluginReloadTriggered;

    /// <summary>
    /// Event raised when a plugin reload completes.
    /// </summary>
    public event EventHandler<PluginReloadEventArgs>? PluginReloadCompleted;

    /// <summary>
    /// Event raised when a plugin reload fails.
    /// </summary>
    public event EventHandler<PluginReloadFailedEventArgs>? PluginReloadFailed;

    /// <summary>
    /// Starts watching loaded plugins for changes.
    /// </summary>
    public void StartWatching()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var plugin in _pluginManager.GetLoadedPlugins())
        {
            WatchPlugin(plugin);
        }

        // Watch for new plugins being loaded
        _pluginManager.PluginLoaded += OnPluginLoaded;
        _pluginManager.PluginUnloaded += OnPluginUnloaded;

        _logger.LogInformation("Started watching {Count} plugins for changes", _watchers.Count);
    }

    /// <summary>
    /// Stops watching for changes.
    /// </summary>
    public void StopWatching()
    {
        _pluginManager.PluginLoaded -= OnPluginLoaded;
        _pluginManager.PluginUnloaded -= OnPluginUnloaded;

        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        _watchers.Clear();

        foreach (var cts in _pendingReloads.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }

        _pendingReloads.Clear();

        _logger.LogInformation("Stopped watching plugins for changes");
    }

    /// <summary>
    /// Watches a specific plugin directory for changes.
    /// </summary>
    /// <param name="directoryPath">The directory to watch.</param>
    /// <param name="filter">The file filter (default: *.dll).</param>
    public void WatchDirectory(string directoryPath, string filter = "*.dll")
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!Directory.Exists(directoryPath))
        {
            _logger.LogWarning("Cannot watch non-existent directory: {Path}", directoryPath);
            return;
        }

        var watcher = new FileSystemWatcher(directoryPath, filter)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true,
            IncludeSubdirectories = false
        };

        watcher.Changed += OnFileChanged;
        watcher.Created += OnFileCreated;

        _watchers.Add(watcher);

        _logger.LogDebug("Watching directory {Path} for plugin changes", directoryPath);
    }

    private void WatchPlugin(IPluginContext plugin)
    {
        var directory = Path.GetDirectoryName(plugin.PluginPath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return;
        }

        var fileName = Path.GetFileName(plugin.PluginPath);
        var watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        watcher.Changed += OnFileChanged;
        _watchers.Add(watcher);

        _logger.LogDebug("Watching plugin file: {Path}", plugin.PluginPath);
    }

    private void OnPluginLoaded(object? sender, PluginEventArgs e)
    {
        WatchPlugin(e.Context);
    }

    private void OnPluginUnloaded(object? sender, PluginEventArgs e)
    {
        // Watcher will be cleaned up on next StopWatching or Dispose
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        HandleFileChange(e.FullPath, isNew: false);
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        HandleFileChange(e.FullPath, isNew: true);
    }

    private void HandleFileChange(string filePath, bool isNew)
    {
        if (_disposed)
        {
            return;
        }

        // Debounce - ignore rapid successive changes
        var now = DateTime.UtcNow;
        if (_lastChangeTime.TryGetValue(filePath, out var lastChange) &&
            now - lastChange < _debounceInterval)
        {
            return;
        }

        _lastChangeTime[filePath] = now;

        // Cancel any pending reload for this file
        if (_pendingReloads.TryRemove(filePath, out var existingCts))
        {
            existingCts.Cancel();
            existingCts.Dispose();
        }

        // Schedule reload after debounce interval
        var cts = new CancellationTokenSource();
        _pendingReloads[filePath] = cts;

        _ = ScheduleReloadAsync(filePath, isNew, cts.Token);
    }

    private async Task ScheduleReloadAsync(string filePath, bool isNew, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_debounceInterval, cancellationToken);

            // Find the plugin for this file
            var plugin = _pluginManager.GetLoadedPlugins()
                .FirstOrDefault(p => p.PluginPath.Equals(filePath, StringComparison.OrdinalIgnoreCase));

            if (plugin is null && !isNew)
            {
                _logger.LogDebug("No loaded plugin found for changed file: {Path}", filePath);
                return;
            }

            if (plugin is not null)
            {
                await ReloadPluginAsync(plugin, cancellationToken);
            }
            else if (isNew)
            {
                await LoadNewPluginAsync(filePath, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when debounce is reset
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling file change: {Path}", filePath);
        }
        finally
        {
            _pendingReloads.TryRemove(filePath, out _);
        }
    }

    private async Task ReloadPluginAsync(IPluginContext plugin, CancellationToken cancellationToken)
    {
        var pluginId = plugin.Manifest.Id;

        if (!plugin.Manifest.Lifecycle.SupportsHotReload)
        {
            _logger.LogWarning(
                "Plugin {PluginId} does not support hot reload, skipping",
                pluginId);
            return;
        }

        _logger.LogInformation("Reloading plugin {PluginId} due to file change", pluginId);

        PluginReloadTriggered?.Invoke(this, new PluginReloadEventArgs(pluginId, plugin.PluginPath));

        try
        {
            var newContext = await _pluginManager.ReloadAsync(pluginId, cancellationToken);

            _logger.LogInformation(
                "Plugin {PluginId} reloaded successfully (version: {Version})",
                newContext.Manifest.Id,
                newContext.Manifest.Version);

            PluginReloadCompleted?.Invoke(this, new PluginReloadEventArgs(pluginId, plugin.PluginPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload plugin {PluginId}", pluginId);
            PluginReloadFailed?.Invoke(this, new PluginReloadFailedEventArgs(pluginId, plugin.PluginPath, ex));
        }
    }

    private async Task LoadNewPluginAsync(string filePath, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading new plugin from: {Path}", filePath);

        try
        {
            var context = await _pluginManager.LoadAsync(filePath, null, cancellationToken);

            _logger.LogInformation(
                "New plugin {PluginId} loaded successfully",
                context.Manifest.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load new plugin from: {Path}", filePath);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopWatching();
    }
}

/// <summary>
/// Event arguments for plugin reload events.
/// </summary>
public sealed class PluginReloadEventArgs(string pluginId, string pluginPath) : EventArgs
{
    /// <summary>
    /// Gets the plugin ID.
    /// </summary>
    public string PluginId { get; } = pluginId;

    /// <summary>
    /// Gets the plugin path.
    /// </summary>
    public string PluginPath { get; } = pluginPath;
}

/// <summary>
/// Event arguments for plugin reload failures.
/// </summary>
public sealed class PluginReloadFailedEventArgs(string pluginId, string pluginPath, Exception exception) : EventArgs
{
    /// <summary>
    /// Gets the plugin ID.
    /// </summary>
    public string PluginId { get; } = pluginId;

    /// <summary>
    /// Gets the plugin path.
    /// </summary>
    public string PluginPath { get; } = pluginPath;

    /// <summary>
    /// Gets the exception that caused the failure.
    /// </summary>
    public Exception Exception { get; } = exception;
}
