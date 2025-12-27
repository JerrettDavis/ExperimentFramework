using Microsoft.CodeAnalysis;

namespace ExperimentFramework.Plugins.Generators.Diagnostics;

/// <summary>
/// Diagnostic descriptors for the plugin manifest generator.
/// </summary>
internal static class PluginManifestDiagnostics
{
    private const string Category = "ExperimentFramework.Plugins";

    public static readonly DiagnosticDescriptor NoImplementationsFound = new(
        id: "EFPG001",
        title: "No plugin implementations found",
        messageFormat: "No classes implementing non-system interfaces were found in assembly '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoDiscoverableInterface = new(
        id: "EFPG002",
        title: "No discoverable interface",
        messageFormat: "Class '{0}' does not implement any non-system interfaces",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateAlias = new(
        id: "EFPG003",
        title: "Duplicate alias",
        messageFormat: "Alias '{0}' is used by multiple implementations for interface '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SystemInterfaceDetected = new(
        id: "EFPG004",
        title: "System interface detected",
        messageFormat: "Interface '{0}' appears to be a system interface - likely not intended for plugin registration",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
