using System.Collections.Concurrent;
using System.Reflection;
using ExperimentFramework.Configuration.Exceptions;

namespace ExperimentFramework.Configuration.Building;

/// <summary>
/// Default type resolver with multi-strategy resolution.
/// </summary>
public sealed class TypeResolver : ITypeResolver
{
    private readonly List<Assembly> _searchAssemblies = [];
    private readonly Dictionary<string, Type> _aliases = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Type?> _cache = new();

    /// <summary>
    /// Creates a new type resolver with default assembly search paths.
    /// </summary>
    public TypeResolver()
        : this(null, null)
    {
    }

    /// <summary>
    /// Creates a new type resolver with custom assembly search paths and type aliases.
    /// </summary>
    /// <param name="assemblySearchPaths">Additional assembly paths to search.</param>
    /// <param name="typeAliases">Pre-registered type aliases.</param>
    public TypeResolver(
        IEnumerable<string>? assemblySearchPaths,
        IDictionary<string, Type>? typeAliases)
    {
        // Load assemblies from custom paths
        if (assemblySearchPaths != null)
        {
            foreach (var path in assemblySearchPaths)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        _searchAssemblies.Add(Assembly.LoadFrom(path));
                    }
                }
                catch
                {
                    // Ignore assembly load failures for optional paths
                }
            }
        }

        // Add entry assembly and all loaded assemblies
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            _searchAssemblies.Add(entryAssembly);
        }

        // Add all loaded non-dynamic assemblies
        _searchAssemblies.AddRange(
            AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a is { IsDynamic: false, ReflectionOnly: false }));

        // Register provided aliases
        if (typeAliases != null)
        {
            foreach (var (alias, type) in typeAliases)
            {
                _aliases[alias] = type;
            }
        }
    }

    /// <inheritdoc />
    public Type Resolve(string typeName)
    {
        if (TryResolve(typeName, out var type) && type != null)
        {
            return type;
        }

        throw new TypeResolutionException(typeName);
    }

    /// <inheritdoc />
    public bool TryResolve(string typeName, out Type? type)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            type = null;
            return false;
        }

        // Check cache first
        if (_cache.TryGetValue(typeName, out type))
        {
            return type != null;
        }

        type = ResolveInternal(typeName);
        _cache[typeName] = type;
        return type != null;
    }

    /// <inheritdoc />
    public void RegisterAlias(string alias, Type type)
    {
        _aliases[alias] = type;
        _cache.TryRemove(alias, out _); // Invalidate cache
    }

    private Type? ResolveInternal(string typeName)
    {
        // Strategy 1: Check aliases
        if (_aliases.TryGetValue(typeName, out var aliasedType))
        {
            return aliasedType;
        }

        // Strategy 2: Try assembly-qualified name (e.g., "MyApp.MyType, MyApp")
        if (typeName.Contains(','))
        {
            var resolved = Type.GetType(typeName, throwOnError: false);
            if (resolved != null)
            {
                return resolved;
            }
        }

        // Strategy 3: Try as full type name in loaded assemblies
        foreach (var assembly in _searchAssemblies)
        {
            try
            {
                var resolved = assembly.GetType(typeName, throwOnError: false);
                if (resolved != null)
                {
                    return resolved;
                }
            }
            catch
            {
                // Ignore errors from searching individual assemblies
            }
        }

        // Strategy 4: Search by simple name (last segment after dots)
        var simpleName = typeName.Contains('.')
            ? typeName.Split('.').Last()
            : typeName;

        var candidates = new List<Type>();
        foreach (var assembly in _searchAssemblies)
        {
            try
            {
                var types = assembly.GetExportedTypes()
                    .Where(t => t.Name.Equals(simpleName, StringComparison.Ordinal))
                    .ToList();
                candidates.AddRange(types);
            }
            catch
            {
                // Ignore errors from searching individual assemblies
            }
        }

        // If we have exactly one match, use it
        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        // If we have multiple matches and typeName contains namespace hints, try to narrow down
        if (candidates.Count > 1 && typeName.Contains('.'))
        {
            // Try to find one that matches the full namespace
            var exactMatch = candidates.FirstOrDefault(t =>
                t.FullName?.Equals(typeName, StringComparison.OrdinalIgnoreCase) == true);
            if (exactMatch != null)
            {
                return exactMatch;
            }

            // Try to find one that ends with the given name
            var endsWith = candidates.FirstOrDefault(t =>
                t.FullName?.EndsWith(typeName, StringComparison.OrdinalIgnoreCase) == true);
            if (endsWith != null)
            {
                return endsWith;
            }
        }

        // Strategy 5: For interface-style names (IMyService), try without the I prefix
        if (simpleName.StartsWith('I') && simpleName.Length > 1 && char.IsUpper(simpleName[1]))
        {
            var implementationName = simpleName[1..];
            foreach (var assembly in _searchAssemblies)
            {
                try
                {
                    var implementation = assembly.GetExportedTypes()
                        .FirstOrDefault(t => t.Name.Equals(implementationName, StringComparison.Ordinal));
                    if (implementation != null)
                    {
                        return implementation;
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }
        }

        return null;
    }
}
