using ExperimentFramework.Plugins.Abstractions;

namespace ExperimentFramework.Plugins.Integration;

/// <summary>
/// Extension methods for integrating plugins with the ExperimentFrameworkBuilder.
/// </summary>
public static class PluginBuilderExtensions
{
    /// <summary>
    /// Creates a plugin type reference string for use in YAML configuration or builder API.
    /// </summary>
    /// <param name="pluginId">The plugin identifier.</param>
    /// <param name="typeIdentifier">The type alias or full type name within the plugin.</param>
    /// <returns>A plugin type reference string in the format "plugin:PluginId/TypeIdentifier".</returns>
    public static string PluginType(string pluginId, string typeIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        ArgumentException.ThrowIfNullOrWhiteSpace(typeIdentifier);

        return $"plugin:{pluginId}/{typeIdentifier}";
    }

    /// <summary>
    /// Parses a plugin type reference string.
    /// </summary>
    /// <param name="typeReference">The type reference string.</param>
    /// <param name="pluginId">The parsed plugin ID.</param>
    /// <param name="typeIdentifier">The parsed type identifier.</param>
    /// <returns>True if the reference was successfully parsed; otherwise, false.</returns>
    public static bool TryParsePluginTypeReference(
        string typeReference,
        out string pluginId,
        out string typeIdentifier)
    {
        pluginId = string.Empty;
        typeIdentifier = string.Empty;

        if (string.IsNullOrWhiteSpace(typeReference) ||
            !typeReference.StartsWith("plugin:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var reference = typeReference[7..]; // Remove "plugin:" prefix
        var separatorIndex = reference.IndexOf('/');

        if (separatorIndex <= 0 || separatorIndex >= reference.Length - 1)
        {
            return false;
        }

        pluginId = reference[..separatorIndex];
        typeIdentifier = reference[(separatorIndex + 1)..];

        return !string.IsNullOrWhiteSpace(pluginId) && !string.IsNullOrWhiteSpace(typeIdentifier);
    }

    /// <summary>
    /// Gets all types from plugins that implement the specified interface.
    /// </summary>
    /// <typeparam name="TInterface">The interface type.</typeparam>
    /// <param name="pluginManager">The plugin manager.</param>
    /// <returns>All implementing types from all loaded plugins.</returns>
    public static IEnumerable<Type> GetPluginImplementations<TInterface>(this IPluginManager pluginManager)
    {
        ArgumentNullException.ThrowIfNull(pluginManager);

        foreach (var plugin in pluginManager.GetLoadedPlugins())
        {
            foreach (var type in plugin.GetImplementations<TInterface>())
            {
                yield return type;
            }
        }
    }

    /// <summary>
    /// Gets all service registrations from loaded plugins for a specific interface.
    /// </summary>
    /// <param name="pluginManager">The plugin manager.</param>
    /// <param name="interfaceName">The interface name to filter by (simple or full name).</param>
    /// <returns>Plugin implementations matching the interface.</returns>
    public static IEnumerable<(IPluginContext Plugin, PluginImplementation Implementation)> GetPluginServicesForInterface(
        this IPluginManager pluginManager,
        string interfaceName)
    {
        ArgumentNullException.ThrowIfNull(pluginManager);
        ArgumentException.ThrowIfNullOrWhiteSpace(interfaceName);

        foreach (var plugin in pluginManager.GetLoadedPlugins())
        {
            foreach (var service in plugin.Manifest.Services)
            {
                if (service.Interface.Equals(interfaceName, StringComparison.OrdinalIgnoreCase) ||
                    service.Interface.EndsWith($".{interfaceName}", StringComparison.OrdinalIgnoreCase) ||
                    service.Interface.EndsWith($"+{interfaceName}", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var impl in service.Implementations)
                    {
                        yield return (plugin, impl);
                    }
                }
            }
        }
    }
}
