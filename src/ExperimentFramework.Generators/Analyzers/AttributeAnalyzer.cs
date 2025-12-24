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
        => method.AttributeLists
            .SelectMany(
                attributeList => attributeList.Attributes, 
                (_, attribute) => semanticModel.GetSymbolInfo(attribute))
            .Select(symbolInfo => symbolInfo.Symbol).OfType<IMethodSymbol>()
            .Select(attributeSymbol => attributeSymbol.ContainingType)
            .Select(attributeType => new { attributeType, attributeName = attributeType.Name })
            .Select(t => new
            {
                t.attributeType, 
                t.attributeName, 
                attributeNamespace = t.attributeType.ContainingNamespace.ToDisplayString()
            })
            .Where(t => t.attributeName is "ExperimentCompositionRootAttribute" or "ExperimentCompositionRoot" && 
                        t.attributeNamespace == "ExperimentFramework")
            .Select(t => t.attributeName)
            .Any();

    /// <summary>
    /// Checks if an invocation is a Define&lt;T&gt; call syntactically.
    /// </summary>
    private static bool IsDefineCall(InvocationExpressionSyntax invocation)
        => invocation.Expression is MemberAccessExpressionSyntax
        {
            Name: GenericNameSyntax
            {
                Identifier.Text: "Define", 
                TypeArgumentList.Arguments.Count: 1
            }
        };
}
