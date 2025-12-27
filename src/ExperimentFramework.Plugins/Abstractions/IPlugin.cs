namespace ExperimentFramework.Plugins.Abstractions;

/// <summary>
/// Marker interface for plugin entry points.
/// Plugins may optionally implement this interface to provide initialization logic.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Called when the plugin is loaded.
    /// Use this to perform any necessary initialization.
    /// </summary>
    /// <param name="context">The plugin context providing access to plugin metadata and services.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task OnLoadAsync(IPluginContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the plugin is about to be unloaded.
    /// Use this to perform cleanup and release resources.
    /// </summary>
    /// <param name="context">The plugin context.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task OnUnloadAsync(IPluginContext context, CancellationToken cancellationToken = default);
}
