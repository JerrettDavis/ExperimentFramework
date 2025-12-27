namespace ExperimentFramework.Configuration;

/// <summary>
/// Options for configuring experiment framework from files.
/// </summary>
public sealed class ExperimentFrameworkConfigurationOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json. Default is "ExperimentFramework".
    /// </summary>
    public string ConfigurationSectionName { get; set; } = "ExperimentFramework";

    /// <summary>
    /// Base path for resolving relative file paths.
    /// Default is the current working directory.
    /// </summary>
    public string? BasePath { get; set; }

    /// <summary>
    /// Whether to scan default file paths (experiments.yaml, ExperimentDefinitions/).
    /// Default is true.
    /// </summary>
    public bool ScanDefaultPaths { get; set; } = true;

    /// <summary>
    /// Additional file paths to scan. Supports relative paths, absolute paths,
    /// and glob patterns (e.g., "./configs/*.yaml").
    /// </summary>
    public List<string> AdditionalPaths { get; } = [];

    /// <summary>
    /// Additional assembly paths to search for type resolution.
    /// </summary>
    public List<string> AssemblySearchPaths { get; } = [];

    /// <summary>
    /// Type aliases for simplified type references in configuration.
    /// Maps alias names to actual types.
    /// </summary>
    public Dictionary<string, Type> TypeAliases { get; } = [];

    /// <summary>
    /// Whether to enable file watching for hot reload.
    /// Default is false.
    /// </summary>
    public bool EnableHotReload { get; set; }

    /// <summary>
    /// Callback invoked when configuration changes (for hot reload).
    /// </summary>
    public Action<Models.ExperimentFrameworkConfigurationRoot>? OnConfigurationChanged { get; set; }

    /// <summary>
    /// Whether to throw on validation errors.
    /// If false, errors are logged and invalid items are skipped.
    /// Default is true.
    /// </summary>
    public bool ThrowOnValidationErrors { get; set; } = true;
}
