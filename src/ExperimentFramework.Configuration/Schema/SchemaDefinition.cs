namespace ExperimentFramework.Configuration.Schema;

/// <summary>
/// Represents a complete schema definition for a configuration extension.
/// </summary>
public sealed class SchemaDefinition
{
    /// <summary>
    /// Gets or sets the metadata for this schema.
    /// </summary>
    public SchemaMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the schema properties (fields/types) as a normalized JSON structure.
    /// </summary>
    public string NormalizedSchema { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of configuration model types included in this schema.
    /// </summary>
    public List<SchemaTypeInfo> Types { get; set; } = new();
}

/// <summary>
/// Represents information about a type in the schema.
/// </summary>
public sealed class SchemaTypeInfo
{
    /// <summary>
    /// Gets or sets the full name of the type.
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the namespace of the type.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the properties of this type.
    /// </summary>
    public List<SchemaPropertyInfo> Properties { get; set; } = new();
}

/// <summary>
/// Represents information about a property in a schema type.
/// </summary>
public sealed class SchemaPropertyInfo
{
    /// <summary>
    /// Gets or sets the property name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the property type name.
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the property is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets whether the property is nullable.
    /// </summary>
    public bool IsNullable { get; set; }
}
