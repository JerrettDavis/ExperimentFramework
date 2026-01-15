using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExperimentFramework.Generators.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExperimentFramework.Generators.CodeFixes;

/// <summary>
/// Code fix provider for EF0001 and EF0002: Type does not implement service interface.
/// Offers to change the generic type argument to a type that implements the interface.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TypeMismatchCodeFixProvider)), Shared]
public sealed class TypeMismatchCodeFixProvider : CodeFixProvider
{
    /// <summary>
    /// Gets the diagnostic IDs that this code fix provider can fix.
    /// </summary>
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(
            ExperimentConfigurationAnalyzer.ControlTypeDoesNotImplementServiceType.Id,
            ExperimentConfigurationAnalyzer.ConditionTypeDoesNotImplementServiceType.Id);

    /// <summary>
    /// Gets the fix all provider for batch fixing.
    /// </summary>
    /// <returns>A fix all provider.</returns>
    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    /// <summary>
    /// Maximum number of candidate type suggestions to offer in code fixes.
    /// </summary>
    private const int MaxCandidateSuggestions = 5;

    /// <summary>
    /// Registers code fixes for the specified diagnostics.
    /// </summary>
    /// <param name="context">The code fix context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the invocation expression
        var invocation = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault();

        if (invocation?.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        // Get the generic name (AddControl<T> or AddCondition<T>)
        if (memberAccess.Name is not GenericNameSyntax genericName)
            return;

        // Get the service type from the containing ServiceExperimentBuilder<TService>
        var symbolInfo = semanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        if (methodSymbol.ContainingType is not INamedTypeSymbol { TypeArguments.Length: 1 } containingType)
            return;

        var serviceType = containingType.TypeArguments[0];

        // Find candidate types in the compilation that implement the service interface
        var candidates = FindImplementingTypes(semanticModel.Compilation, serviceType, context.CancellationToken);

        // Offer a code fix for each candidate (limited to avoid overwhelming the user)
        foreach (var candidate in candidates.Take(MaxCandidateSuggestions))
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Change to '{candidate.Name}'",
                    createChangedDocument: c => ChangeGenericTypeAsync(
                        context.Document,
                        genericName,
                        candidate,
                        c),
                    equivalenceKey: $"{nameof(TypeMismatchCodeFixProvider)}_{candidate.ToDisplayString()}"),
                diagnostic);
        }
    }

    private static ImmutableArray<INamedTypeSymbol> FindImplementingTypes(
        Compilation compilation,
        ITypeSymbol serviceType,
        CancellationToken cancellationToken)
    {
        var candidates = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

        // Search through all types in the compilation
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot(cancellationToken);

            var classDeclarations = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c => !c.Modifiers.Any(SyntaxKind.AbstractKeyword));

            foreach (var classDecl in classDeclarations)
            {
                var symbol = semanticModel.GetDeclaredSymbol(classDecl, cancellationToken);
                if (symbol is INamedTypeSymbol namedType && !namedType.IsAbstract)
                {
                    if (ImplementsInterface(namedType, serviceType))
                    {
                        candidates.Add(namedType);
                    }
                }
            }
        }

        // Sort by name for consistency
        return candidates
            .OrderBy(t => t.Name)
            .ToImmutableArray();
    }

    private static bool ImplementsInterface(ITypeSymbol implType, ITypeSymbol serviceType)
    {
        // Check if implType is the same as serviceType
        if (SymbolEqualityComparer.Default.Equals(implType, serviceType))
            return true;

        // Check base types (for class inheritance)
        var current = implType.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, serviceType))
                return true;
            current = current.BaseType;
        }

        // Check interfaces
        foreach (var iface in implType.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface, serviceType))
                return true;

            // Handle generic interfaces
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

    private static async Task<Document> ChangeGenericTypeAsync(
        Document document,
        GenericNameSyntax genericName,
        INamedTypeSymbol newType,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        // Create new type argument with fully qualified name to ensure it's accessible
        var fullyQualifiedTypeName = newType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var newTypeName = SyntaxFactory.ParseTypeName(fullyQualifiedTypeName)
            .WithTriviaFrom(genericName.TypeArgumentList.Arguments[0]);

        // Create new type argument list
        var newTypeArgumentList = SyntaxFactory.TypeArgumentList(
            SyntaxFactory.SingletonSeparatedList(newTypeName));

        // Replace the generic name
        var newGenericName = genericName.WithTypeArgumentList(newTypeArgumentList);
        var newRoot = root.ReplaceNode(genericName, newGenericName);

        return document.WithSyntaxRoot(newRoot);
    }
}
