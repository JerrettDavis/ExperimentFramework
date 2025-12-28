using ExperimentFramework.Plugins.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExperimentFramework.Plugins.Configuration;

/// <summary>
/// Background service that discovers and loads plugins on application startup.
/// </summary>
public sealed class PluginDiscoveryService : IHostedService
{
    private readonly IPluginManager _pluginManager;
    private readonly PluginConfigurationOptions _options;
    private readonly ILogger<PluginDiscoveryService> _logger;

    /// <summary>
    /// Creates a new plugin discovery service.
    /// </summary>
    /// <param name="pluginManager">The plugin manager.</param>
    /// <param name="options">Configuration options.</param>
    /// <param name="logger">Logger.</param>
    public PluginDiscoveryService(
        IPluginManager pluginManager,
        IOptions<PluginConfigurationOptions> options,
        ILogger<PluginDiscoveryService> logger)
    {
        _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.AutoLoadOnStartup)
        {
            _logger.LogDebug("Auto-load on startup is disabled, skipping plugin discovery");
            return;
        }

        if (_options.DiscoveryPaths.Count == 0)
        {
            _logger.LogDebug("No discovery paths configured, skipping plugin discovery");
            return;
        }

        _logger.LogInformation(
            "Starting plugin discovery with {Count} discovery paths",
            _options.DiscoveryPaths.Count);

        try
        {
            var plugins = await _pluginManager.DiscoverAndLoadAsync(cancellationToken);

            _logger.LogInformation(
                "Plugin discovery complete. Loaded {Count} plugins: {Plugins}",
                plugins.Count,
                string.Join(", ", plugins.Select(p => $"{p.Manifest.Id} v{p.Manifest.Version}")));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Plugin discovery was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during plugin discovery");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Cleanup is handled by PluginManager disposal
        return Task.CompletedTask;
    }
}
