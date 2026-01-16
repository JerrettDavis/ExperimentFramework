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
        var argument = FindDuplicateKeyArgument(root, diagnostic);
        
        if (argument?.Expression is not LiteralExpressionSyntax literal)
            return;

        var currentKey = literal.Token.ValueText;
        var newKey = GenerateUniqueKey(currentKey, root, argument);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Rename to '{newKey}'",
                createChangedDocument: c => RenameKeyAsync(context.Document, argument, newKey, c),
                equivalenceKey: nameof(DuplicateKeyCodeFixProvider)),
            diagnostic);
    }

    private static ArgumentSyntax? FindDuplicateKeyArgument(SyntaxNode root, Diagnostic diagnostic) =>
        root.FindToken(diagnostic.Location.SourceSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<ArgumentSyntax>()
            .FirstOrDefault();

    private static string GenerateUniqueKey(string baseKey, SyntaxNode root, ArgumentSyntax currentArgument)
    {
        var invocation = currentArgument.FirstAncestorOrSelf<InvocationExpressionSyntax>();
        var trialRoot = invocation != null ? FindTrialRootForArgument(invocation) : null;
        
        if (trialRoot == null)
            return baseKey + "2";

        var existingKeys = CollectExistingKeysInTrial(trialRoot, currentArgument);
        return GenerateUniqueKeyFromBase(baseKey, existingKeys);
    }

    private static System.Collections.Generic.HashSet<string> CollectExistingKeysInTrial(
        InvocationExpressionSyntax trialRoot,
        ArgumentSyntax currentArgument)
    {
        var existingKeys = new System.Collections.Generic.HashSet<string>();
        var current = trialRoot;
        
        while (current != null)
        {
            TryAddKeyFromInvocation(current, currentArgument, existingKeys);
            current = FindNextInvocationInChain(current);
        }

        return existingKeys;
    }

    private static void TryAddKeyFromInvocation(
        InvocationExpressionSyntax invocation,
        ArgumentSyntax currentArgument,
        System.Collections.Generic.HashSet<string> existingKeys)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax ma &&
            ma.Name.Identifier.Text is "AddCondition" or "AddVariant" or "AddTrial" &&
            invocation.ArgumentList.Arguments.Count > 0)
        {
            var arg = invocation.ArgumentList.Arguments[0];
            if (arg != currentArgument && arg.Expression is LiteralExpressionSyntax lit)
            {
                existingKeys.Add(lit.Token.ValueText);
            }
        }
    }

    private static string GenerateUniqueKeyFromBase(
        string baseKey,
        System.Collections.Generic.HashSet<string> existingKeys)
    {
        var newKey = baseKey;
        var counter = 2;
        
        while (existingKeys.Contains(newKey))
        {
            newKey = $"{baseKey}{counter}";
            counter++;
        }

        return newKey;
    }

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

    private static InvocationExpressionSyntax? FindTrialRootForArgument(InvocationExpressionSyntax invocation)
    {
        var current = invocation;
        
        while (current?.Expression is MemberAccessExpressionSyntax ma)
        {
            if (ma.Name.Identifier.Text == "Trial")
                return current;
                
            current = ma.Expression as InvocationExpressionSyntax;
        }
        
        return null;
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
