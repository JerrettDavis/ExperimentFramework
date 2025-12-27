using System.Linq;
using ExperimentFramework.Plugins.Generators.Models;
using Microsoft.CodeAnalysis;

namespace ExperimentFramework.Plugins.Generators.Analyzers;

/// <summary>
/// Analyzes [GeneratePluginManifest] assembly attribute.
/// </summary>
internal static class ManifestConfigAnalyzer
{
    private const string GeneratePluginManifestAttributeName = "ExperimentFramework.Plugins.Manifest.GeneratePluginManifestAttribute";

    /// <summary>
    /// Extracts manifest configuration from the assembly attribute.
    /// Returns null if the attribute is not present.
    /// </summary>
    public static ManifestConfigModel? Extract(GeneratorAttributeSyntaxContext context)
    {
        var attribute = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString() == GeneratePluginManifestAttributeName);

        if (attribute is null)
            return null;

        var id = GetNamedArgument<string>(attribute, "Id");
        var name = GetNamedArgument<string>(attribute, "Name");
        var description = GetNamedArgument<string>(attribute, "Description");
        var isolationMode = GetNamedArgument<int>(attribute, "IsolationMode");
        var sharedAssemblies = GetNamedArrayArgument(attribute, "SharedAssemblies");
        var supportsHotReload = GetNamedArgument<bool?>(attribute, "SupportsHotReload") ?? true;

        // Convert isolation mode enum to string
        var isolationModeStr = isolationMode switch
        {
            0 => "Full",
            1 => "Shared",
            2 => "None",
            _ => "Shared"
        };

        return new ManifestConfigModel(
            Id: id,
            Name: name,
            Description: description,
            IsolationMode: isolationModeStr,
            SharedAssemblies: sharedAssemblies,
            SupportsHotReload: supportsHotReload);
    }

    private static T? GetNamedArgument<T>(AttributeData attribute, string name)
    {
        var arg = attribute.NamedArguments.FirstOrDefault(a => a.Key == name);
        if (arg.Key is null)
            return default;

        return arg.Value.Value is T value ? value : default;
    }

    private static string[]? GetNamedArrayArgument(AttributeData attribute, string name)
    {
        var arg = attribute.NamedArguments.FirstOrDefault(a => a.Key == name);
        if (arg.Key is null || arg.Value.IsNull)
            return null;

        if (arg.Value.Kind == TypedConstantKind.Array)
        {
            return arg.Value.Values
                .Select(v => v.Value?.ToString())
                .Where(v => v is not null)
                .Cast<string>()
                .ToArray();
        }

        return null;
    }
}
