using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExperimentFramework.Generators.Analyzers;

/// <summary>
/// Analyzer that detects common experiment configuration misconfigurations.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExperimentConfigurationAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic descriptor for EF0001: Control type does not implement service type.
    /// </summary>
    public static readonly DiagnosticDescriptor ControlTypeDoesNotImplementServiceType = new(
        id: "EF0001",
        title: "Control type does not implement service type",
        messageFormat: "Type '{0}' specified as control does not implement service interface '{1}'",
        category: "ExperimentFramework.Configuration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The control type must implement the service interface being experimented on.");

    /// <summary>
    /// Diagnostic descriptor for EF0002: Condition type does not implement service type.
    /// </summary>
    public static readonly DiagnosticDescriptor ConditionTypeDoesNotImplementServiceType = new(
        id: "EF0002",
        title: "Condition type does not implement service type",
        messageFormat: "Type '{0}' specified as condition does not implement service interface '{1}'",
        category: "ExperimentFramework.Configuration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The condition type must implement the service interface being experimented on.");

    /// <summary>
    /// Diagnostic descriptor for EF0003: Duplicate condition key in trial.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateConditionKey = new(
        id: "EF0003",
        title: "Duplicate condition key in trial",
        messageFormat: "Condition key '{0}' is already registered in this trial",
        category: "ExperimentFramework.Configuration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each condition key must be unique within a trial. Duplicate keys will result in the last registration overwriting earlier ones.");

    /// <summary>
    /// Diagnostic descriptor for EF0004: Trial declared but not registered.
    /// </summary>
    public static readonly DiagnosticDescriptor TrialNotRegistered = new(
        id: "EF0004",
        title: "Trial declared but not registered",
        messageFormat: "Trial for service '{0}' is declared but never registered with the service collection",
        category: "ExperimentFramework.Configuration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A trial is configured but the ExperimentFrameworkBuilder is never added to the service collection. Call AddExperimentFramework() or ensure the builder is registered.");

    /// <summary>
    /// Diagnostic descriptor for EF0005: Potential lifetime capture mismatch.
    /// </summary>
    public static readonly DiagnosticDescriptor LifetimeMismatch = new(
        id: "EF0005",
        title: "Potential lifetime capture mismatch",
        messageFormat: "Service '{0}' registered as Singleton depends on '{1}' which may be registered as Scoped. This can lead to captive dependencies.",
        category: "ExperimentFramework.Configuration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When a singleton service depends on a scoped service, it can lead to incorrect behavior. Ensure all dependencies have compatible lifetimes.");

    /// <summary>
    /// Gets the set of supported diagnostics by this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            ControlTypeDoesNotImplementServiceType,
            ConditionTypeDoesNotImplementServiceType,
            DuplicateConditionKey,
            TrialNotRegistered,
            LifetimeMismatch);

    /// <summary>
    /// Initializes the analyzer by registering actions for syntax node analysis.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // Register for invocation expressions to catch AddControl/AddCondition calls
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        
        // Check if this is an AddControl, AddCondition, or AddVariant call
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.Text;
        if (methodName is not ("AddControl" or "AddCondition" or "AddVariant" or "AddTrial" or "AddDefaultTrial"))
            return;

        // Get semantic information
        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        // Verify this is a ServiceExperimentBuilder method
        if (methodSymbol.ContainingType.Name != "ServiceExperimentBuilder" ||
            methodSymbol.ContainingType.ContainingNamespace.ToDisplayString() != "ExperimentFramework")
            return;

        // Extract the service type (TService) from ServiceExperimentBuilder<TService>
        if (methodSymbol.ContainingType is not INamedTypeSymbol { TypeArguments.Length: 1 } containingType)
            return;

        var serviceType = containingType.TypeArguments[0];

        // Extract the implementation type (TImpl) from the generic method
        if (methodSymbol.TypeArguments.Length != 1)
            return;

        var implType = methodSymbol.TypeArguments[0];

        // Check if implementation type implements service type (EF0001/EF0002)
        if (!ImplementsInterface(implType, serviceType, context.Compilation))
        {
            var diagnostic = methodName == "AddControl"
                ? Diagnostic.Create(
                    ControlTypeDoesNotImplementServiceType,
                    invocation.GetLocation(),
                    implType.ToDisplayString(),
                    serviceType.ToDisplayString())
                : Diagnostic.Create(
                    ConditionTypeDoesNotImplementServiceType,
                    invocation.GetLocation(),
                    implType.ToDisplayString(),
                    serviceType.ToDisplayString());

            context.ReportDiagnostic(diagnostic);
        }

        // Check for duplicate keys (EF0003)
        if (methodName is "AddCondition" or "AddVariant" or "AddTrial")
        {
            AnalyzeDuplicateKeys(context, invocation, memberAccess);
        }
    }

    private static void AnalyzeDuplicateKeys(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax currentInvocation,
        MemberAccessExpressionSyntax currentMemberAccess)
    {
        // Extract the key argument from current invocation
        if (currentInvocation.ArgumentList.Arguments.Count == 0)
            return;

        var currentKeyArg = currentInvocation.ArgumentList.Arguments[0];
        if (currentKeyArg.Expression is not LiteralExpressionSyntax currentKeyLiteral)
            return;

        var currentKey = currentKeyLiteral.Token.ValueText;

        // Walk up the invocation chain to find the beginning of the trial builder
        var trialRoot = FindTrialRoot(currentMemberAccess);
        if (trialRoot == null)
            return;

        // Find all AddCondition/AddVariant/AddTrial calls in this chain
        var allConditionCalls = FindAllConditionCalls(trialRoot);

        // Check for duplicates - only report on first occurrence to avoid multiple diagnostics
        var seenKeys = new System.Collections.Generic.HashSet<string>();
        var isFirstOccurrence = true;
        
        foreach (var otherCall in allConditionCalls)
        {
            if (otherCall.ArgumentList.Arguments.Count == 0)
                continue;

            var otherKeyArg = otherCall.ArgumentList.Arguments[0];
            if (otherKeyArg.Expression is not LiteralExpressionSyntax otherKeyLiteral)
                continue;

            var otherKey = otherKeyLiteral.Token.ValueText;
            
            if (otherKey == currentKey)
            {
                if (otherCall == currentInvocation)
                {
                    // This is the current invocation
                    if (!seenKeys.Add(currentKey))
                    {
                        // We've seen this key before, so this is a duplicate
                        var diagnostic = Diagnostic.Create(
                            DuplicateConditionKey,
                            currentKeyArg.GetLocation(),
                            currentKey);

                        context.ReportDiagnostic(diagnostic);
                    }
                    return; // Stop checking after processing current invocation
                }
                else
                {
                    // Found an earlier occurrence
                    seenKeys.Add(otherKey);
                    isFirstOccurrence = false;
                }
            }
        }
    }

    private static InvocationExpressionSyntax? FindTrialRoot(MemberAccessExpressionSyntax memberAccess)
    {
        var current = memberAccess.Expression;

        // Walk back through the chain until we find the Trial<T> call
        while (current != null)
        {
            if (current is InvocationExpressionSyntax invocation)
            {
                if (invocation.Expression is MemberAccessExpressionSyntax ma &&
                    ma.Name.Identifier.Text == "Trial")
                {
                    return invocation;
                }

                if (invocation.Expression is MemberAccessExpressionSyntax { Expression: var expr })
                {
                    current = expr;
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        return current as InvocationExpressionSyntax;
    }

    private static ImmutableArray<InvocationExpressionSyntax> FindAllConditionCalls(InvocationExpressionSyntax root)
    {
        var calls = ImmutableArray.CreateBuilder<InvocationExpressionSyntax>();
        
        // Find all invocations in the entire statement/expression
        var statement = root.FirstAncestorOrSelf<StatementSyntax>();
        if (statement == null)
            return calls.ToImmutable();

        var allInvocations = statement.DescendantNodes().OfType<InvocationExpressionSyntax>();
        
        foreach (var invocation in allInvocations)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax ma)
            {
                var methodName = ma.Name.Identifier.Text;
                if (methodName is "AddCondition" or "AddVariant" or "AddTrial")
                {
                    calls.Add(invocation);
                }
            }
        }

        return calls.ToImmutable();
    }

    private static bool ImplementsInterface(ITypeSymbol implType, ITypeSymbol serviceType, Compilation compilation)
    {
        // Handle the case where serviceType is the same as implType (class registered as itself)
        if (SymbolEqualityComparer.Default.Equals(implType, serviceType))
            return true;

        // Check if implType is derived from serviceType (class inheritance)
        var current = implType.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, serviceType))
                return true;
            current = current.BaseType;
        }

        // Check interfaces
        var allInterfaces = implType.AllInterfaces;
        foreach (var iface in allInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface, serviceType))
                return true;

            // Handle generic interface matching
            if (serviceType is INamedTypeSymbol serviceNamedType && 
                iface is INamedTypeSymbol ifaceNamedType)
            {
                if (serviceNamedType.IsGenericType && ifaceNamedType.IsGenericType)
                {
                    var unconstructedService = serviceNamedType.ConstructedFrom;
                    var unconstructedIface = ifaceNamedType.ConstructedFrom;
                    
                    if (SymbolEqualityComparer.Default.Equals(unconstructedIface, unconstructedService))
                        return true;
                }
            }
        }

        return false;
    }
}
