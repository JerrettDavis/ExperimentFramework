using System.Collections.Immutable;

namespace ExperimentFramework.Plugins.Generators.Models;

/// <summary>
/// Represents a discovered plugin implementation.
/// </summary>
internal sealed record PluginImplementationModel(
    string FullTypeName,
    string ClassName,
    string? ExplicitAlias,
    ImmutableArray<string> ServiceInterfaces,
    bool IsExcluded);
