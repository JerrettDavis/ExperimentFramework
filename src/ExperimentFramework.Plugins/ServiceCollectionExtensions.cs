using ExperimentFramework.Configuration.Building;
using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Configuration;
using ExperimentFramework.Plugins.HotReload;
using ExperimentFramework.Plugins.Integration;
using ExperimentFramework.Plugins.Loading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExperimentFramework.Plugins;

/// <summary>
/// Extension methods for registering plugin services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the experiment framework plugin system to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExperimentPlugins(
        this IServiceCollection services,
        Action<PluginConfigurationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure options
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.AddOptions<PluginConfigurationOptions>();
        }

        // Register core services
        services.TryAddSingleton<SharedTypeRegistry>();
        services.TryAddSingleton<IPluginLoader, PluginLoader>();
        services.TryAddSingleton<IPluginManager, PluginManager>();

        // Register discovery service
        services.AddHostedService<PluginDiscoveryService>();

        return services;
    }

    /// <summary>
    /// Adds the experiment framework plugin system with hot reload support.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExperimentPluginsWithHotReload(
        this IServiceCollection services,
        Action<PluginConfigurationOptions>? configure = null)
    {
        services.AddExperimentPlugins(opts =>
        {
            opts.EnableHotReload = true;
            configure?.Invoke(opts);
        });

        // Register hot reload service
        services.AddHostedService<PluginReloadService>();

        return services;
    }

    /// <summary>
    /// Decorates the existing <see cref="ITypeResolver"/> with plugin type resolution support.
    /// Call this after <see cref="AddExperimentPlugins"/> and after registering the base type resolver.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPluginTypeResolver(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Decorate the existing type resolver
        services.Decorate<ITypeResolver>((inner, sp) =>
        {
            var pluginManager = sp.GetRequiredService<IPluginManager>();
            return new PluginTypeResolver(inner, pluginManager);
        });

        return services;
    }

    /// <summary>
    /// Adds the experiment framework plugin system from YAML/JSON configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="pluginsConfig">The plugins configuration from YAML/JSON.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExperimentPluginsFromConfiguration(
        this IServiceCollection services,
        PluginsConfig pluginsConfig)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(pluginsConfig);

        var options = pluginsConfig.ToOptions();

        services.AddExperimentPlugins(opts =>
        {
            opts.DiscoveryPaths = options.DiscoveryPaths;
            opts.DefaultIsolationMode = options.DefaultIsolationMode;
            opts.DefaultSharedAssemblies = options.DefaultSharedAssemblies;
            opts.EnableHotReload = options.EnableHotReload;
            opts.HotReloadDebounceMs = options.HotReloadDebounceMs;
        });

        if (options.EnableHotReload)
        {
            services.AddHostedService<PluginReloadService>();
        }

        return services;
    }
}

/// <summary>
/// Service collection decorator extensions.
/// </summary>
internal static class ServiceCollectionDecoratorExtensions
{
    /// <summary>
    /// Decorates a registered service with a decorator.
    /// </summary>
    public static IServiceCollection Decorate<TService>(
        this IServiceCollection services,
        Func<TService, IServiceProvider, TService> decorator)
        where TService : class
    {
        var descriptorToDecorate = services.LastOrDefault(d => d.ServiceType == typeof(TService));

        if (descriptorToDecorate is null)
        {
            throw new InvalidOperationException(
                $"No service of type {typeof(TService).Name} is registered to decorate.");
        }

        // Create a factory that wraps the original service
        object DecoratorFactory(IServiceProvider sp)
        {
            var originalService = CreateOriginalService(sp, descriptorToDecorate);
            return decorator((TService)originalService, sp);
        }

        // Replace the original descriptor with the decorated version
        services.Remove(descriptorToDecorate);
        services.Add(ServiceDescriptor.Describe(
            typeof(TService),
            DecoratorFactory,
            descriptorToDecorate.Lifetime));

        return services;
    }

    private static object CreateOriginalService(IServiceProvider sp, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance is not null)
        {
            return descriptor.ImplementationInstance;
        }

        if (descriptor.ImplementationFactory is not null)
        {
            return descriptor.ImplementationFactory(sp);
        }

        if (descriptor.ImplementationType is not null)
        {
            return ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType);
        }

        throw new InvalidOperationException("Invalid service descriptor");
    }
}
