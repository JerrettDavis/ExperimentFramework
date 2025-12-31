namespace ExperimentFramework.Configuration.Schema;

/// <summary>
/// Represents the unified schema document for the entire solution,
/// containing schemas from all extensions.
/// </summary>
public sealed class UnifiedSchemaDocument
{
    /// <summary>
    /// Gets or sets the version of the unified schema format.
    /// </summary>
    public string SchemaFormatVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the timestamp when this unified schema was generated.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the individual schemas from each extension, keyed by extension name.
    /// </summary>
    public Dictionary<string, SchemaDefinition> Schemas { get; set; } = new();

    /// <summary>
    /// Gets or sets the overall hash of the unified schema.
    /// This is computed from all individual schema hashes.
    /// </summary>
    public string UnifiedHash { get; set; } = string.Empty;
}
