using System.Reflection;
using System.Runtime.Loader;
using ExperimentFramework.Plugins.Abstractions;

namespace ExperimentFramework.Plugins.Loading;

/// <summary>
/// Custom AssemblyLoadContext for plugin isolation.
/// Supports shared and fully isolated modes with collectible support for unloading.
/// </summary>
public sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly string _pluginDirectory;
    private readonly SharedTypeRegistry _sharedTypeRegistry;
    private readonly PluginIsolationMode _isolationMode;
    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>
    /// Creates a new plugin load context.
    /// </summary>
    /// <param name="pluginPath">Path to the main plugin assembly.</param>
    /// <param name="isolationMode">The isolation mode for this context.</param>
    /// <param name="sharedTypeRegistry">Registry of shared types.</param>
    /// <param name="isCollectible">Whether the context can be unloaded.</param>
    public PluginLoadContext(
        string pluginPath,
        PluginIsolationMode isolationMode,
        SharedTypeRegistry sharedTypeRegistry,
        bool isCollectible = true)
        : base(name: Path.GetFileNameWithoutExtension(pluginPath), isCollectible: isCollectible)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginPath);
        ArgumentNullException.ThrowIfNull(sharedTypeRegistry);

        _pluginDirectory = Path.GetDirectoryName(pluginPath) ?? throw new ArgumentException("Invalid plugin path", nameof(pluginPath));
        _isolationMode = isolationMode;
        _sharedTypeRegistry = sharedTypeRegistry;
        _resolver = new AssemblyDependencyResolver(pluginPath);

        PluginPath = pluginPath;
    }

    /// <summary>
    /// Gets the path to the main plugin assembly.
    /// </summary>
    public string PluginPath { get; }

    /// <summary>
    /// Loads the main plugin assembly.
    /// </summary>
    /// <returns>The loaded assembly.</returns>
    public Assembly LoadMainAssembly()
    {
        return LoadFromAssemblyPath(PluginPath);
    }

    /// <inheritdoc />
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // For None isolation, always defer to default context
        if (_isolationMode == PluginIsolationMode.None)
        {
            return null; // Defer to default context
        }

        // For Shared isolation, check if this assembly should be shared
        if (_isolationMode == PluginIsolationMode.Shared &&
            _sharedTypeRegistry.TryGetSharedAssembly(assemblyName, out var sharedAssembly))
        {
            return sharedAssembly;
        }

        // For Full isolation, or non-shared assemblies in Shared mode,
        // try to load from the plugin directory
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath is not null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        // Try loading from plugin directory directly
        var directPath = Path.Combine(_pluginDirectory, $"{assemblyName.Name}.dll");
        if (File.Exists(directPath))
        {
            return LoadFromAssemblyPath(directPath);
        }

        // Fall back to default context for assemblies not found locally
        // This handles framework assemblies in all modes
        return null;
    }

    /// <inheritdoc />
    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath is not null
            ? LoadUnmanagedDllFromPath(libraryPath)
            : IntPtr.Zero;
    }
}
