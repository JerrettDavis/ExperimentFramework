using System.Collections.Immutable;
using System.Linq;
using ExperimentFramework.Plugins.Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExperimentFramework.Plugins.Generators.Analyzers;

/// <summary>
/// Discovers plugin implementations from class declarations.
/// </summary>
internal static class ImplementationDiscovery
{
    private const string PluginImplementationAttributeName = "ExperimentFramework.Plugins.Manifest.PluginImplementationAttribute";

    /// <summary>
    /// Extracts implementation model from a class declaration.
    /// Returns null if the class should not be included in the manifest.
    /// </summary>
    public static PluginImplementationModel? Extract(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDecl)
            return null;

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
        if (classSymbol is null)
            return null;

        // Must be public and concrete (not abstract)
        if (classSymbol.DeclaredAccessibility != Accessibility.Public || classSymbol.IsAbstract)
            return null;

        // Check for [PluginImplementation] attribute
        var pluginImplAttr = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == PluginImplementationAttributeName);

        // Check if excluded
        if (pluginImplAttr is not null)
        {
            var excludeArg = pluginImplAttr.NamedArguments
                .FirstOrDefault(a => a.Key == "Exclude");
            if (excludeArg.Value.Value is true)
            {
                return new PluginImplementationModel(
                    FullTypeName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    ClassName: classSymbol.Name,
                    ExplicitAlias: null,
                    ServiceInterfaces: ImmutableArray<string>.Empty,
                    IsExcluded: true);
            }
        }

        // Get explicit alias if specified
        string? explicitAlias = null;
        if (pluginImplAttr is not null)
        {
            var aliasArg = pluginImplAttr.NamedArguments
                .FirstOrDefault(a => a.Key == "Alias");
            explicitAlias = aliasArg.Value.Value as string;
        }

        // Get explicit interface if specified
        INamedTypeSymbol? explicitInterface = null;
        if (pluginImplAttr is not null)
        {
            var interfaceArg = pluginImplAttr.NamedArguments
                .FirstOrDefault(a => a.Key == "ServiceInterface");
            if (interfaceArg.Value.Value is INamedTypeSymbol namedType)
            {
                explicitInterface = namedType;
            }
        }

        // Discover interfaces
        var interfaces = explicitInterface is not null
            ? ImmutableArray.Create(explicitInterface.ToDisplayString())
            : DiscoverInterfaces(classSymbol);

        if (interfaces.IsEmpty)
            return null;

        return new PluginImplementationModel(
            FullTypeName: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ClassName: classSymbol.Name,
            ExplicitAlias: explicitAlias,
            ServiceInterfaces: interfaces,
            IsExcluded: false);
    }

    /// <summary>
    /// Discovers non-system interfaces implemented by a class.
    /// </summary>
    private static ImmutableArray<string> DiscoverInterfaces(INamedTypeSymbol classSymbol)
    {
        return classSymbol.AllInterfaces
            .Where(i => !IsSystemInterface(i))
            .Select(i => i.ToDisplayString())
            .ToImmutableArray();
    }

    /// <summary>
    /// Determines if an interface is a system interface that should be excluded.
    /// </summary>
    private static bool IsSystemInterface(INamedTypeSymbol iface)
    {
        var ns = iface.ContainingNamespace?.ToDisplayString() ?? "";

        // Exclude common framework namespaces
        return ns.StartsWith("System") ||
               ns.StartsWith("Microsoft") ||
               ns.StartsWith("Polly") ||
               ns.StartsWith("Newtonsoft") ||
               ns == "";
    }
}
