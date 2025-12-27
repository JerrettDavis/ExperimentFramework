using ExperimentFramework.Configuration.Building;
using ExperimentFramework.Configuration.Exceptions;
using ExperimentFramework.Plugins.Abstractions;

namespace ExperimentFramework.Plugins.Integration;

/// <summary>
/// Type resolver decorator that adds support for plugin type references.
/// Handles "plugin:PluginId/alias" and "plugin:PluginId/Full.Type.Name" syntax.
/// </summary>
public sealed class PluginTypeResolver : ITypeResolver
{
    private readonly ITypeResolver _innerResolver;
    private readonly IPluginManager _pluginManager;
    private const string PluginPrefix = "plugin:";

    /// <summary>
    /// Creates a new plugin type resolver decorator.
    /// </summary>
    /// <param name="innerResolver">The inner type resolver to delegate to for non-plugin types.</param>
    /// <param name="pluginManager">The plugin manager for resolving plugin types.</param>
    public PluginTypeResolver(ITypeResolver innerResolver, IPluginManager pluginManager)
    {
        _innerResolver = innerResolver ?? throw new ArgumentNullException(nameof(innerResolver));
        _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
    }

    /// <inheritdoc />
    public Type Resolve(string typeName)
    {
        if (TryResolve(typeName, out var type) && type is not null)
        {
            return type;
        }

        throw new TypeResolutionException($"Type '{typeName}' could not be resolved.");
    }

    /// <inheritdoc />
    public bool TryResolve(string typeName, out Type? type)
    {
        type = null;

        if (string.IsNullOrWhiteSpace(typeName))
        {
            return false;
        }

        // Handle plugin references
        if (typeName.StartsWith(PluginPrefix, StringComparison.OrdinalIgnoreCase))
        {
            type = _pluginManager.ResolveType(typeName);
            return type is not null;
        }

        // Try inner resolver first
        if (_innerResolver.TryResolve(typeName, out type))
        {
            return true;
        }

        // Fall back to searching plugins for the type name
        type = SearchPluginsForType(typeName);
        return type is not null;
    }

    /// <inheritdoc />
    public void RegisterAlias(string alias, Type type)
    {
        // Delegate to inner resolver for alias registration
        _innerResolver.RegisterAlias(alias, type);
    }

    private Type? SearchPluginsForType(string typeName)
    {
        foreach (var plugin in _pluginManager.GetLoadedPlugins())
        {
            // Try as alias first
            var type = plugin.GetTypeByAlias(typeName);
            if (type is not null)
            {
                return type;
            }

            // Try as type name
            type = plugin.GetType(typeName);
            if (type is not null)
            {
                return type;
            }
        }

        return null;
    }
}
