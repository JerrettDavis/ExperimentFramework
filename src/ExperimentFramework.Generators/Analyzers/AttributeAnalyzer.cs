using ExperimentFramework.Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;

namespace ExperimentFramework.Generators.Analyzers;

/// <summary>
/// Analyzes methods decorated with [ExperimentCompositionRoot] attribute
/// to extract experiment definitions.
/// </summary>
internal static class AttributeAnalyzer
{
    /// <summary>
    /// Extracts experiment definitions from a method decorated with [ExperimentCompositionRoot].
    /// </summary>
    public static ExperimentDefinitionCollection? ExtractDefinitions(GeneratorSyntaxContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;

        // Verify this method has the ExperimentCompositionRoot attribute
        var hasAttribute = HasCompositionRootAttribute(method, context.SemanticModel);

        // DEBUG: Always return a diagnostic even if attribute check fails
        if (!hasAttribute)
        {
            // Return empty collection with debug info
            return new ExperimentDefinitionCollection(
                ImmutableArray<ExperimentDefinitionModel>.Empty,
                method.GetLocation());
        }

        // Find all Define<T> invocations in the method body
        var defineInvocations = method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(IsDefineCall)
            .ToImmutableArray();

        if (defineInvocations.Length == 0)
        {
            // Return empty collection - method has attribute but no Define calls
            return new ExperimentDefinitionCollection(
                ImmutableArray<ExperimentDefinitionModel>.Empty,
                method.GetLocation());
        }

        // Parse each Define call
        var definitions = defineInvocations
            .Select(inv => DefineCallParser.ParseDefineCall(inv, context.SemanticModel))
            .Where(def => def != null)
            .ToImmutableArray();

        return new ExperimentDefinitionCollection(
            definitions!,
            method.GetLocation());
    }

    /// <summary>
    /// Checks if a method has the [ExperimentCompositionRoot] attribute.
    /// </summary>
    private static bool HasCompositionRootAttribute(
        MethodDeclarationSyntax method,
        SemanticModel semanticModel)
    {
        foreach (var attributeList in method.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(attribute);
                var attributeSymbol = symbolInfo.Symbol as IMethodSymbol;

                if (attributeSymbol == null)
                    continue;

                var attributeType = attributeSymbol.ContainingType;
                var attributeName = attributeType.Name;
                var attributeNamespace = attributeType.ContainingNamespace.ToDisplayString();

                // Check if this is ExperimentCompositionRootAttribute
                // Also check without "Attribute" suffix since C# allows omitting it
                if ((attributeName == "ExperimentCompositionRootAttribute" ||
                     attributeName == "ExperimentCompositionRoot") &&
                    attributeNamespace == "ExperimentFramework")
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if an invocation is a Define&lt;T&gt; call syntactically.
    /// </summary>
    private static bool IsDefineCall(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name is GenericNameSyntax genericName)
        {
            return genericName.Identifier.Text == "Define" &&
                   genericName.TypeArgumentList.Arguments.Count == 1;
        }

        return false;
    }
}
