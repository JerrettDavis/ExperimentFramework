using System.Text.Json;
using ExperimentFramework.Configuration.Schema;

namespace ExperimentFramework.Configuration.Schema;

/// <summary>
/// Exports schema definitions to JSON files for build artifacts and releases.
/// </summary>
public static class SchemaExporter
{
    /// <summary>
    /// Exports a unified schema document to a JSON file.
    /// </summary>
    /// <param name="unifiedSchema">The unified schema document to export.</param>
    /// <param name="outputPath">The path where the JSON file will be written.</param>
    public static void ExportUnifiedSchema(UnifiedSchemaDocument unifiedSchema, string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(unifiedSchema, options);
        File.WriteAllText(outputPath, json);
    }

    /// <summary>
    /// Exports an individual extension schema to a JSON file.
    /// </summary>
    /// <param name="schemaDefinition">The schema definition to export.</param>
    /// <param name="outputPath">The path where the JSON file will be written.</param>
    public static void ExportExtensionSchema(SchemaDefinition schemaDefinition, string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(schemaDefinition, options);
        File.WriteAllText(outputPath, json);
    }

    /// <summary>
    /// Creates a unified schema from all configuration models in the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for configuration models.</param>
    /// <param name="extensionName">Name of the extension.</param>
    /// <param name="namespace">Namespace containing the configuration models.</param>
    /// <returns>A schema definition for the extension.</returns>
    public static SchemaDefinition CreateSchemaFromAssembly(System.Reflection.Assembly assembly, string extensionName, string @namespace)
    {
        var configTypes = assembly.GetTypes()
            .Where(t => t.IsClass && 
                       t.IsPublic && 
                       t.Namespace == @namespace &&
                       (t.Name.EndsWith("Config") || 
                        t.Name.EndsWith("Configuration") || 
                        t.Name.EndsWith("Options") ||
                        t.Name.EndsWith("Settings")))
            .OrderBy(t => t.Name)
            .ToList();

        var schemaTypes = new List<SchemaTypeInfo>();

        foreach (var type in configTypes)
        {
            var properties = type.GetProperties()
                .Where(p => p.CanRead && p.CanWrite)
                .OrderBy(p => p.Name)
                .Select(p => new SchemaPropertyInfo
                {
                    Name = p.Name,
                    TypeName = GetTypeName(p.PropertyType),
                    IsRequired = IsRequired(p),
                    IsNullable = IsNullable(p.PropertyType)
                })
                .ToList();

            schemaTypes.Add(new SchemaTypeInfo
            {
                TypeName = type.Name,
                Namespace = type.Namespace ?? "",
                Properties = properties
            });
        }

        var schema = new SchemaDefinition
        {
            Types = schemaTypes,
            Metadata = new SchemaMetadata
            {
                ExtensionName = extensionName,
                Namespace = @namespace,
                GeneratedAt = DateTimeOffset.UtcNow
            }
        };

        // Compute hash and version
        var normalized = SchemaHasher.NormalizeSchema(schema);
        schema.Metadata.SchemaHash = SchemaHasher.ComputeHash(normalized);
        schema.NormalizedSchema = normalized;

        return schema;
    }

    private static string GetTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var genericTypeName = type.GetGenericTypeDefinition().Name;
            var index = genericTypeName.IndexOf('`');
            if (index > 0)
            {
                genericTypeName = genericTypeName.Substring(0, index);
            }

            var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetTypeName));
            return $"{genericTypeName}<{genericArgs}>";
        }

        return type.Name;
    }

    private static bool IsRequired(System.Reflection.PropertyInfo property)
    {
        var requiredAttribute = property.GetCustomAttributes(false)
            .Any(a => a.GetType().Name == "RequiredAttribute");

        return requiredAttribute || (!property.PropertyType.IsValueType && 
                                     Nullable.GetUnderlyingType(property.PropertyType) == null &&
                                     !property.PropertyType.IsClass);
    }

    private static bool IsNullable(Type type)
    {
        return Nullable.GetUnderlyingType(type) != null ||
               (!type.IsValueType && type.IsClass);
    }
}
