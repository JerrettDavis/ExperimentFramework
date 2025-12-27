using System.Collections.Concurrent;
using System.Reflection;
using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ExperimentFramework.Plugins.Integration;

/// <summary>
/// Central manager for coordinating plugin operations.
/// </summary>
public sealed class PluginManager : IPluginManager
{
    private readonly IPluginLoader _loader;
    private readonly PluginConfigurationOptions _options;
    private readonly ILogger<PluginManager> _logger;
    private readonly ConcurrentDictionary<string, IPluginContext> _plugins = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _pathToIdMapping = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _pluginLoadTimes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, PluginLoadFailure> _failedLoads = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private const int MaxFailedLoadsToTrack = 100;
    private bool _disposed;

    /// <summary>
    /// Creates a new plugin manager.
    /// </summary>
    /// <param name="loader">The plugin loader.</param>
    /// <param name="options">Plugin configuration options.</param>
    /// <param name="logger">Optional logger.</param>
    public PluginManager(
        IPluginLoader loader,
        IOptions<PluginConfigurationOptions> options,
        ILogger<PluginManager>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(options);

        _loader = loader;
        _options = options.Value;
        _logger = logger ?? NullLogger<PluginManager>.Instance;
    }

    /// <summary>
    /// Creates a new plugin manager with default options.
    /// </summary>
    /// <param name="loader">The plugin loader.</param>
    /// <param name="logger">Optional logger.</param>
    public PluginManager(IPluginLoader loader, ILogger<PluginManager>? logger = null)
        : this(loader, Options.Create(new PluginConfigurationOptions()), logger)
    {
    }

    /// <inheritdoc />
    public event EventHandler<PluginEventArgs>? PluginLoaded;

    /// <inheritdoc />
    public event EventHandler<PluginEventArgs>? PluginUnloaded;

    /// <inheritdoc />
    public event EventHandler<PluginLoadFailedEventArgs>? PluginLoadFailed;

    /// <inheritdoc />
    public IReadOnlyList<IPluginContext> GetLoadedPlugins()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return [.. _plugins.Values];
    }

    /// <inheritdoc />
    public IPluginContext? GetPlugin(string pluginId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);

        return _plugins.GetValueOrDefault(pluginId);
    }

    /// <inheritdoc />
    public bool IsLoaded(string pluginId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        return _plugins.ContainsKey(pluginId);
    }

    /// <inheritdoc />
    public async Task<IPluginContext> LoadAsync(
        string path,
        PluginLoadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var fullPath = Path.GetFullPath(path);

        await _loadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Check if already loaded from this path
            if (_pathToIdMapping.TryGetValue(fullPath, out var existingId) &&
                _plugins.ContainsKey(existingId))
            {
                _logger.LogDebug("Plugin from {Path} already loaded as {PluginId}", fullPath, existingId);
                return _plugins[existingId];
            }

            // Apply default options
            options = ApplyDefaultOptions(options);

            _logger.LogInformation("Loading plugin from {Path}", fullPath);

            IPluginContext context;
            try
            {
                context = await _loader.LoadAsync(fullPath, options, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from {Path}", fullPath);

                // Track failed load
                TrackFailedLoad(fullPath, ex.Message);

                PluginLoadFailed?.Invoke(this, new PluginLoadFailedEventArgs(fullPath, ex));
                throw;
            }

            // Check for duplicate plugin ID
            if (_plugins.ContainsKey(context.Manifest.Id))
            {
                await context.DisposeAsync().ConfigureAwait(false);
                throw new InvalidOperationException(
                    $"A plugin with ID '{context.Manifest.Id}' is already loaded.");
            }

            _plugins[context.Manifest.Id] = context;
            _pathToIdMapping[fullPath] = context.Manifest.Id;
            _pluginLoadTimes[context.Manifest.Id] = DateTimeOffset.UtcNow;

            // Clear any previous failed load for this path
            _failedLoads.TryRemove(fullPath, out _);

            _logger.LogInformation(
                "Successfully loaded plugin {PluginId} v{Version} from {Path}",
                context.Manifest.Id,
                context.Manifest.Version,
                fullPath);

            PluginLoaded?.Invoke(this, new PluginEventArgs(context));

            return context;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UnloadAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);

        await _loadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_plugins.TryRemove(pluginId, out var context))
            {
                _logger.LogWarning("Plugin {PluginId} not found for unloading", pluginId);
                return;
            }

            // Remove path mapping
            var pathEntry = _pathToIdMapping
                .FirstOrDefault(kvp => kvp.Value.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
            if (pathEntry.Key is not null)
            {
                _pathToIdMapping.TryRemove(pathEntry.Key, out _);
            }

            _logger.LogInformation("Unloading plugin {PluginId}", pluginId);

            await _loader.UnloadAsync(context, cancellationToken).ConfigureAwait(false);

            PluginUnloaded?.Invoke(this, new PluginEventArgs(context));

            _logger.LogInformation("Plugin {PluginId} unloaded successfully", pluginId);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IPluginContext> ReloadAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);

        await _loadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_plugins.TryGetValue(pluginId, out var existingContext))
            {
                throw new InvalidOperationException($"Plugin '{pluginId}' is not loaded.");
            }

            var pluginPath = existingContext.PluginPath;
            _logger.LogInformation("Reloading plugin {PluginId} from {Path}", pluginId, pluginPath);

            // Unload existing
            _plugins.TryRemove(pluginId, out _);
            await _loader.UnloadAsync(existingContext, cancellationToken).ConfigureAwait(false);
            PluginUnloaded?.Invoke(this, new PluginEventArgs(existingContext));

            // Small delay to ensure cleanup
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            // Load again
            var options = ApplyDefaultOptions(null);
            var newContext = await _loader.LoadAsync(pluginPath, options, cancellationToken).ConfigureAwait(false);

            _plugins[newContext.Manifest.Id] = newContext;
            _pathToIdMapping[pluginPath] = newContext.Manifest.Id;

            _logger.LogInformation(
                "Plugin {PluginId} reloaded successfully (new version: {Version})",
                newContext.Manifest.Id,
                newContext.Manifest.Version);

            PluginLoaded?.Invoke(this, new PluginEventArgs(newContext));

            return newContext;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IPluginContext>> DiscoverAndLoadAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var discoveredPlugins = new List<IPluginContext>();
        var pluginPaths = DiscoverPluginPaths();

        _logger.LogInformation("Discovered {Count} potential plugins", pluginPaths.Count);

        foreach (var path in pluginPaths)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var context = await LoadAsync(path, null, cancellationToken).ConfigureAwait(false);
                discoveredPlugins.Add(context);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load discovered plugin: {Path}", path);
                // Continue with other plugins
            }
        }

        return discoveredPlugins;
    }

    /// <inheritdoc />
    public Type? ResolveType(string typeReference)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(typeReference))
        {
            return null;
        }

        // Parse plugin:PluginId/alias or plugin:PluginId/Full.Type.Name
        if (!typeReference.StartsWith("plugin:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var reference = typeReference[7..]; // Remove "plugin:" prefix
        var separatorIndex = reference.IndexOf('/');

        if (separatorIndex <= 0)
        {
            return null;
        }

        var pluginId = reference[..separatorIndex];
        var typeIdentifier = reference[(separatorIndex + 1)..];

        if (!_plugins.TryGetValue(pluginId, out var context))
        {
            _logger.LogWarning("Plugin {PluginId} not found for type resolution", pluginId);
            return null;
        }

        // Try as alias first, then as type name
        var type = context.GetTypeByAlias(typeIdentifier)
                   ?? context.GetType(typeIdentifier);

        if (type is null)
        {
            _logger.LogWarning(
                "Type '{TypeIdentifier}' not found in plugin {PluginId}",
                typeIdentifier,
                pluginId);
        }

        return type;
    }

    /// <inheritdoc />
    public Task<PluginSystemHealth> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var pluginDetails = new List<PluginHealthDetails>();
        var healthyCount = 0;
        var degradedCount = 0;
        var unhealthyCount = 0;

        foreach (var (pluginId, context) in _plugins)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var loadTime = _pluginLoadTimes.GetValueOrDefault(pluginId, DateTimeOffset.UtcNow);
            var warnings = new List<string>();
            var state = PluginHealthState.Healthy;

            // Check for potential issues
            try
            {
                var typeCount = 0;
                foreach (var assembly in context.LoadedAssemblies)
                {
                    try
                    {
                        typeCount += assembly.GetExportedTypes().Length;
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        warnings.Add($"Some types could not be loaded from {assembly.GetName().Name}");
                        typeCount += ex.Types.Count(t => t is not null);
                        state = PluginHealthState.Warning;
                    }
                }

                var details = new PluginHealthDetails(
                    PluginId: pluginId,
                    State: state,
                    Version: context.Manifest.Version.ToString(),
                    LoadedAt: loadTime,
                    AssemblyCount: context.LoadedAssemblies.Count,
                    TypeCount: typeCount,
                    IsolationMode: context.Manifest.Isolation.Mode.ToString(),
                    Message: state == PluginHealthState.Healthy ? "Plugin is operating normally" : null,
                    Warnings: warnings.Count > 0 ? warnings : null);

                pluginDetails.Add(details);

                if (state == PluginHealthState.Healthy)
                    healthyCount++;
                else if (state == PluginHealthState.Warning)
                    degradedCount++;
                else
                    unhealthyCount++;
            }
            catch (Exception ex)
            {
                pluginDetails.Add(new PluginHealthDetails(
                    PluginId: pluginId,
                    State: PluginHealthState.Error,
                    Version: context.Manifest.Version.ToString(),
                    LoadedAt: loadTime,
                    AssemblyCount: context.LoadedAssemblies.Count,
                    TypeCount: 0,
                    IsolationMode: context.Manifest.Isolation.Mode.ToString(),
                    Message: $"Health check failed: {ex.Message}",
                    Warnings: null));
                unhealthyCount++;
            }
        }

        var overallState = unhealthyCount > 0 ? PluginSystemHealthState.Unhealthy
            : degradedCount > 0 ? PluginSystemHealthState.Degraded
            : PluginSystemHealthState.Healthy;

        var failedLoads = _failedLoads.Values.ToList();

        var health = new PluginSystemHealth(
            State: overallState,
            TotalPlugins: _plugins.Count,
            HealthyPlugins: healthyCount,
            DegradedPlugins: degradedCount,
            UnhealthyPlugins: unhealthyCount,
            HotReloadEnabled: _options.EnableHotReload,
            Message: overallState == PluginSystemHealthState.Healthy
                ? $"All {_plugins.Count} plugins are healthy"
                : overallState == PluginSystemHealthState.Degraded
                    ? $"{degradedCount} plugin(s) have warnings"
                    : $"{unhealthyCount} plugin(s) are unhealthy",
            Plugins: pluginDetails,
            FailedLoads: failedLoads);

        return Task.FromResult(health);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _logger.LogDebug("Disposing plugin manager, unloading {Count} plugins", _plugins.Count);

        foreach (var context in _plugins.Values)
        {
            try
            {
                await _loader.UnloadAsync(context, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unloading plugin {PluginId} during disposal", context.Manifest.Id);
            }
        }

        _plugins.Clear();
        _pathToIdMapping.Clear();
        _pluginLoadTimes.Clear();
        _failedLoads.Clear();
        _loadLock.Dispose();
    }

    private PluginLoadOptions ApplyDefaultOptions(PluginLoadOptions? options)
    {
        options ??= new PluginLoadOptions();

        // Merge with default options
        if (options.IsolationModeOverride is null && _options.DefaultIsolationMode != PluginIsolationMode.Shared)
        {
            options = options with { IsolationModeOverride = _options.DefaultIsolationMode };
        }

        if (options.AdditionalSharedAssemblies.Count == 0 && _options.DefaultSharedAssemblies.Count > 0)
        {
            options = options with { AdditionalSharedAssemblies = _options.DefaultSharedAssemblies };
        }

        return options;
    }

    private List<string> DiscoverPluginPaths()
    {
        var paths = new List<string>();

        foreach (var discoveryPath in _options.DiscoveryPaths)
        {
            if (discoveryPath.Contains('*'))
            {
                // Glob pattern
                paths.AddRange(ExpandGlobPattern(discoveryPath));
            }
            else if (Directory.Exists(discoveryPath))
            {
                // Directory - find all .dll files
                paths.AddRange(Directory.GetFiles(discoveryPath, "*.dll", SearchOption.TopDirectoryOnly));
            }
            else if (File.Exists(discoveryPath))
            {
                // Direct file path
                paths.Add(discoveryPath);
            }
        }

        return paths
            .Select(Path.GetFullPath)
            .Where(_loader.CanLoad)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> ExpandGlobPattern(string pattern)
    {
        var directory = Path.GetDirectoryName(pattern) ?? ".";
        var filePattern = Path.GetFileName(pattern);

        // Simple glob expansion - handle ** for recursive search
        var searchOption = pattern.Contains("**")
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        // Normalize directory for ** patterns
        if (directory.Contains("**"))
        {
            directory = directory[..directory.IndexOf("**", StringComparison.Ordinal)].TrimEnd('/', '\\');
            if (string.IsNullOrEmpty(directory))
            {
                directory = ".";
            }
        }

        if (!Directory.Exists(directory))
        {
            return [];
        }

        // Convert glob pattern to regex safely by escaping all regex metacharacters first
        var regexPattern = System.Text.RegularExpressions.Regex.Escape(filePattern);

        // Then convert glob wildcards to regex equivalents
        regexPattern = regexPattern
            .Replace(@"\*\*", ".*")     // ** matches anything including path separators
            .Replace(@"\*", "[^/\\\\]*") // * matches any chars except path separators
            .Replace(@"\?", ".");        // ? matches single character

        try
        {
            // Use a timeout to prevent ReDoS attacks
            var regex = new System.Text.RegularExpressions.Regex(
                $"^{regexPattern}$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase,
                TimeSpan.FromSeconds(1));

            return Directory.GetFiles(directory, "*.dll", searchOption)
                .Where(f =>
                {
                    try
                    {
                        return regex.IsMatch(Path.GetFileName(f));
                    }
                    catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
                    {
                        return false;
                    }
                });
        }
        catch (Exception)
        {
            return [];
        }
    }

    private void TrackFailedLoad(string pluginPath, string errorMessage)
    {
        var failure = new PluginLoadFailure(pluginPath, errorMessage, DateTimeOffset.UtcNow);
        _failedLoads[pluginPath] = failure;

        // Limit the number of tracked failures to prevent memory growth
        if (_failedLoads.Count > MaxFailedLoadsToTrack)
        {
            var oldestKey = _failedLoads
                .OrderBy(kvp => kvp.Value.FailedAt)
                .First()
                .Key;
            _failedLoads.TryRemove(oldestKey, out _);
        }
    }
}
