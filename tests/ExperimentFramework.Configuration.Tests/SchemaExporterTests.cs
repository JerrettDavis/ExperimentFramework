using System.Text.Json;
using ExperimentFramework.Configuration.Schema;

namespace ExperimentFramework.Configuration.Tests;

/// <summary>
/// Unit tests for SchemaExporter.
/// </summary>
public sealed class SchemaExporterTests : IDisposable
{
    private readonly string _tempDir;

    public SchemaExporterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SchemaExporterTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void ExportUnifiedSchema_WritesValidJsonFile()
    {
        var schema = new UnifiedSchemaDocument
        {
            SchemaFormatVersion = "1.0.0",
            GeneratedAt = DateTimeOffset.UtcNow,
            Schemas = new Dictionary<string, SchemaDefinition>
            {
                ["test"] = new SchemaDefinition
                {
                    NormalizedSchema = "{}",
                    Metadata = new SchemaMetadata { ExtensionName = "test" }
                }
            },
            UnifiedHash = "abc123"
        };

        var outputPath = Path.Combine(_tempDir, "unified.json");
        SchemaExporter.ExportUnifiedSchema(schema, outputPath);

        Assert.True(File.Exists(outputPath));
        var content = File.ReadAllText(outputPath);
        Assert.Contains("schemaFormatVersion", content);
        Assert.Contains("1.0.0", content);
    }

    [Fact]
    public void ExportUnifiedSchema_CreatesDirectoryIfNotExists()
    {
        var schema = new UnifiedSchemaDocument { UnifiedHash = "hash" };
        var subDir = Path.Combine(_tempDir, "subdir", "deep");
        var outputPath = Path.Combine(subDir, "schema.json");

        SchemaExporter.ExportUnifiedSchema(schema, outputPath);

        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public void ExportExtensionSchema_WritesValidJsonFile()
    {
        var schemaDef = new SchemaDefinition
        {
            NormalizedSchema = "{\"test\": true}",
            Metadata = new SchemaMetadata
            {
                ExtensionName = "MyExtension",
                Namespace = "MyNamespace"
            },
            Types = new List<SchemaTypeInfo>
            {
                new SchemaTypeInfo
                {
                    TypeName = "MyConfig",
                    Namespace = "MyNamespace",
                    Properties = new List<SchemaPropertyInfo>
                    {
                        new SchemaPropertyInfo { Name = "Setting", TypeName = "String", IsRequired = true }
                    }
                }
            }
        };

        var outputPath = Path.Combine(_tempDir, "extension.json");
        SchemaExporter.ExportExtensionSchema(schemaDef, outputPath);

        Assert.True(File.Exists(outputPath));
        var content = File.ReadAllText(outputPath);
        Assert.Contains("MyExtension", content);
    }

    [Fact]
    public void ExportExtensionSchema_CreatesDirectoryIfNotExists()
    {
        var schemaDef = new SchemaDefinition { NormalizedSchema = "{}" };
        var subDir = Path.Combine(_tempDir, "ext", "nested");
        var outputPath = Path.Combine(subDir, "ext.json");

        SchemaExporter.ExportExtensionSchema(schemaDef, outputPath);

        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public void ExportUnifiedSchema_OutputIsFormattedJson()
    {
        var schema = new UnifiedSchemaDocument { UnifiedHash = "test-hash" };
        var outputPath = Path.Combine(_tempDir, "pretty.json");

        SchemaExporter.ExportUnifiedSchema(schema, outputPath);

        var content = File.ReadAllText(outputPath);
        // Indented JSON has newlines
        Assert.Contains(Environment.NewLine, content);
    }

    [Fact]
    public void ExportUnifiedSchema_OutputIsDeserializable()
    {
        var schema = new UnifiedSchemaDocument
        {
            SchemaFormatVersion = "2.0.0",
            UnifiedHash = "xyz"
        };
        var outputPath = Path.Combine(_tempDir, "deser.json");

        SchemaExporter.ExportUnifiedSchema(schema, outputPath);

        var content = File.ReadAllText(outputPath);
        var deserialized = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.True(deserialized.TryGetProperty("schemaFormatVersion", out var versionElement));
        Assert.Equal("2.0.0", versionElement.GetString());
    }

    [Fact]
    public void CreateSchemaFromAssembly_ReturnsSchemaDefinition()
    {
        var assembly = typeof(SchemaExporter).Assembly;

        var schema = SchemaExporter.CreateSchemaFromAssembly(
            assembly,
            "TestExtension",
            "ExperimentFramework.Configuration.Schema");

        Assert.NotNull(schema);
        Assert.Equal("TestExtension", schema.Metadata.ExtensionName);
        Assert.Equal("ExperimentFramework.Configuration.Schema", schema.Metadata.Namespace);
        Assert.NotEmpty(schema.Metadata.SchemaHash);
    }

    [Fact]
    public void CreateSchemaFromAssembly_FiltersToNamespace()
    {
        var assembly = typeof(SchemaExporter).Assembly;
        const string ns = "ExperimentFramework.Configuration.Schema";

        var schema = SchemaExporter.CreateSchemaFromAssembly(assembly, "Test", ns);

        // All types should be from the specified namespace
        foreach (var typeInfo in schema.Types)
        {
            Assert.Equal(ns, typeInfo.Namespace);
        }
    }

    [Fact]
    public void ExportExtensionSchema_OutputIsDeserializable()
    {
        var schemaDef = new SchemaDefinition
        {
            NormalizedSchema = "{}",
            Metadata = new SchemaMetadata { ExtensionName = "ext" }
        };
        var outputPath = Path.Combine(_tempDir, "ext-deser.json");

        SchemaExporter.ExportExtensionSchema(schemaDef, outputPath);

        var content = File.ReadAllText(outputPath);
        var deserialized = JsonSerializer.Deserialize<JsonElement>(content);
        Assert.True(deserialized.TryGetProperty("normalizedSchema", out _));
    }
}
