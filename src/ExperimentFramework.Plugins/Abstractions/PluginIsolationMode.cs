namespace ExperimentFramework.Plugins.Abstractions;

/// <summary>
/// Defines the isolation level for loaded plugins.
/// </summary>
public enum PluginIsolationMode
{
    /// <summary>
    /// Full isolation: Plugin loads in a separate AssemblyLoadContext with no shared types.
    /// Use for untrusted plugins or when version conflicts are expected.
    /// Types from different contexts cannot be directly cast to each other.
    /// </summary>
    Full,

    /// <summary>
    /// Shared isolation: Plugin loads in a separate context but shares specified assemblies with the host.
    /// Most common mode - allows DI integration while maintaining some isolation.
    /// Shared assemblies (like ExperimentFramework and DI abstractions) are loaded from the host context.
    /// </summary>
    Shared,

    /// <summary>
    /// No isolation: Plugin loads directly into the default AssemblyLoadContext.
    /// Use only for fully trusted plugins that need complete compatibility with the host.
    /// Cannot be unloaded without restarting the application.
    /// </summary>
    None
}
