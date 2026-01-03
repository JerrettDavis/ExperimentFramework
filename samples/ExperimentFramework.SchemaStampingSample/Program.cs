using ExperimentFramework.Configuration.Schema;

namespace ExperimentFramework.SchemaStampingSample;

/// <summary>
/// Demonstrates configuration schema stamping, hashing, and versioning capabilities.
/// </summary>
public class SchemaStampingDemo
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== ExperimentFramework Schema Stamping Demo ===\n");

        // Demo 1: Basic Schema Hashing
        DemoBasicHashing();

        // Demo 2: Schema Normalization
        DemoNormalization();

        // Demo 3: Version Tracking
        DemoVersionTracking();

        // Demo 4: Unified Schema
        DemoUnifiedSchema();

        Console.WriteLine("\n=== Demo Complete ===");
    }

    private static void DemoBasicHashing()
    {
        Console.WriteLine("1. Basic Schema Hashing");
        Console.WriteLine("   Demonstrates deterministic hash computation\n");

        var content1 = "TYPE:MyConfig\nPROP:Name\nTYPE:string";
        var content2 = "TYPE:MyConfig\nPROP:Name\nTYPE:string";
        var content3 = "TYPE:DifferentConfig\nPROP:Name\nTYPE:string";

        var hash1 = SchemaHasher.ComputeHash(content1);
        var hash2 = SchemaHasher.ComputeHash(content2);
        var hash3 = SchemaHasher.ComputeHash(content3);

        Console.WriteLine($"   Hash 1: {hash1}");
        Console.WriteLine($"   Hash 2: {hash2}");
        Console.WriteLine($"   Hashes match: {hash1 == hash2}");
        Console.WriteLine($"   Hash 3: {hash3}");
        Console.WriteLine($"   Hash 3 different: {hash1 != hash3}");
        Console.WriteLine();
    }

    private static void DemoNormalization()
    {
        Console.WriteLine("2. Schema Normalization");
        Console.WriteLine("   Shows how properties are sorted for deterministic comparison\n");

        var schema = new SchemaDefinition
        {
            Types =
            [
                new SchemaTypeInfo
                {
                    TypeName = "ExperimentConfig",
                    Namespace = "ExperimentFramework.Configuration.Models",
                    Properties =
                    [
                        new SchemaPropertyInfo { Name = "Trials", TypeName = "List<TrialConfig>", IsRequired = false },
                        new SchemaPropertyInfo { Name = "Settings", TypeName = "FrameworkSettings", IsRequired = false },
                        new SchemaPropertyInfo { Name = "Name", TypeName = "string", IsRequired = true }
                    ]
                }
            ]
        };

        var normalized = SchemaHasher.NormalizeSchema(schema);
        Console.WriteLine("   Normalized schema (properties sorted alphabetically):");
        Console.WriteLine(normalized.Replace("\n", "\n   "));
        Console.WriteLine();
    }

    private static void DemoVersionTracking()
    {
        Console.WriteLine("3. Version Tracking");
        Console.WriteLine("   Demonstrates automatic version incrementation\n");

        var historyFile = Path.Combine(Path.GetTempPath(), $"demo-schema-history-{Guid.NewGuid()}.json");
        try
        {
            var tracker = new SchemaVersionTracker(historyFile);

            // First schema version
            var v1 = tracker.GetVersionForHash("Configuration", "hash_abc123");
            Console.WriteLine($"   First schema: version {v1}");

            // Same hash = same version
            var v2 = tracker.GetVersionForHash("Configuration", "hash_abc123");
            Console.WriteLine($"   Same schema: version {v2} (no change)");

            // Different hash = incremented version
            var v3 = tracker.GetVersionForHash("Configuration", "hash_def456");
            Console.WriteLine($"   Changed schema: version {v3} (incremented)");

            // Another change
            var v4 = tracker.GetVersionForHash("Configuration", "hash_ghi789");
            Console.WriteLine($"   Changed again: version {v4} (incremented)");

            // Different extension - independent versioning
            var v5 = tracker.GetVersionForHash("Governance", "hash_xyz999");
            Console.WriteLine($"   Different extension: version {v5} (starts at 1.0.0)");

            // Save history
            tracker.SaveHistory();
            Console.WriteLine($"\n   History saved to: {historyFile}");

            // Show history
            var history = tracker.GetHistory();
            Console.WriteLine($"\n   Version History:");
            foreach (var (extName, extHistory) in history.Extensions)
            {
                Console.WriteLine($"     {extName}: v{extHistory.CurrentVersion} (hash: {extHistory.CurrentHash})");
                foreach (var entry in extHistory.VersionHistory)
                {
                    Console.WriteLine($"       - v{entry.Version} @ {entry.Timestamp:yyyy-MM-dd HH:mm}");
                }
            }
        }
        finally
        {
            if (File.Exists(historyFile))
            {
                File.Delete(historyFile);
            }
        }
        Console.WriteLine();
    }

    private static void DemoUnifiedSchema()
    {
        Console.WriteLine("4. Unified Schema");
        Console.WriteLine("   Shows combining multiple extension schemas\n");

        // Create schemas for different extensions
        var configurationSchema = new SchemaDefinition
        {
            Metadata = new SchemaMetadata
            {
                ExtensionName = "Configuration",
                SchemaHash = "abc123def456",
                SchemaVersion = "1.2.0",
                Namespace = "ExperimentFramework.Configuration.Models"
            }
        };

        var governanceSchema = new SchemaDefinition
        {
            Metadata = new SchemaMetadata
            {
                ExtensionName = "Governance",
                SchemaHash = "789ghi012jkl",
                SchemaVersion = "1.0.5",
                Namespace = "ExperimentFramework.Governance.Models"
            }
        };

        var dataPlaneSchema = new SchemaDefinition
        {
            Metadata = new SchemaMetadata
            {
                ExtensionName = "DataPlane",
                SchemaHash = "345mno678pqr",
                SchemaVersion = "2.1.0",
                Namespace = "ExperimentFramework.DataPlane.Models"
            }
        };

        // Create unified schema document
        var unifiedDoc = new UnifiedSchemaDocument
        {
            SchemaFormatVersion = "1.0.0",
            GeneratedAt = DateTimeOffset.UtcNow,
            Schemas = new Dictionary<string, SchemaDefinition>
            {
                ["Configuration"] = configurationSchema,
                ["Governance"] = governanceSchema,
                ["DataPlane"] = dataPlaneSchema
            }
        };

        // Compute unified hash
        var allHashes = unifiedDoc.Schemas.Values.Select(s => s.Metadata.SchemaHash);
        unifiedDoc.UnifiedHash = SchemaHasher.ComputeUnifiedHash(allHashes);

        Console.WriteLine("   Unified Schema Document:");
        Console.WriteLine($"     Format Version: {unifiedDoc.SchemaFormatVersion}");
        Console.WriteLine($"     Generated: {unifiedDoc.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"     Unified Hash: {unifiedDoc.UnifiedHash}");
        Console.WriteLine($"\n     Extension Schemas:");
        
        foreach (var (name, schema) in unifiedDoc.Schemas)
        {
            Console.WriteLine($"       {name}:");
            Console.WriteLine($"         Version: {schema.Metadata.SchemaVersion}");
            Console.WriteLine($"         Hash: {schema.Metadata.SchemaHash}");
            Console.WriteLine($"         Namespace: {schema.Metadata.Namespace}");
        }
        Console.WriteLine();
    }
}
