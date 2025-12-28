using System.Collections.Concurrent;
using System.Reflection;
using ExperimentFramework.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Plugins.Loading;

/// <summary>
/// Concrete implementation of <see cref="IPluginContext"/>.
/// </summary>
public sealed class PluginContext : IPluginContext
{
    private readonly PluginLoadContext? _loadContext;
    private readonly ILogger? _logger;
    private readonly ConcurrentDictionary<string, Type> _aliasCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Type, List<Type>> _implementationCache = new();
    private volatile bool _isLoaded = true;
    private bool _disposed;

    internal PluginContext(
        string contextId,
        IPluginManifest manifest,
        string pluginPath,
        Assembly mainAssembly,
        IReadOnlyList<Assembly> loadedAssemblies,
        PluginLoadContext? loadContext,
        ILogger? logger = null)
    {
        ContextId = contextId;
        Manifest = manifest;
        PluginPath = pluginPath;
        MainAssembly = mainAssembly;
        LoadedAssemblies = loadedAssemblies;
        _loadContext = loadContext;
        _logger = logger;

        BuildAliasCache();
    }

    /// <inheritdoc />
    public string ContextId { get; }

    /// <inheritdoc />
    public IPluginManifest Manifest { get; }

    /// <inheritdoc />
    public bool IsLoaded => _isLoaded;

    /// <inheritdoc />
    public string PluginPath { get; }

    /// <inheritdoc />
    public Assembly? MainAssembly { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<Assembly> LoadedAssemblies { get; }

    /// <inheritdoc />
    public Type? GetType(string typeName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_isLoaded || MainAssembly is null)
        {
            return null;
        }

        // Try main assembly first
        var type = MainAssembly.GetType(typeName);
        if (type is not null)
        {
            return type;
        }

        // Try all loaded assemblies
        foreach (var assembly in LoadedAssemblies)
        {
            type = assembly.GetType(typeName);
            if (type is not null)
            {
                return type;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public Type? GetTypeByAlias(string alias)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_isLoaded)
        {
            return null;
        }

        return _aliasCache.GetValueOrDefault(alias);
    }

    /// <inheritdoc />
    public IEnumerable<Type> GetImplementations(Type interfaceType)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_isLoaded || MainAssembly is null)
        {
            return [];
        }

        return _implementationCache.GetOrAdd(interfaceType, FindImplementations);
    }

    /// <inheritdoc />
    public IEnumerable<Type> GetImplementations<TInterface>()
    {
        return GetImplementations(typeof(TInterface));
    }

    /// <inheritdoc />
    public object CreateInstance(Type type, IServiceProvider serviceProvider)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_isLoaded)
        {
            throw new InvalidOperationException($"Plugin '{Manifest.Id}' is not loaded.");
        }

        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return ActivatorUtilities.CreateInstance(serviceProvider, type);
    }

    /// <inheritdoc />
    public object? CreateInstanceByAlias(string alias, IServiceProvider serviceProvider)
    {
        var type = GetTypeByAlias(alias);
        return type is null ? null : CreateInstance(type, serviceProvider);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _isLoaded = false;
        MainAssembly = null;

        _aliasCache.Clear();
        _implementationCache.Clear();

        // Trigger unloading if collectible
        if (_loadContext is not null)
        {
            // Allow GC to collect the load context
            // The actual unload happens asynchronously when all references are released
            _loadContext.Unload();

            // Give a chance for cleanup
            // Note: Collectible context unloading is non-deterministic and depends on
            // all references being released. This delay and GC calls help but don't guarantee.
            await Task.Delay(100).ConfigureAwait(false);

            // Trigger GC to help with unloading
            for (var i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }

    private void BuildAliasCache()
    {
        foreach (var service in Manifest.Services)
        {
            foreach (var impl in service.Implementations)
            {
                if (!string.IsNullOrWhiteSpace(impl.Alias))
                {
                    var type = GetType(impl.Type);
                    if (type is not null)
                    {
                        _aliasCache.TryAdd(impl.Alias, type);
                    }
                }
            }
        }
    }

    private List<Type> FindImplementations(Type interfaceType)
    {
        var implementations = new List<Type>();

        foreach (var assembly in LoadedAssemblies)
        {
            try
            {
                var types = assembly.GetExportedTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && interfaceType.IsAssignableFrom(t));
                implementations.AddRange(types);
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Log detailed information about which types failed to load
                var loaderExceptions = ex.LoaderExceptions
                    .Where(e => e is not null)
                    .Select(e => e!.Message)
                    .Distinct();

                _logger?.LogWarning(
                    "Some types could not be loaded from assembly {Assembly} in plugin {PluginId}: {Errors}",
                    assembly.GetName().Name,
                    Manifest.Id,
                    string.Join("; ", loaderExceptions));

                // Process the types that did load successfully
                var loadedTypes = ex.Types
                    .Where(t => t is not null)
                    .Where(t => t!.IsClass && !t.IsAbstract && interfaceType.IsAssignableFrom(t));

                implementations.AddRange(loadedTypes!);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(
                    ex,
                    "Failed to discover types from assembly {Assembly} in plugin {PluginId}",
                    assembly.GetName().Name,
                    Manifest.Id);
            }
        }

        return implementations;
    }
}
