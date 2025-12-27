namespace ExperimentFramework.Configuration.Building;

/// <summary>
/// Resolves type names from configuration to actual Type objects.
/// </summary>
public interface ITypeResolver
{
    /// <summary>
    /// Resolves a type name to a Type object.
    /// </summary>
    /// <param name="typeName">The type name (simple, full, or assembly-qualified).</param>
    /// <returns>The resolved Type.</returns>
    /// <exception cref="Exceptions.TypeResolutionException">When the type cannot be found.</exception>
    Type Resolve(string typeName);

    /// <summary>
    /// Attempts to resolve a type name to a Type object.
    /// </summary>
    /// <param name="typeName">The type name.</param>
    /// <param name="type">The resolved type, or null if not found.</param>
    /// <returns>True if the type was resolved; otherwise false.</returns>
    bool TryResolve(string typeName, out Type? type);

    /// <summary>
    /// Registers a type alias for simplified configuration.
    /// </summary>
    /// <param name="alias">The alias name to use in configuration.</param>
    /// <param name="type">The actual type the alias maps to.</param>
    void RegisterAlias(string alias, Type type);
}
