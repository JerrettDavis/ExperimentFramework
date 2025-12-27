namespace ExperimentFramework.Plugins.Generators.Models;

/// <summary>
/// Represents the plugin manifest configuration from [GeneratePluginManifest] attribute.
/// </summary>
internal sealed record ManifestConfigModel(
    string? Id,
    string? Name,
    string? Description,
    string IsolationMode,
    string[]? SharedAssemblies,
    bool SupportsHotReload);
