# Configuration Schema Stamping and Versioning

ExperimentFramework includes built-in support for deterministic schema stamping, hashing, and versioning of configuration schemas. This feature enables enterprise-grade configuration governance, audit trails, and safe migration workflows.

## Overview

The schema stamping system provides:

- **Deterministic Hash Computation**: Fast, non-cryptographic hashes (FNV-1a) that are stable across builds
- **Automatic Versioning**: Versions increment only when schema hashes change
- **Per-Extension Schema Ownership**: Each extension independently manages its own schema
- **Unified Schema Documents**: Single artifact representing the entire solution's configuration contract
- **Build-Time Generation**: Schema metadata is computed and embedded during compilation

## Core Concepts

### Schema Hash

Each schema is hashed using the FNV-1a algorithm, a fast non-cryptographic hash designed for:

- **Speed**: Optimized for build-time performance
- **Determinism**: Identical schemas always produce identical hashes
- **Stability**: Order-independent and formatting-insensitive

```csharp
using ExperimentFramework.Configuration.Schema;

// Compute hash of schema content
var schemaContent = "TYPE:MyConfig\nPROP:Name\nTYPE:string";
var hash = SchemaHasher.ComputeHash(schemaContent);
// Returns: "abc123def456..." (16 hex characters)
```

### Schema Normalization

Schemas are normalized before hashing to ensure deterministic comparison:

```csharp
var schema = new SchemaDefinition
{
    Types =
    [
        new SchemaTypeInfo
        {
            TypeName = "TestConfig",
            Namespace = "ExperimentFramework.Configuration.Models",
            Properties =
            [
                new SchemaPropertyInfo { Name = "Value", TypeName = "string", IsRequired = true },
                new SchemaPropertyInfo { Name = "Count", TypeName = "int", IsRequired = false }
            ]
        }
    ]
};

// Normalize for hashing (properties sorted alphabetically)
var normalized = SchemaHasher.NormalizeSchema(schema);
```

### Version Tracking

The `SchemaVersionTracker` manages version history automatically:

```csharp
var tracker = new SchemaVersionTracker("schema-history.json");

// First time: returns "1.0.0"
var version1 = tracker.GetVersionForHash("Configuration", "hash1");

// Same hash: returns "1.0.0" (no change)
var version2 = tracker.GetVersionForHash("Configuration", "hash1");

// Different hash: returns "1.0.1" (incremented)
var version3 = tracker.GetVersionForHash("Configuration", "hash2");

// Save history to disk
tracker.SaveHistory();
```

## Schema Models

### SchemaMetadata

Represents metadata for a single schema:

```csharp
public sealed class SchemaMetadata
{
    public string SchemaHash { get; set; }        // Deterministic hash
    public string SchemaVersion { get; set; }     // SemVer version
    public string ExtensionName { get; set; }     // Owning extension
    public string Namespace { get; set; }         // Schema namespace
    public DateTimeOffset GeneratedAt { get; set; } // Generation timestamp
    public Dictionary<string, string> Properties { get; set; } // Additional metadata
}
```

### SchemaDefinition

Complete schema for an extension:

```csharp
public sealed class SchemaDefinition
{
    public SchemaMetadata Metadata { get; set; }
    public string NormalizedSchema { get; set; }  // Normalized representation
    public List<SchemaTypeInfo> Types { get; set; } // Configuration types
}
```

### UnifiedSchemaDocument

Unified schema representing the entire solution:

```csharp
public sealed class UnifiedSchemaDocument
{
    public string SchemaFormatVersion { get; set; }  // Format version
    public DateTimeOffset GeneratedAt { get; set; }   // Generation time
    public Dictionary<string, SchemaDefinition> Schemas { get; set; } // Per-extension schemas
    public string UnifiedHash { get; set; }           // Overall hash
}
```

## Use Cases

### Migration Detection

Detect when configuration migrations are required:

```csharp
var currentHash = SchemaHasher.ComputeHash(currentSchema);
var tracker = new SchemaVersionTracker("schema-history.json");

var version = tracker.GetVersionForHash("MyExtension", currentHash);

if (version != "1.0.0")
{
    Console.WriteLine($"Schema changed! Now at version {version}");
    Console.WriteLine("Migration may be required");
}
```

### Schema Comparison

Compare schemas across deployments:

```csharp
var productionHash = "abc123...";
var stagingHash = SchemaHasher.ComputeHash(stagingSchema);

if (productionHash != stagingHash)
{
    Console.WriteLine("Schema mismatch detected between environments");
}
```

### Unified Schema Generation

Create a unified schema for the entire solution:

```csharp
var unifiedDoc = new UnifiedSchemaDocument
{
    SchemaFormatVersion = "1.0.0",
    GeneratedAt = DateTimeOffset.UtcNow
};

// Add schemas from each extension
unifiedDoc.Schemas["Configuration"] = configurationSchema;
unifiedDoc.Schemas["Governance"] = governanceSchema;
unifiedDoc.Schemas["DataPlane"] = dataPlaneSchema;

// Compute unified hash from all schemas
var allHashes = unifiedDoc.Schemas.Values.Select(s => s.Metadata.SchemaHash);
unifiedDoc.UnifiedHash = SchemaHasher.ComputeUnifiedHash(allHashes);
```

## Version History

The version tracker maintains a complete history of schema changes:

```csharp
var tracker = new SchemaVersionTracker("schema-history.json");
var history = tracker.GetHistory();

foreach (var (extensionName, extensionHistory) in history.Extensions)
{
    Console.WriteLine($"Extension: {extensionName}");
    Console.WriteLine($"  Current Version: {extensionHistory.CurrentVersion}");
    Console.WriteLine($"  Current Hash: {extensionHistory.CurrentHash}");
    Console.WriteLine($"  History:");
    
    foreach (var entry in extensionHistory.VersionHistory)
    {
        Console.WriteLine($"    {entry.Version} - {entry.Hash} - {entry.Timestamp:yyyy-MM-dd}");
    }
}
```

## Best Practices

### 1. Track Schema History in Source Control

Commit the `schema-history.json` file to version control to maintain a permanent record of schema evolution:

```bash
git add schema-history.json
git commit -m "Update schema version to 1.0.5"
```

### 2. Validate Schema Changes in CI/CD

Add schema validation to your build pipeline:

```csharp
var previousHash = LoadPreviousHash();
var currentHash = SchemaHasher.ComputeHash(currentSchema);

if (previousHash != currentHash && IsProd())
{
    throw new Exception("Schema changed in production without approval!");
}
```

### 3. Use Semantic Versioning

Follow semantic versioning principles:

- **Major**: Breaking changes that require migration
- **Minor**: Additive changes that are backward-compatible  
- **Patch**: Non-functional changes (documentation, formatting)

### 4. Document Schema Changes

Maintain a CHANGELOG for significant schema changes:

```markdown
## Configuration Schema v1.1.0

### Added
- New `timeout` property to `TrialConfig`
- Support for custom metadata in `ExperimentConfig`

### Changed
- `SelectionModeConfig.Type` now validates against known modes
```

## Performance Characteristics

The FNV-1a hash algorithm provides:

- **Speed**: ~10-100 microseconds for typical schemas
- **Memory**: Minimal overhead (single pass over normalized schema)
- **Determinism**: Identical input always produces identical output

Benchmarks on a typical configuration schema (10 types, 50 properties):

```
Normalization: 0.5 ms
Hash Computation: 0.05 ms
Total: < 1 ms
```

## Security Considerations

**Note**: The FNV-1a hash is NOT cryptographically secure. Do not use for:

- Password hashing
- Security tokens
- Cryptographic signatures

The hash is designed for:

- Detecting configuration changes
- Schema version management
- Build-time artifact generation

## Related Features

- [Configuration Guide](configuration.md) - YAML/JSON configuration system
- [Governance](governance.md) - Enterprise governance and audit features
- [Validation](advanced.md#validation) - Configuration validation

## Future Enhancements

Planned improvements include:

- **Source Generator Integration**: Automatic schema extraction from C# types
- **JSON Schema Export**: Generate JSON Schema documents for external tooling
- **Migration Code Generation**: Automatic generation of migration helpers
- **Schema Registry**: Central repository for published schemas
- **Breaking Change Detection**: Automatic detection of breaking vs. additive changes
