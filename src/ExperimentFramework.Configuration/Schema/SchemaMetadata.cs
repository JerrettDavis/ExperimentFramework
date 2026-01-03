namespace ExperimentFramework.Configuration.Schema;

/// <summary>
/// Represents metadata for a configuration schema, including deterministic hash and version information.
/// </summary>
public sealed class SchemaMetadata
{
    /// <summary>
    /// Gets or sets the deterministic hash of the schema structure.
    /// This hash is computed using a fast, non-cryptographic algorithm (FNV-1a)
    /// and is stable across builds for identical schemas.
    /// </summary>
    public string SchemaHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the semantic version of the schema.
    /// The version is incremented automatically when the schema hash changes.
    /// </summary>
    public string SchemaVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the name of the extension or module that owns this schema.
    /// </summary>
    public string ExtensionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace of the schema types.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when this schema metadata was generated.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets additional metadata properties for extensibility.
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new();
}
