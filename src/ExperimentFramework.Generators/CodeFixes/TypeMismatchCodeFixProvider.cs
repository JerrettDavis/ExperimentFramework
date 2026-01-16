using System.Collections.Generic;
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
        var (root, semanticModel) = await GetDocumentContext(context);
        if (root == null || semanticModel == null)
            return;

        var diagnostic = context.Diagnostics.First();
        if (!TryGetGenericNameAndServiceType(root, diagnostic, semanticModel, context.CancellationToken, 
            out var genericName, out var serviceType))
            return;

        // After successful Try pattern, both should be non-null
        if (genericName == null || serviceType == null)
            return;

        var candidates = FindImplementingTypes(semanticModel.Compilation, serviceType, context.CancellationToken);
        RegisterCodeFixesForCandidates(context, diagnostic, genericName, candidates);
    }

    private static async Task<(SyntaxNode? root, SemanticModel? model)> GetDocumentContext(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        return (root, model);
    }

    private static bool TryGetGenericNameAndServiceType(
        SyntaxNode root,
        Diagnostic diagnostic,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out GenericNameSyntax? genericName,
        out ITypeSymbol? serviceType)
    {
        genericName = null;
        serviceType = null;

        var invocation = FindInvocationExpression(root, diagnostic);
        if (invocation?.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        if (memberAccess.Name is not GenericNameSyntax gn)
            return false;

        genericName = gn;

        if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol methodSymbol)
            return false;

        if (methodSymbol.ContainingType is not INamedTypeSymbol { TypeArguments.Length: 1 } containingType)
            return false;

        serviceType = containingType.TypeArguments[0];
        return true;
    }

    private static InvocationExpressionSyntax? FindInvocationExpression(SyntaxNode root, Diagnostic diagnostic) =>
        root.FindToken(diagnostic.Location.SourceSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault();

    private static void RegisterCodeFixesForCandidates(
        CodeFixContext context,
        Diagnostic diagnostic,
        GenericNameSyntax genericName,
        ImmutableArray<INamedTypeSymbol> candidates)
    {
        foreach (var candidate in candidates.Take(MaxCandidateSuggestions))
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Change to '{candidate.Name}'",
                    createChangedDocument: c => ChangeGenericTypeAsync(context.Document, genericName, candidate, c),
                    equivalenceKey: $"{nameof(TypeMismatchCodeFixProvider)}_{candidate.ToDisplayString()}"),
                diagnostic);
        }
    }

    private static ImmutableArray<INamedTypeSymbol> FindImplementingTypes(
        Compilation compilation,
        ITypeSymbol serviceType,
        CancellationToken cancellationToken)
    {
        return compilation.SyntaxTrees
            .Where(tree => !cancellationToken.IsCancellationRequested)
            .SelectMany(tree => GetCandidateTypesFromTree(tree, compilation, serviceType, cancellationToken))
            .OrderBy(t => t.Name)
            .ToImmutableArray();
    }

    private static IEnumerable<INamedTypeSymbol> GetCandidateTypesFromTree(
        SyntaxTree tree,
        Compilation compilation,
        ITypeSymbol serviceType,
        CancellationToken cancellationToken)
    {
        var semanticModel = compilation.GetSemanticModel(tree);
        var root = tree.GetRoot(cancellationToken);

        return root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(c => !c.Modifiers.Any(SyntaxKind.AbstractKeyword))
            .Select(c => semanticModel.GetDeclaredSymbol(c, cancellationToken))
            .OfType<INamedTypeSymbol>()
            .Where(type => !type.IsAbstract && ImplementsInterface(type, serviceType));
    }

    private static bool ImplementsInterface(ITypeSymbol implType, ITypeSymbol serviceType) =>
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
