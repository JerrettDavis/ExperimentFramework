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
/// Code fix provider for EF0003: Duplicate condition key.
/// Offers to rename the duplicate key to make it unique.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DuplicateKeyCodeFixProvider)), Shared]
public sealed class DuplicateKeyCodeFixProvider : CodeFixProvider
{
    /// <summary>
    /// Gets the diagnostic IDs that this code fix provider can fix.
    /// </summary>
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(ExperimentConfigurationAnalyzer.DuplicateConditionKey.Id);

    /// <summary>
    /// Gets the fix all provider for batch fixing.
    /// </summary>
    /// <returns>A fix all provider.</returns>
    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

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

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the argument that contains the duplicate key
        var argument = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<ArgumentSyntax>()
            .FirstOrDefault();

        if (argument?.Expression is not LiteralExpressionSyntax literal)
            return;

        var currentKey = literal.Token.ValueText;

        // Offer to rename to a unique key
        var newKey = GenerateUniqueKey(currentKey, root, argument);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Rename to '{newKey}'",
                createChangedDocument: c => RenameKeyAsync(context.Document, argument, newKey, c),
                equivalenceKey: nameof(DuplicateKeyCodeFixProvider)),
            diagnostic);
    }

    private static string GenerateUniqueKey(string baseKey, SyntaxNode root, ArgumentSyntax currentArgument)
    {
        // Find all existing keys in the same trial
        var existingKeys = new System.Collections.Generic.HashSet<string>(
            root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(inv => inv.Expression is MemberAccessExpressionSyntax ma &&
                              ma.Name.Identifier.Text is "AddCondition" or "AddVariant" or "AddTrial")
                .Select(inv => inv.ArgumentList.Arguments.FirstOrDefault())
                .Where(arg => arg != null && arg != currentArgument)
                .Select(arg => arg!.Expression)
                .OfType<LiteralExpressionSyntax>()
                .Select(lit => lit.Token.ValueText));

        // Generate a unique key by appending a number
        var newKey = baseKey;
        var counter = 2;
        while (existingKeys.Contains(newKey))
        {
            newKey = $"{baseKey}{counter}";
            counter++;
        }

        return newKey;
    }

    private static async Task<Document> RenameKeyAsync(
        Document document,
        ArgumentSyntax argument,
        string newKey,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        // Create new literal with the new key
        var newLiteral = SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal(newKey));

        // Replace the old argument with the new one
        var newArgument = argument.WithExpression(newLiteral);
        var newRoot = root.ReplaceNode(argument, newArgument);

        return document.WithSyntaxRoot(newRoot);
    }
}
