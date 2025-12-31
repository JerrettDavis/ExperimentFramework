using ExperimentFramework.Configuration.Schema;

namespace ExperimentFramework.Tools.SchemaGenerator;

/// <summary>
/// Command-line tool to generate ExperimentFramework schema JSON files.
/// </summary>
public class Program
{
    public static int Main(string[] args)
    {
        Console.WriteLine("ExperimentFramework Schema Generator");
        Console.WriteLine("=====================================\n");

        var outputDir = args.Length > 0 
            ? args[0] 
            : Path.Combine(Directory.GetCurrentDirectory(), "schemas");

        try
        {
            GenerateSchemas(outputDir);
            Console.WriteLine("\n✓ Schema generation completed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"\n✗ Schema generation failed: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    private static void GenerateSchemas(string outputDirectory)
    {
        // Create output directory
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        // Generate schema for Configuration extension
        var configSchema = CreateConfigurationSchema();
        SchemaExporter.ExportExtensionSchema(
            configSchema,
            Path.Combine(outputDirectory, "Configuration.schema.json"));

        // Generate schema for Governance.Persistence extension
        var governanceSchema = CreateGovernancePersistenceSchema();
        SchemaExporter.ExportExtensionSchema(
            governanceSchema,
            Path.Combine(outputDirectory, "Governance.Persistence.schema.json"));

        // Create unified schema
        var unified = CreateUnifiedSchema(configSchema, governanceSchema);
        SchemaExporter.ExportUnifiedSchema(
            unified,
            Path.Combine(outputDirectory, "ExperimentFramework.unified.schema.json"));

        Console.WriteLine($"Generated 3 schema files in {outputDirectory}");
    }

    private static SchemaDefinition CreateConfigurationSchema()
    {
        var assembly = typeof(ExperimentFramework.Configuration.Models.TrialConfig).Assembly;
        return SchemaExporter.CreateSchemaFromAssembly(
            assembly,
            "Configuration",
            "ExperimentFramework.Configuration.Models");
    }

    private static SchemaDefinition CreateGovernancePersistenceSchema()
    {
        var assembly = typeof(ExperimentFramework.Governance.Persistence.Models.PersistedExperimentState).Assembly;
        return SchemaExporter.CreateSchemaFromAssembly(
            assembly,
            "Governance.Persistence",
            "ExperimentFramework.Governance.Persistence.Models");
    }

    private static UnifiedSchemaDocument CreateUnifiedSchema(params SchemaDefinition[] schemas)
    {
        var unified = new UnifiedSchemaDocument
        {
            SchemaFormatVersion = "1.0.0",
            GeneratedAt = DateTimeOffset.UtcNow
        };

        foreach (var schema in schemas)
        {
            unified.Schemas[schema.Metadata.ExtensionName] = schema;
        }

        // Compute unified hash
        var hashes = unified.Schemas.Values.Select(s => s.Metadata.SchemaHash);
        unified.UnifiedHash = SchemaHasher.ComputeUnifiedHash(hashes);

        return unified;
    }
}
