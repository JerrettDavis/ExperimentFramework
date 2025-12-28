using System.Collections.Concurrent;
using ExperimentFramework.Configuration.Building;
using ExperimentFramework.Configuration.Exceptions;
using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Configuration.Loading;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Configuration;

/// <summary>
/// Extension methods for registering experiment framework from configuration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds experiment framework with configuration loaded from YAML/JSON files.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExperimentFrameworkFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddExperimentFrameworkFromConfiguration(configuration, _ => { });
    }

    /// <summary>
    /// Adds experiment framework with configuration loaded from YAML/JSON files.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="configure">Additional configuration options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExperimentFrameworkFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ExperimentFrameworkConfigurationOptions> configure)
    {
        var options = new ExperimentFrameworkConfigurationOptions();
        configure(options);

        // Create type resolver with configured aliases and assembly paths
        var typeResolver = new TypeResolver(options.AssemblySearchPaths, options.TypeAliases);

        // Register type resolver as singleton
        services.TryAddSingleton<ITypeResolver>(typeResolver);

        // Add extension registry with built-in handlers
        services.AddExperimentConfigurationExtensions();

        // Build service provider to get the registry and logger
        var serviceProvider = services.BuildServiceProvider();
        var extensionRegistry = serviceProvider.GetService<ConfigurationExtensionRegistry>();
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger<ConfigurationExperimentBuilder>();

        // Load configuration
        var loader = new ExperimentConfigurationLoader();
        var configRoot = loader.Load(configuration, options);

        // Validate configuration using the extension registry
        var validator = new ConfigurationValidator(extensionRegistry);
        var validationResult = validator.Validate(configRoot);

        // Log warnings
        foreach (var warning in validationResult.Warnings)
        {
            logger?.LogWarning("Configuration warning at {Path}: {Message}", warning.Path, warning.Message);
        }

        // Handle validation errors
        if (!validationResult.IsValid)
        {
            if (options.ThrowOnValidationErrors)
            {
                throw new ExperimentConfigurationException(
                    "Experiment configuration is invalid",
                    validationResult.Errors);
            }

            foreach (var error in validationResult.FatalErrors)
            {
                logger?.LogError("Configuration error at {Path}: {Message}", error.Path, error.Message);
            }
        }

        // Build framework configuration from loaded config using the extension registry
        var configBuilder = new ConfigurationExperimentBuilder(typeResolver, extensionRegistry, logger);
        var frameworkBuilder = configBuilder.Build(configRoot);

        // Register with the existing framework
        services.AddExperimentFramework(frameworkBuilder);

        // Set up hot reload if enabled
        if (options.EnableHotReload)
        {
            SetupHotReload(services, configuration, options, typeResolver, extensionRegistry, logger);
        }

        return services;
    }

    /// <summary>
    /// Adds experiment framework by merging programmatic and file-based configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="builder">The programmatic experiment framework builder.</param>
    /// <param name="configuration">The application configuration for additional settings.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExperimentFramework(
        this IServiceCollection services,
        ExperimentFrameworkBuilder builder,
        IConfiguration configuration)
    {
        return services.AddExperimentFramework(builder, configuration, _ => { });
    }

    /// <summary>
    /// Adds experiment framework by merging programmatic and file-based configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="builder">The programmatic experiment framework builder.</param>
    /// <param name="configuration">The application configuration for additional settings.</param>
    /// <param name="configure">Additional configuration options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExperimentFramework(
        this IServiceCollection services,
        ExperimentFrameworkBuilder builder,
        IConfiguration configuration,
        Action<ExperimentFrameworkConfigurationOptions> configure)
    {
        var options = new ExperimentFrameworkConfigurationOptions();
        configure(options);

        // Create type resolver
        var typeResolver = new TypeResolver(options.AssemblySearchPaths, options.TypeAliases);
        services.TryAddSingleton<ITypeResolver>(typeResolver);

        // Add extension registry with built-in handlers
        services.AddExperimentConfigurationExtensions();

        // Build service provider to get the registry and logger
        var serviceProvider = services.BuildServiceProvider();
        var extensionRegistry = serviceProvider.GetService<ConfigurationExtensionRegistry>();
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger<ConfigurationExperimentBuilder>();

        // Load configuration from files
        var loader = new ExperimentConfigurationLoader();
        var configRoot = loader.Load(configuration, options);

        // Validate configuration using the extension registry
        var validator = new ConfigurationValidator(extensionRegistry);
        var validationResult = validator.Validate(configRoot);

        // Log warnings
        foreach (var warning in validationResult.Warnings)
        {
            logger?.LogWarning("Configuration warning at {Path}: {Message}", warning.Path, warning.Message);
        }

        if (!validationResult.IsValid && options.ThrowOnValidationErrors)
        {
            throw new ExperimentConfigurationException(
                "Experiment configuration is invalid",
                validationResult.Errors);
        }

        // Merge file-based config into the programmatic builder using the extension registry
        var configBuilder = new ConfigurationExperimentBuilder(typeResolver, extensionRegistry, logger);
        configBuilder.MergeInto(builder, configRoot);

        // Register the merged builder
        return services.AddExperimentFramework(builder);
    }

    private static void SetupHotReload(
        IServiceCollection services,
        IConfiguration configuration,
        ExperimentFrameworkConfigurationOptions options,
        ITypeResolver typeResolver,
        ConfigurationExtensionRegistry? extensionRegistry,
        ILogger<ConfigurationExperimentBuilder>? logger)
    {
        var basePath = options.BasePath ?? Directory.GetCurrentDirectory();
        var fileDiscovery = new ConfigurationFileDiscovery();
        var discoveredFiles = fileDiscovery.DiscoverFiles(basePath, options);

        if (discoveredFiles.Count == 0)
        {
            logger?.LogDebug("Hot reload enabled but no configuration files discovered");
            return;
        }

        logger?.LogInformation(
            "Hot reload enabled for {FileCount} configuration file(s)",
            discoveredFiles.Count);

        // Register the configuration watcher as a hosted service
        var watcher = new ConfigurationFileWatcher(
            discoveredFiles,
            configuration,
            options,
            typeResolver,
            extensionRegistry,
            logger);

        services.AddSingleton(watcher);
        services.AddHostedService(sp => sp.GetRequiredService<ConfigurationFileWatcher>());
    }
}

/// <summary>
/// Watches configuration files for changes and triggers reload.
/// </summary>
internal sealed class ConfigurationFileWatcher : IHostedService, IDisposable
{
    private readonly IReadOnlyList<string> _filePaths;
    private readonly IConfiguration _configuration;
    private readonly ExperimentFrameworkConfigurationOptions _options;
    private readonly ITypeResolver _typeResolver;
    private readonly ConfigurationExtensionRegistry? _extensionRegistry;
    private readonly ILogger? _logger;
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly ConcurrentDictionary<string, DateTime> _lastChangeTime = new();
    private readonly TimeSpan _debounceInterval = TimeSpan.FromMilliseconds(500);
    private readonly object _reloadLock = new();
    private bool _disposed;

    public ConfigurationFileWatcher(
        IReadOnlyList<string> filePaths,
        IConfiguration configuration,
        ExperimentFrameworkConfigurationOptions options,
        ITypeResolver typeResolver,
        ConfigurationExtensionRegistry? extensionRegistry,
        ILogger? logger)
    {
        _filePaths = filePaths;
        _configuration = configuration;
        _options = options;
        _typeResolver = typeResolver;
        _extensionRegistry = extensionRegistry;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Group files by directory for efficient watching
        var directories = _filePaths
            .Select(Path.GetDirectoryName)
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var directory in directories)
        {
            if (directory == null || !Directory.Exists(directory))
                continue;

            var watcher = new FileSystemWatcher(directory)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            watcher.Changed += OnFileChanged;
            watcher.Created += OnFileChanged;
            watcher.Deleted += OnFileChanged;
            watcher.Renamed += OnFileRenamed;

            // Watch for yaml, yml, and json files
            watcher.Filter = "*.*";
            _watchers.Add(watcher);

            _logger?.LogDebug("Watching directory for configuration changes: {Directory}", directory);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
        }

        return Task.CompletedTask;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var extension = Path.GetExtension(e.FullPath).ToLowerInvariant();
        if (extension is not (".yaml" or ".yml" or ".json"))
            return;

        // Check if this file is one we're watching
        if (!_filePaths.Contains(e.FullPath, StringComparer.OrdinalIgnoreCase))
            return;

        // Debounce rapid successive changes
        var now = DateTime.UtcNow;
        if (_lastChangeTime.TryGetValue(e.FullPath, out var lastChange) &&
            now - lastChange < _debounceInterval)
        {
            return;
        }

        _lastChangeTime[e.FullPath] = now;

        _logger?.LogInformation(
            "Configuration file changed: {FilePath}, triggering reload",
            e.FullPath);

        TriggerReload();
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        // Handle as deletion of old + creation of new
        OnFileChanged(sender, new FileSystemEventArgs(WatcherChangeTypes.Deleted, Path.GetDirectoryName(e.OldFullPath) ?? "", e.OldName ?? ""));
        OnFileChanged(sender, new FileSystemEventArgs(WatcherChangeTypes.Created, Path.GetDirectoryName(e.FullPath) ?? "", e.Name ?? ""));
    }

    private void TriggerReload()
    {
        // Ensure only one reload happens at a time
        lock (_reloadLock)
        {
            try
            {
                var loader = new ExperimentConfigurationLoader();
                var configRoot = loader.Load(_configuration, _options);

                // Validate using the extension registry
                var validator = new ConfigurationValidator(_extensionRegistry);
                var validationResult = validator.Validate(configRoot);

                foreach (var warning in validationResult.Warnings)
                {
                    _logger?.LogWarning(
                        "Configuration reload warning at {Path}: {Message}",
                        warning.Path,
                        warning.Message);
                }

                if (!validationResult.IsValid)
                {
                    foreach (var error in validationResult.FatalErrors)
                    {
                        _logger?.LogError(
                            "Configuration reload error at {Path}: {Message}",
                            error.Path,
                            error.Message);
                    }

                    _logger?.LogWarning(
                        "Configuration reload failed validation with {ErrorCount} error(s). Keeping previous configuration.",
                        validationResult.FatalErrors.Count());

                    return;
                }

                // Invoke the callback with the new configuration
                _options.OnConfigurationChanged?.Invoke(configRoot);

                _logger?.LogInformation("Configuration successfully reloaded");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to reload configuration");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Changed -= OnFileChanged;
            watcher.Created -= OnFileChanged;
            watcher.Deleted -= OnFileChanged;
            watcher.Renamed -= OnFileRenamed;
            watcher.Dispose();
        }

        _watchers.Clear();
    }
}
