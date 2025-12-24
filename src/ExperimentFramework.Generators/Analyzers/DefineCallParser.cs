using ExperimentFramework.Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ExperimentFramework.Generators.Analyzers;

/// <summary>
/// Parses .Define&lt;TService&gt;(c => ...) invocations to extract experiment metadata.
/// </summary>
internal static class DefineCallParser
{
    /// <summary>
    /// Parses a Define&lt;TService&gt; invocation and extracts the experiment definition.
    /// </summary>
    public static ExperimentDefinitionModel? ParseDefineCall(
        InvocationExpressionSyntax defineInvocation,
        SemanticModel semanticModel)
    {
        // Extract service type from Define<TService>
        var serviceType = ExtractServiceType(defineInvocation, semanticModel);
        if (serviceType == null)
            return null;

        // Extract the lambda expression: c => c.UsingFeatureFlag(...)...
        var lambdaExpression = defineInvocation.ArgumentList.Arguments.FirstOrDefault()?.Expression as LambdaExpressionSyntax;
        if (lambdaExpression == null)
            return null;

        // Parse the lambda body to extract configuration
        var config = ParseServiceConfiguration(lambdaExpression, semanticModel);
        if (config == null)
            return null;

        return new ExperimentDefinitionModel(
            serviceType,
            config.SelectionMode,
            config.SelectorName,
            config.DefaultKey,
            config.Trials,
            config.ErrorPolicy,
            config.FallbackTrialKey,
            config.OrderedFallbackKeys);
    }

    /// <summary>
    /// Extracts the service type symbol from Define&lt;TService&gt;.
    /// </summary>
    private static INamedTypeSymbol? ExtractServiceType(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel)
    {
        // The invocation should be: builder.Define<TService>(...)
        // Expression is MemberAccessExpressionSyntax with Name being GenericNameSyntax
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return null;

        if (memberAccess.Name is not GenericNameSyntax genericName)
            return null;

        if (genericName.Identifier.Text != "Define" || genericName.TypeArgumentList.Arguments.Count != 1)
            return null;

        var typeInfo = semanticModel.GetTypeInfo(genericName.TypeArgumentList.Arguments[0]);
        return typeInfo.Type as INamedTypeSymbol;
    }

    /// <summary>
    /// Parses the service configuration lambda body.
    /// </summary>
    private static ServiceConfiguration? ParseServiceConfiguration(
        LambdaExpressionSyntax lambda,
        SemanticModel semanticModel)
    {
        // Get the body of the lambda (can be expression or block)
        var bodyExpression = lambda.Body as ExpressionSyntax;
        if (bodyExpression == null && lambda.Body is BlockSyntax block)
        {
            // If it's a block, get the return statement
            var returnStatement = block.Statements.OfType<ReturnStatementSyntax>().FirstOrDefault();
            bodyExpression = returnStatement?.Expression;
        }

        if (bodyExpression == null)
            return null;

        // Walk the invocation chain to collect all configuration calls
        var invocations = CollectInvocationChain(bodyExpression);

        // Parse each invocation
        var selectionMode = SelectionModeModel.BooleanFeatureFlag;
        string? selectorName = null;
        string? defaultKey = null;
        var trials = new Dictionary<string, INamedTypeSymbol>();
        var errorPolicy = ErrorPolicyModel.Throw;
        string? fallbackTrialKey = null;
        List<string>? orderedFallbackKeys = null;

        foreach (var invocation in invocations)
        {
            var methodName = GetMethodName(invocation);

            switch (methodName)
            {
                case "UsingFeatureFlag":
                    selectionMode = SelectionModeModel.BooleanFeatureFlag;
                    selectorName = ExtractStringArgument(invocation, 0);
                    break;

                case "UsingConfigurationKey":
                    selectionMode = SelectionModeModel.ConfigurationValue;
                    selectorName = ExtractStringArgument(invocation, 0);
                    break;

                case "UsingVariantFeatureFlag":
                    selectionMode = SelectionModeModel.VariantFeatureFlag;
                    selectorName = ExtractStringArgument(invocation, 0);
                    break;

                case "UsingStickyRouting":
                    selectionMode = SelectionModeModel.StickyRouting;
                    selectorName = ExtractStringArgument(invocation, 0);
                    break;

                case "AddDefaultTrial":
                    var defaultTrialType = ExtractGenericTypeArgument(invocation, semanticModel);
                    var defaultTrialKey = ExtractStringArgument(invocation, 0);
                    if (defaultTrialType != null && defaultTrialKey != null)
                    {
                        defaultKey = defaultTrialKey;
                        trials[defaultTrialKey] = defaultTrialType;
                    }
                    break;

                case "AddTrial":
                    var trialType = ExtractGenericTypeArgument(invocation, semanticModel);
                    var trialKey = ExtractStringArgument(invocation, 0);
                    if (trialType != null && trialKey != null)
                    {
                        trials[trialKey] = trialType;
                    }
                    break;

                case "OnErrorRedirectAndReplayDefault":
                    errorPolicy = ErrorPolicyModel.RedirectAndReplayDefault;
                    break;

                case "OnErrorRedirectAndReplayAny":
                    errorPolicy = ErrorPolicyModel.RedirectAndReplayAny;
                    break;

                case "OnErrorRedirectAndReplay":
                    errorPolicy = ErrorPolicyModel.RedirectAndReplay;
                    fallbackTrialKey = ExtractStringArgument(invocation, 0);
                    break;

                case "OnErrorRedirectAndReplayOrdered":
                    errorPolicy = ErrorPolicyModel.RedirectAndReplayOrdered;
                    orderedFallbackKeys = ExtractStringArrayArgument(invocation);
                    break;

                case "OnErrorThrow":
                    errorPolicy = ErrorPolicyModel.Throw;
                    break;
            }
        }

        // Validate we have required data
        if (defaultKey == null || trials.Count == 0)
            return null;

        return new ServiceConfiguration(
            selectionMode,
            selectorName ?? "", // Will be filled by naming convention if empty
            defaultKey,
            trials.ToImmutableDictionary(),
            errorPolicy,
            fallbackTrialKey,
            orderedFallbackKeys?.ToImmutableArray());
    }

    /// <summary>
    /// Collects all method invocations in a fluent chain.
    /// </summary>
    private static List<InvocationExpressionSyntax> CollectInvocationChain(ExpressionSyntax expression)
    {
        var invocations = new List<InvocationExpressionSyntax>();
        var current = expression;

        while (current != null)
        {
            if (current is InvocationExpressionSyntax invocation)
            {
                invocations.Add(invocation);

                // Move to the expression being invoked (walk up the chain)
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    current = memberAccess.Expression;
                }
                else
                {
                    break;
                }
            }
            else if (current is MemberAccessExpressionSyntax memberAccess)
            {
                current = memberAccess.Expression;
            }
            else
            {
                break;
            }
        }

        // Reverse to get original order (we walked from end to start)
        invocations.Reverse();
        return invocations;
    }

    /// <summary>
    /// Gets the method name from an invocation expression.
    /// </summary>
    private static string? GetMethodName(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Name.Identifier.Text;
        }
        else if (invocation.Expression is IdentifierNameSyntax identifier)
        {
            return identifier.Identifier.Text;
        }

        return null;
    }

    /// <summary>
    /// Extracts a string literal argument at the specified index.
    /// </summary>
    private static string? ExtractStringArgument(InvocationExpressionSyntax invocation, int argumentIndex)
    {
        if (argumentIndex >= invocation.ArgumentList.Arguments.Count)
            return null;

        var argument = invocation.ArgumentList.Arguments[argumentIndex].Expression;

        if (argument is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return literal.Token.ValueText;
        }

        return null;
    }

    /// <summary>
    /// Extracts the generic type argument from a method call like AddDefaultTrial&lt;TImpl&gt;().
    /// </summary>
    private static INamedTypeSymbol? ExtractGenericTypeArgument(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel)
    {
        GenericNameSyntax? genericName = null;

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name is GenericNameSyntax generic)
        {
            genericName = generic;
        }
        else if (invocation.Expression is GenericNameSyntax directGeneric)
        {
            genericName = directGeneric;
        }

        if (genericName == null || genericName.TypeArgumentList.Arguments.Count != 1)
            return null;

        var typeInfo = semanticModel.GetTypeInfo(genericName.TypeArgumentList.Arguments[0]);
        return typeInfo.Type as INamedTypeSymbol;
    }

    /// <summary>
    /// Extracts string array arguments (params string[]) from an invocation.
    /// </summary>
    private static List<string>? ExtractStringArrayArgument(InvocationExpressionSyntax invocation)
    {
        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count == 0)
            return null;

        var stringList = new List<string>();

        foreach (var argument in arguments)
        {
            if (argument.Expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                stringList.Add(literal.Token.ValueText);
            }
        }

        return stringList.Count > 0 ? stringList : null;
    }

    /// <summary>
    /// Intermediate result of parsing service configuration.
    /// </summary>
    private sealed class ServiceConfiguration
    {
        public ServiceConfiguration(
            SelectionModeModel selectionMode,
            string selectorName,
            string defaultKey,
            ImmutableDictionary<string, INamedTypeSymbol> trials,
            ErrorPolicyModel errorPolicy,
            string? fallbackTrialKey = null,
            ImmutableArray<string>? orderedFallbackKeys = null)
        {
            SelectionMode = selectionMode;
            SelectorName = selectorName;
            DefaultKey = defaultKey;
            Trials = trials;
            ErrorPolicy = errorPolicy;
            FallbackTrialKey = fallbackTrialKey;
            OrderedFallbackKeys = orderedFallbackKeys;
        }

        public SelectionModeModel SelectionMode { get; }
        public string SelectorName { get; }
        public string DefaultKey { get; }
        public ImmutableDictionary<string, INamedTypeSymbol> Trials { get; }
        public ErrorPolicyModel ErrorPolicy { get; }
        public string? FallbackTrialKey { get; }
        public ImmutableArray<string>? OrderedFallbackKeys { get; }
    }
}
