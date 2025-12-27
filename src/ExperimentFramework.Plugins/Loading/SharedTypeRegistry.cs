using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;

namespace ExperimentFramework.Plugins.Loading;

/// <summary>
/// Registry for types that should be shared across plugin contexts.
/// Ensures that shared interfaces (like ExperimentFramework types) are loaded
/// from the host context to enable proper type compatibility.
/// </summary>
public sealed class SharedTypeRegistry
{
    private readonly ConcurrentDictionary<string, Assembly> _sharedAssemblies = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _sharedAssemblyNames;

    /// <summary>
    /// Default assemblies that are always shared for ExperimentFramework compatibility.
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultSharedAssemblies =
    [
        "ExperimentFramework",
        "ExperimentFramework.Abstractions",
        "ExperimentFramework.Configuration",
        "ExperimentFramework.Plugins",
        "Microsoft.Extensions.DependencyInjection.Abstractions",
        "Microsoft.Extensions.DependencyInjection",
        "Microsoft.Extensions.Logging.Abstractions",
        "Microsoft.Extensions.Options",
        "Microsoft.Extensions.Configuration.Abstractions",
        "Microsoft.Extensions.Primitives",
        "System.Runtime",
        "System.Private.CoreLib",
        "netstandard"
    ];

    /// <summary>
    /// Creates a new shared type registry with the specified shared assemblies.
    /// </summary>
    /// <param name="additionalSharedAssemblies">Additional assemblies to share beyond the defaults.</param>
    public SharedTypeRegistry(IEnumerable<string>? additionalSharedAssemblies = null)
    {
        _sharedAssemblyNames = [..DefaultSharedAssemblies];

        if (additionalSharedAssemblies is not null)
        {
            foreach (var name in additionalSharedAssemblies)
            {
                _sharedAssemblyNames.Add(name);
            }
        }

        // Pre-load commonly used assemblies from the default context
        foreach (var assembly in AssemblyLoadContext.Default.Assemblies)
        {
            var name = assembly.GetName().Name;
            if (name is not null && _sharedAssemblyNames.Contains(name))
            {
                _sharedAssemblies.TryAdd(name, assembly);
            }
        }
    }

    /// <summary>
    /// Checks if an assembly should be shared with plugins.
    /// </summary>
    /// <param name="assemblyName">The assembly name to check.</param>
    /// <returns>True if the assembly should be shared.</returns>
    public bool IsShared(string assemblyName)
    {
        return _sharedAssemblyNames.Contains(assemblyName);
    }

    /// <summary>
    /// Checks if an assembly should be shared with plugins.
    /// </summary>
    /// <param name="assemblyName">The assembly name to check.</param>
    /// <returns>True if the assembly should be shared.</returns>
    public bool IsShared(AssemblyName assemblyName)
    {
        return assemblyName.Name is not null && IsShared(assemblyName.Name);
    }

    /// <summary>
    /// Tries to get a shared assembly.
    /// </summary>
    /// <param name="assemblyName">The assembly name.</param>
    /// <param name="assembly">The shared assembly if found.</param>
    /// <returns>True if the assembly was found.</returns>
    public bool TryGetSharedAssembly(AssemblyName assemblyName, out Assembly? assembly)
    {
        assembly = null;

        if (assemblyName.Name is null || !IsShared(assemblyName.Name))
        {
            return false;
        }

        if (_sharedAssemblies.TryGetValue(assemblyName.Name, out assembly))
        {
            return true;
        }

        // Try to load from default context
        try
        {
            assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
            if (assembly is not null && assemblyName.Name is not null)
            {
                _sharedAssemblies.TryAdd(assemblyName.Name, assembly);
                return true;
            }
        }
        catch (Exception)
        {
            // Assembly not available in default context
        }

        return false;
    }

    /// <summary>
    /// Adds an assembly to the shared registry.
    /// </summary>
    /// <param name="name">The assembly name.</param>
    /// <param name="assembly">The assembly.</param>
    public void AddSharedAssembly(string name, Assembly assembly)
    {
        _sharedAssemblyNames.Add(name);
        _sharedAssemblies.TryAdd(name, assembly);
    }

    /// <summary>
    /// Gets all shared assembly names.
    /// </summary>
    public IReadOnlySet<string> SharedAssemblyNames => _sharedAssemblyNames;
}
