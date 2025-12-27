using System.Reflection;
using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Manifest;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExperimentFramework.Plugins.Loading;

/// <summary>
/// Default implementation of <see cref="IPluginLoader"/>.
/// </summary>
public sealed class PluginLoader : IPluginLoader
{
    private readonly ManifestLoader _manifestLoader;
    private readonly ManifestValidator _manifestValidator;
    private readonly SharedTypeRegistry _defaultSharedRegistry;
    private readonly ILogger<PluginLoader> _logger;

    /// <summary>
    /// Creates a new plugin loader with the specified shared type registry.
    /// </summary>
    /// <param name="sharedTypeRegistry">The shared type registry.</param>
    /// <param name="logger">Optional logger.</param>
    public PluginLoader(
        SharedTypeRegistry? sharedTypeRegistry = null,
        ILogger<PluginLoader>? logger = null)
    {
        _manifestLoader = new ManifestLoader();
        _manifestValidator = new ManifestValidator();
        _defaultSharedRegistry = sharedTypeRegistry ?? new SharedTypeRegistry();
        _logger = logger ?? NullLogger<PluginLoader>.Instance;
    }

    /// <inheritdoc />
    public Task<IPluginContext> LoadAsync(
        string pluginPath,
        PluginLoadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginPath);

        if (!File.Exists(pluginPath))
        {
            throw new FileNotFoundException($"Plugin assembly not found: {pluginPath}", pluginPath);
        }

        cancellationToken.ThrowIfCancellationRequested();

        options ??= new PluginLoadOptions();

        var context = LoadPlugin(pluginPath, options);
        return Task.FromResult(context);
    }

    /// <inheritdoc />
    public async Task UnloadAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation("Unloading plugin {PluginId}", context.Manifest.Id);

        await context.DisposeAsync();

        _logger.LogInformation("Plugin {PluginId} unloaded", context.Manifest.Id);
    }

    /// <inheritdoc />
    public bool CanLoad(string pluginPath)
    {
        if (string.IsNullOrWhiteSpace(pluginPath))
        {
            return false;
        }

        if (!File.Exists(pluginPath))
        {
            return false;
        }

        var extension = Path.GetExtension(pluginPath);
        return extension.Equals(".dll", StringComparison.OrdinalIgnoreCase);
    }

    private IPluginContext LoadPlugin(string pluginPath, PluginLoadOptions options)
    {
        var fullPath = Path.GetFullPath(pluginPath);
        var contextId = Guid.NewGuid().ToString("N");

        _logger.LogDebug("Loading plugin from {Path} with context {ContextId}", fullPath, contextId);

        // Determine isolation mode
        var sharedRegistry = BuildSharedRegistry(options);
        var isolationMode = DetermineIsolationMode(null, options); // We'll refine after loading manifest

        // For None isolation, load into default context
        if (isolationMode == PluginIsolationMode.None)
        {
            return LoadIntoDefaultContext(fullPath, contextId, options);
        }

        // Create isolated context
        var loadContext = new PluginLoadContext(
            fullPath,
            isolationMode,
            sharedRegistry,
            options.EnableUnloading);

        try
        {
            var mainAssembly = loadContext.LoadMainAssembly();
            var manifest = _manifestLoader.Load(mainAssembly, fullPath);

            // Refine isolation mode now that we have the manifest
            var finalIsolationMode = DetermineIsolationMode(manifest, options);
            if (finalIsolationMode == PluginIsolationMode.None)
            {
                // Need to reload in default context
                loadContext.Unload();
                return LoadIntoDefaultContext(fullPath, contextId, options);
            }

            // Validate manifest
            var validationResult = _manifestValidator.Validate(manifest);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors);
                throw new InvalidOperationException($"Invalid plugin manifest: {errors}");
            }

            foreach (var warning in validationResult.Warnings)
            {
                _logger.LogWarning("Plugin manifest warning for {PluginId}: {Warning}", manifest.Id, warning);
            }

            var loadedAssemblies = CollectLoadedAssemblies(loadContext, mainAssembly);

            var context = new PluginContext(
                contextId,
                manifest,
                fullPath,
                mainAssembly,
                loadedAssemblies,
                loadContext);

            _logger.LogInformation(
                "Loaded plugin {PluginId} v{Version} with {Count} assemblies (isolation: {Mode})",
                manifest.Id,
                manifest.Version,
                loadedAssemblies.Count,
                finalIsolationMode);

            return context;
        }
        catch
        {
            loadContext.Unload();
            throw;
        }
    }

    private IPluginContext LoadIntoDefaultContext(string fullPath, string contextId, PluginLoadOptions options)
    {
        _logger.LogDebug("Loading plugin into default context: {Path}", fullPath);

        var mainAssembly = Assembly.LoadFrom(fullPath);
        var manifest = _manifestLoader.Load(mainAssembly, fullPath);

        var validationResult = _manifestValidator.Validate(manifest);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors);
            throw new InvalidOperationException($"Invalid plugin manifest: {errors}");
        }

        var context = new PluginContext(
            contextId,
            manifest,
            fullPath,
            mainAssembly,
            [mainAssembly],
            null); // No custom load context

        _logger.LogInformation(
            "Loaded plugin {PluginId} v{Version} into default context",
            manifest.Id,
            manifest.Version);

        return context;
    }

    private SharedTypeRegistry BuildSharedRegistry(PluginLoadOptions options)
    {
        if (options.AdditionalSharedAssemblies.Count == 0)
        {
            return _defaultSharedRegistry;
        }

        return new SharedTypeRegistry(options.AdditionalSharedAssemblies);
    }

    private static PluginIsolationMode DetermineIsolationMode(IPluginManifest? manifest, PluginLoadOptions options)
    {
        // Force isolation overrides everything
        if (options.ForceIsolation)
        {
            return PluginIsolationMode.Full;
        }

        // Explicit override from options
        if (options.IsolationModeOverride.HasValue)
        {
            return options.IsolationModeOverride.Value;
        }

        // Use manifest setting if available
        return manifest?.Isolation.Mode ?? PluginIsolationMode.Shared;
    }

    private static List<Assembly> CollectLoadedAssemblies(PluginLoadContext context, Assembly mainAssembly)
    {
        var assemblies = new List<Assembly> { mainAssembly };

        // Get all assemblies loaded into this context
        foreach (var assembly in context.Assemblies)
        {
            if (!assemblies.Contains(assembly))
            {
                assemblies.Add(assembly);
            }
        }

        return assemblies;
    }
}
