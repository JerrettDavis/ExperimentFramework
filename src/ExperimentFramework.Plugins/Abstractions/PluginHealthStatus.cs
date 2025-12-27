namespace ExperimentFramework.Plugins.Abstractions;

/// <summary>
/// Overall health status of the plugin system.
/// </summary>
public enum PluginSystemHealthState
{
    /// <summary>
    /// All plugins are healthy and operational.
    /// </summary>
    Healthy,

    /// <summary>
    /// Some plugins have warnings but the system is operational.
    /// </summary>
    Degraded,

    /// <summary>
    /// Critical failures detected in the plugin system.
    /// </summary>
    Unhealthy
}

/// <summary>
/// Health status of an individual plugin.
/// </summary>
public enum PluginHealthState
{
    /// <summary>
    /// The plugin is loaded and operational.
    /// </summary>
    Healthy,

    /// <summary>
    /// The plugin is loaded but has warnings.
    /// </summary>
    Warning,

    /// <summary>
    /// The plugin has errors and may not function correctly.
    /// </summary>
    Error,

    /// <summary>
    /// The plugin is not loaded or has been unloaded.
    /// </summary>
    Unloaded
}

/// <summary>
/// Health status details for an individual plugin.
/// </summary>
/// <param name="PluginId">The plugin identifier.</param>
/// <param name="State">The health state of the plugin.</param>
/// <param name="Version">The plugin version.</param>
/// <param name="LoadedAt">When the plugin was loaded.</param>
/// <param name="AssemblyCount">Number of assemblies loaded by this plugin.</param>
/// <param name="TypeCount">Number of types discovered in this plugin.</param>
/// <param name="IsolationMode">The isolation mode used for this plugin.</param>
/// <param name="Message">Optional status message or error details.</param>
/// <param name="Warnings">Any warnings associated with this plugin.</param>
public sealed record PluginHealthDetails(
    string PluginId,
    PluginHealthState State,
    string Version,
    DateTimeOffset LoadedAt,
    int AssemblyCount,
    int TypeCount,
    string IsolationMode,
    string? Message = null,
    IReadOnlyList<string>? Warnings = null);

/// <summary>
/// Overall health status of the plugin system.
/// </summary>
/// <param name="State">The overall health state.</param>
/// <param name="TotalPlugins">Total number of plugins registered.</param>
/// <param name="HealthyPlugins">Number of healthy plugins.</param>
/// <param name="DegradedPlugins">Number of degraded plugins.</param>
/// <param name="UnhealthyPlugins">Number of unhealthy plugins.</param>
/// <param name="HotReloadEnabled">Whether hot reload is enabled.</param>
/// <param name="Message">Optional overall status message.</param>
/// <param name="Plugins">Detailed status for each plugin.</param>
/// <param name="FailedLoads">Plugins that failed to load.</param>
public sealed record PluginSystemHealth(
    PluginSystemHealthState State,
    int TotalPlugins,
    int HealthyPlugins,
    int DegradedPlugins,
    int UnhealthyPlugins,
    bool HotReloadEnabled,
    string? Message,
    IReadOnlyList<PluginHealthDetails> Plugins,
    IReadOnlyList<PluginLoadFailure> FailedLoads);

/// <summary>
/// Details about a failed plugin load attempt.
/// </summary>
/// <param name="PluginPath">The path that was attempted.</param>
/// <param name="ErrorMessage">The error message.</param>
/// <param name="FailedAt">When the failure occurred.</param>
public sealed record PluginLoadFailure(
    string PluginPath,
    string ErrorMessage,
    DateTimeOffset FailedAt);
