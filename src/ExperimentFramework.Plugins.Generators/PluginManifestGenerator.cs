using System.Collections.Immutable;
using System.Linq;
using ExperimentFramework.Plugins.Generators.Analyzers;
using ExperimentFramework.Plugins.Generators.CodeGen;
using ExperimentFramework.Plugins.Generators.Diagnostics;
using ExperimentFramework.Plugins.Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExperimentFramework.Plugins.Generators;

/// <summary>
/// Source generator that automatically generates plugin manifests at compile time.
/// </summary>
[Generator]
public sealed class PluginManifestGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Pipeline 1: Find optional [GeneratePluginManifest] assembly attribute
        var manifestConfig = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "ExperimentFramework.Plugins.Manifest.GeneratePluginManifestAttribute",
                predicate: static (node, _) => true,
                transform: static (ctx, _) => ManifestConfigAnalyzer.Extract(ctx))
            .Where(static m => m is not null)
            .Collect();

        // Pipeline 2: Find ALL public concrete class declarations
        var allClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => ImplementationDiscovery.Extract(ctx))
            .Where(static i => i is not null)
            .Collect();

        // Pipeline 3: Get assembly info
        var assemblyInfo = context.CompilationProvider
            .Select(static (compilation, _) =>
            {
                var name = compilation.AssemblyName ?? "UnknownPlugin";
                var version = compilation.Assembly.Identity.Version.ToString();
                if (string.IsNullOrEmpty(version) || version == "0.0.0.0")
                    version = "1.0.0";
                return new AssemblyInfoModel(name, version);
            });

        // Combine all pipelines
        var combined = manifestConfig
            .Combine(allClasses)
            .Combine(assemblyInfo);

        // Generate output
        context.RegisterSourceOutput(combined,
            static (ctx, data) => Generate(ctx, data.Left.Left, data.Left.Right!, data.Right));
    }

    private static void Generate(
        SourceProductionContext context,
        ImmutableArray<ManifestConfigModel?> configArray,
        ImmutableArray<PluginImplementationModel?> implementations,
        AssemblyInfoModel assemblyInfo)
    {
        // Get config (first one wins if multiple)
        var config = configArray.FirstOrDefault(c => c is not null);

        // Filter to non-null, non-excluded implementations
        var validImplementations = implementations
            .Where(i => i is not null && !i.IsExcluded && i.ServiceInterfaces.Length > 0)
            .Cast<PluginImplementationModel>()
            .ToList();

        // If no implementations found, report a warning and exit
        if (validImplementations.Count == 0)
        {
            // Only warn if [GeneratePluginManifest] was explicitly used
            if (config is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    PluginManifestDiagnostics.NoImplementationsFound,
                    Location.None,
                    assemblyInfo.AssemblyName));
            }
            return;
        }

        // Check for duplicate aliases per interface
        var duplicates = validImplementations
            .SelectMany(impl => impl.ServiceInterfaces.Select(iface => (
                Interface: iface,
                Alias: impl.ExplicitAlias ?? AliasGenerator.GenerateAlias(impl.ClassName))))
            .GroupBy(x => (x.Interface, x.Alias))
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var dup in duplicates)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                PluginManifestDiagnostics.DuplicateAlias,
                Location.None,
                dup.Key.Alias,
                dup.Key.Interface));
        }

        // Generate the source
        var source = ManifestAttributeBuilder.Build(assemblyInfo, config, validImplementations);

        context.AddSource("PluginManifest.g.cs", source);
    }
}
