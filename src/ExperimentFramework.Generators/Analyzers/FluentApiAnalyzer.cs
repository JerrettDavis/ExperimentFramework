using System.Collections.Immutable;
using System.Linq;
using ExperimentFramework.Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExperimentFramework.Generators.Analyzers;

/// <summary>
/// Analyzes fluent API patterns to detect .UseSourceGenerators() calls
/// and extract experiment definitions.
/// </summary>
internal static class FluentApiAnalyzer
{
    /// <summary>
    /// Extracts experiment definitions from a fluent API call chain ending with .UseSourceGenerators().
    /// </summary>
    public static ExperimentDefinitionCollection? ExtractDefinitions(GeneratorSyntaxContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Verify this is actually a UseSourceGenerators() call
        if (!IsUseSourceGeneratorsCall(invocation, context.SemanticModel))
            return null;

        // Walk up the invocation chain to find all .Define<T>(...) calls
        var defineInvocations = CollectDefineInvocations(invocation);

        // Parse each Define call
        var definitions = defineInvocations
            .Select(inv => DefineCallParser.ParseDefineCall(inv, context.SemanticModel))
            .Where(def => def != null)
            .ToImmutableArray();

        if (definitions.Length == 0)
            return null;

        return new ExperimentDefinitionCollection(
            definitions!,
            invocation.GetLocation());
    }

    /// <summary>
    /// Verifies that an invocation is a call to UseSourceGenerators().
    /// </summary>
    private static bool IsUseSourceGeneratorsCall(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel)
    {
        // Check method name syntactically
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            if (memberAccess.Name.Identifier.Text != "UseSourceGenerators")
                return false;
        }
        else
        {
            return false;
        }

        // Verify semantically that this is ExperimentFrameworkBuilder.UseSourceGenerators()
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        var method = symbolInfo.Symbol as IMethodSymbol;

        if (method == null)
            return false;

        // Check that the containing type is ExperimentFrameworkBuilder
        return method.ContainingType.Name == "ExperimentFrameworkBuilder" &&
               method.ContainingType.ContainingNamespace.ToDisplayString() == "ExperimentFramework";
    }

    /// <summary>
    /// Walks up the invocation chain to collect all .Define&lt;T&gt;(...) calls.
    /// </summary>
    private static ImmutableArray<InvocationExpressionSyntax> CollectDefineInvocations(
        InvocationExpressionSyntax startInvocation)
    {
        var defineInvocations = ImmutableArray.CreateBuilder<InvocationExpressionSyntax>();
        var current = startInvocation;

        // Walk backwards through the fluent chain
        while (current != null)
        {
            // Check if this invocation is a Define<T> call
            if (IsDefineCall(current))
            {
                defineInvocations.Add(current);
            }

            // Move to the previous invocation in the chain
            if (current.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression is InvocationExpressionSyntax previousInvocation)
            {
                current = previousInvocation;
            }
            else
            {
                break;
            }
        }

        // Reverse to get original order (we walked from end to start)
        defineInvocations.Reverse();
        return defineInvocations.ToImmutable();
    }

    /// <summary>
    /// Checks if an invocation is a Define&lt;T&gt; or Trial&lt;T&gt; call syntactically.
    /// </summary>
    private static bool IsDefineCall(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name is GenericNameSyntax genericName)
        {
            // Support both "Define" and "Trial" method names
            var methodName = genericName.Identifier.Text;
            return (methodName == "Define" || methodName == "Trial") &&
                   genericName.TypeArgumentList.Arguments.Count == 1;
        }

        return false;
    }
}
