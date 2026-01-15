using System.Collections.Generic;
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
        
        if (!TryGetExperimentMethodInfo(invocation, context.SemanticModel, out var methodInfo))
            return;

        ValidateTypeImplementation(context, invocation, methodInfo);
        
        if (methodInfo.RequiresKeyValidation)
            AnalyzeDuplicateKeys(context, invocation, methodInfo.MemberAccess);
    }

    private static bool TryGetExperimentMethodInfo(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        out ExperimentMethodInfo methodInfo)
    {
        methodInfo = default;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        var methodName = memberAccess.Name.Identifier.Text;
        if (methodName is not ("AddControl" or "AddCondition" or "AddVariant" or "AddTrial" or "AddDefaultTrial"))
            return false;

        if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
            return false;

        if (!IsServiceExperimentBuilderMethod(methodSymbol))
            return false;

        if (methodSymbol.ContainingType is not INamedTypeSymbol { TypeArguments.Length: 1 } containingType ||
            methodSymbol.TypeArguments.Length != 1)
            return false;

        methodInfo = new ExperimentMethodInfo(
            methodName,
            memberAccess,
            containingType.TypeArguments[0],
            methodSymbol.TypeArguments[0]);

        return true;
    }

    private static bool IsServiceExperimentBuilderMethod(IMethodSymbol methodSymbol) =>
        methodSymbol.ContainingType.Name == "ServiceExperimentBuilder" &&
        methodSymbol.ContainingType.ContainingNamespace.ToDisplayString() == "ExperimentFramework";

    private static void ValidateTypeImplementation(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        ExperimentMethodInfo methodInfo)
    {
        if (ImplementsInterface(methodInfo.ImplType, methodInfo.ServiceType, context.Compilation))
            return;

        var descriptor = methodInfo.MethodName == "AddControl"
            ? ControlTypeDoesNotImplementServiceType
            : ConditionTypeDoesNotImplementServiceType;

        var diagnostic = Diagnostic.Create(
            descriptor,
            invocation.GetLocation(),
            methodInfo.ImplType.ToDisplayString(),
            methodInfo.ServiceType.ToDisplayString());

        context.ReportDiagnostic(diagnostic);
    }

    private readonly struct ExperimentMethodInfo
    {
        public ExperimentMethodInfo(
            string methodName,
            MemberAccessExpressionSyntax memberAccess,
            ITypeSymbol serviceType,
            ITypeSymbol implType)
        {
            MethodName = methodName;
            MemberAccess = memberAccess;
            ServiceType = serviceType;
            ImplType = implType;
        }

        public string MethodName { get; }
        public MemberAccessExpressionSyntax MemberAccess { get; }
        public ITypeSymbol ServiceType { get; }
        public ITypeSymbol ImplType { get; }

        public bool RequiresKeyValidation =>
            MethodName is "AddCondition" or "AddVariant" or "AddTrial";
    }

    private static void AnalyzeDuplicateKeys(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax currentInvocation,
        MemberAccessExpressionSyntax currentMemberAccess)
    {
        if (!TryGetKeyLiteral(currentInvocation, out var currentKey, out var currentKeyArg))
            return;

        var trialRoot = FindTrialRoot(currentMemberAccess);
        if (trialRoot == null)
            return;

        var allConditionCalls = FindAllConditionCallsInTrialChain(trialRoot, currentInvocation);
        var seenKeys = CollectKeysBeforeCurrent(allConditionCalls, currentInvocation, currentKey);

        if (!seenKeys.Add(currentKey))
            ReportDuplicateKey(context, currentKeyArg, currentKey);
    }

    private static bool TryGetKeyLiteral(
        InvocationExpressionSyntax invocation,
        out string key,
        out ArgumentSyntax keyArg)
    {
        key = string.Empty;
        keyArg = null!;

        if (invocation.ArgumentList.Arguments.Count == 0)
            return false;

        keyArg = invocation.ArgumentList.Arguments[0];
        if (keyArg.Expression is not LiteralExpressionSyntax keyLiteral)
            return false;

        key = keyLiteral.Token.ValueText;
        return true;
    }

    private static System.Collections.Generic.HashSet<string> CollectKeysBeforeCurrent(
        ImmutableArray<InvocationExpressionSyntax> allCalls,
        InvocationExpressionSyntax currentInvocation,
        string currentKey)
    {
        var seenKeys = new System.Collections.Generic.HashSet<string>();

        foreach (var call in allCalls)
        {
            if (call == currentInvocation)
                break;

            if (TryGetKeyLiteral(call, out var key, out _) && key == currentKey)
                seenKeys.Add(key);
        }

        return seenKeys;
    }

    private static void ReportDuplicateKey(
        SyntaxNodeAnalysisContext context,
        ArgumentSyntax keyArg,
        string key)
    {
        var diagnostic = Diagnostic.Create(
            DuplicateConditionKey,
            keyArg.GetLocation(),
            key);

        context.ReportDiagnostic(diagnostic);
    }

    private static InvocationExpressionSyntax? FindTrialRoot(MemberAccessExpressionSyntax memberAccess)
    {
        var current = memberAccess.Expression;

        while (current is InvocationExpressionSyntax invocation)
        {
            if (IsTrialInvocation(invocation))
                return invocation;

            current = invocation.Expression is MemberAccessExpressionSyntax { Expression: var expr }
                ? expr
                : null;
        }

        return current as InvocationExpressionSyntax;
    }

    private static bool IsTrialInvocation(InvocationExpressionSyntax invocation) =>
        invocation.Expression is MemberAccessExpressionSyntax ma &&
        ma.Name.Identifier.Text == "Trial";

    private static ImmutableArray<InvocationExpressionSyntax> FindAllConditionCallsInTrialChain(
        InvocationExpressionSyntax trialRoot,
        InvocationExpressionSyntax currentInvocation)
    {
        var calls = ImmutableArray.CreateBuilder<InvocationExpressionSyntax>();
        var current = trialRoot;
        
        while (current != null)
        {
            if (IsConditionInvocation(current))
                calls.Add(current);
            
            if (current == currentInvocation)
                break;
            
            current = FindNextInvocationInChain(current);
        }

        return calls.ToImmutable();
    }

    private static bool IsConditionInvocation(InvocationExpressionSyntax invocation) =>
        invocation.Expression is MemberAccessExpressionSyntax ma &&
        ma.Name.Identifier.Text is "AddCondition" or "AddVariant" or "AddTrial";

    private static InvocationExpressionSyntax? FindNextInvocationInChain(InvocationExpressionSyntax current)
    {
        var parent = current.Parent;
        
        while (parent != null)
        {
            if (parent is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression == current &&
                memberAccess.Parent is InvocationExpressionSyntax nextInvocation)
            {
                return nextInvocation;
            }
            parent = parent.Parent;
        }
        
        return null;
    }

    private static bool ImplementsInterface(ITypeSymbol implType, ITypeSymbol serviceType, Compilation compilation) =>
        SymbolEqualityComparer.Default.Equals(implType, serviceType) ||
        ImplementsViaInheritance(implType, serviceType) ||
        ImplementsViaInterface(implType, serviceType);

    private static bool ImplementsViaInheritance(ITypeSymbol implType, ITypeSymbol serviceType)
    {
        var current = implType.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, serviceType))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static bool ImplementsViaInterface(ITypeSymbol implType, ITypeSymbol serviceType) =>
        implType.AllInterfaces.Any(iface =>
            SymbolEqualityComparer.Default.Equals(iface, serviceType) ||
            IsMatchingGenericInterface(iface, serviceType));

    private static bool IsMatchingGenericInterface(ITypeSymbol iface, ITypeSymbol serviceType) =>
        serviceType is INamedTypeSymbol serviceNamedType &&
        iface is INamedTypeSymbol ifaceNamedType &&
        serviceNamedType.IsGenericType &&
        ifaceNamedType.IsGenericType &&
        SymbolEqualityComparer.Default.Equals(
            ifaceNamedType.ConstructedFrom,
            serviceNamedType.ConstructedFrom);
}
