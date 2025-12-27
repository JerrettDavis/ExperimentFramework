namespace ExperimentFramework.Configuration.Exceptions;

/// <summary>
/// Exception thrown when a type cannot be resolved from a configuration type name.
/// </summary>
public class TypeResolutionException : ExperimentConfigurationException
{
    /// <summary>
    /// The type name that could not be resolved.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// The configuration path where the type was referenced.
    /// </summary>
    public string? ConfigurationPath { get; }

    /// <summary>
    /// Creates a new instance with the specified type name.
    /// </summary>
    public TypeResolutionException(string typeName)
        : base($"Could not resolve type '{typeName}'. Ensure the type exists and its assembly is loaded.")
    {
        TypeName = typeName;
    }

    /// <summary>
    /// Creates a new instance with the specified type name and message.
    /// </summary>
    public TypeResolutionException(string typeName, string message)
        : base($"Failed to resolve type '{typeName}': {message}")
    {
        TypeName = typeName;
    }

    /// <summary>
    /// Creates a new instance with the specified type name, configuration path, and message.
    /// </summary>
    public TypeResolutionException(string typeName, string configurationPath, string message)
        : base($"Failed to resolve type '{typeName}' at '{configurationPath}': {message}")
    {
        TypeName = typeName;
        ConfigurationPath = configurationPath;
    }

    /// <summary>
    /// Creates a new instance with the specified type name and inner exception.
    /// </summary>
    public TypeResolutionException(string typeName, Exception innerException)
        : base($"Failed to resolve type '{typeName}': {innerException.Message}", innerException)
    {
        TypeName = typeName;
    }
}
