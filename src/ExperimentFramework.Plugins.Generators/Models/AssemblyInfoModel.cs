namespace ExperimentFramework.Plugins.Generators.Models;

/// <summary>
/// Represents assembly metadata for manifest generation.
/// </summary>
internal sealed record AssemblyInfoModel(
    string AssemblyName,
    string Version);
