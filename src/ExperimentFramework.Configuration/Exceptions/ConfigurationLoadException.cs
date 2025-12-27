namespace ExperimentFramework.Configuration.Exceptions;

/// <summary>
/// Exception thrown when configuration files cannot be loaded or parsed.
/// </summary>
public class ConfigurationLoadException : ExperimentConfigurationException
{
    /// <summary>
    /// The file path that could not be loaded.
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Creates a new instance with the specified message.
    /// </summary>
    public ConfigurationLoadException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new instance with the specified file path and message.
    /// </summary>
    public ConfigurationLoadException(string filePath, string message)
        : base($"Failed to load configuration from '{filePath}': {message}")
    {
        FilePath = filePath;
    }

    /// <summary>
    /// Creates a new instance with the specified file path, message, and inner exception.
    /// </summary>
    public ConfigurationLoadException(string filePath, string message, Exception innerException)
        : base($"Failed to load configuration from '{filePath}': {message}", innerException)
    {
        FilePath = filePath;
    }
}
