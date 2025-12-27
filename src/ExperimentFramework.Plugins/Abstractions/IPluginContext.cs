using System.Reflection;

namespace ExperimentFramework.Plugins.Abstractions;

/// <summary>
/// Represents the runtime context of a loaded plugin.
/// Provides access to plugin metadata, types, and instance creation.
/// </summary>
public interface IPluginContext : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique identifier for this plugin context.
    /// </summary>
    string ContextId { get; }

    /// <summary>
    /// Gets the plugin manifest.
    /// </summary>
    IPluginManifest Manifest { get; }

    /// <summary>
    /// Gets whether the plugin is currently loaded.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Gets the path from which the plugin was loaded.
    /// </summary>
    string PluginPath { get; }

    /// <summary>
    /// Gets the main assembly of the plugin.
    /// </summary>
    Assembly? MainAssembly { get; }

    /// <summary>
    /// Gets all assemblies loaded by this plugin context.
    /// </summary>
    IReadOnlyList<Assembly> LoadedAssemblies { get; }

    /// <summary>
    /// Gets a type by its fully qualified name from the plugin.
    /// </summary>
    /// <param name="typeName">The fully qualified type name.</param>
    /// <returns>The type if found; otherwise, null.</returns>
    Type? GetType(string typeName);

    /// <summary>
    /// Gets a type by its alias as defined in the manifest.
    /// </summary>
    /// <param name="alias">The alias of the type.</param>
    /// <returns>The type if found; otherwise, null.</returns>
    Type? GetTypeByAlias(string alias);

    /// <summary>
    /// Gets all implementations of the specified interface type.
    /// </summary>
    /// <param name="interfaceType">The interface type to find implementations for.</param>
    /// <returns>All types implementing the interface.</returns>
    IEnumerable<Type> GetImplementations(Type interfaceType);

    /// <summary>
    /// Gets all implementations of the specified interface type.
    /// </summary>
    /// <typeparam name="TInterface">The interface type to find implementations for.</typeparam>
    /// <returns>All types implementing the interface.</returns>
    IEnumerable<Type> GetImplementations<TInterface>();

    /// <summary>
    /// Creates an instance of the specified type using the provided service provider.
    /// </summary>
    /// <param name="type">The type to instantiate.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <returns>The created instance.</returns>
    object CreateInstance(Type type, IServiceProvider serviceProvider);

    /// <summary>
    /// Creates an instance of the type identified by the alias.
    /// </summary>
    /// <param name="alias">The alias of the type.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <returns>The created instance.</returns>
    object? CreateInstanceByAlias(string alias, IServiceProvider serviceProvider);
}
