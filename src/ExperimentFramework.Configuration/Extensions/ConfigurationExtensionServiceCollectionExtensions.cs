using ExperimentFramework.Configuration.Building;
using ExperimentFramework.Configuration.Extensions.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExperimentFramework.Configuration.Extensions;

/// <summary>
/// Extension methods for registering configuration extension handlers.
/// </summary>
public static class ConfigurationExtensionServiceCollectionExtensions
{
    /// <summary>
    /// Adds the configuration extension registry with built-in handlers.
    /// This is called automatically by AddExperimentFrameworkFromConfiguration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExperimentConfigurationExtensions(this IServiceCollection services)
    {
        // Register the registry as singleton
        services.TryAddSingleton<ConfigurationExtensionRegistry>(sp =>
        {
            var registry = new ConfigurationExtensionRegistry();

            // Register built-in decorator handlers
            registry.RegisterDecoratorHandler(new LoggingDecoratorHandler());
            registry.RegisterDecoratorHandler(new TimeoutDecoratorHandler());

            // Custom decorator handler needs type resolver
            var typeResolver = sp.GetService<ITypeResolver>();
            if (typeResolver != null)
            {
                registry.RegisterDecoratorHandler(new CustomDecoratorHandler(typeResolver));
            }

            // Register built-in selection mode handlers
            registry.RegisterSelectionModeHandler(new FeatureFlagSelectionModeHandler());
            registry.RegisterSelectionModeHandler(new ConfigurationKeySelectionModeHandler());
            registry.RegisterSelectionModeHandler(new CustomSelectionModeHandler());

            // Register built-in backplane handlers
            registry.RegisterBackplaneHandler(new InMemoryBackplaneConfigurationHandler());
            registry.RegisterBackplaneHandler(new LoggingBackplaneConfigurationHandler());
            registry.RegisterBackplaneHandler(new OpenTelemetryBackplaneConfigurationHandler());

            // Discover and register extension handlers from DI
            foreach (var handler in sp.GetServices<IConfigurationDecoratorHandler>())
            {
                registry.RegisterDecoratorHandler(handler);
            }

            foreach (var handler in sp.GetServices<IConfigurationSelectionModeHandler>())
            {
                registry.RegisterSelectionModeHandler(handler);
            }

            foreach (var handler in sp.GetServices<IConfigurationBackplaneHandler>())
            {
                registry.RegisterBackplaneHandler(handler);
            }

            return registry;
        });

        return services;
    }

    /// <summary>
    /// Registers a custom decorator handler for configuration files.
    /// Call this before AddExperimentFrameworkFromConfiguration.
    /// </summary>
    /// <typeparam name="THandler">The decorator handler type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConfigurationDecoratorHandler<THandler>(this IServiceCollection services)
        where THandler : class, IConfigurationDecoratorHandler
    {
        services.AddSingleton<IConfigurationDecoratorHandler, THandler>();
        return services;
    }

    /// <summary>
    /// Registers a custom decorator handler instance for configuration files.
    /// Call this before AddExperimentFrameworkFromConfiguration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="handler">The handler instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConfigurationDecoratorHandler(
        this IServiceCollection services,
        IConfigurationDecoratorHandler handler)
    {
        services.AddSingleton(handler);
        return services;
    }

    /// <summary>
    /// Registers a custom selection mode handler for configuration files.
    /// Call this before AddExperimentFrameworkFromConfiguration.
    /// </summary>
    /// <typeparam name="THandler">The selection mode handler type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConfigurationSelectionModeHandler<THandler>(this IServiceCollection services)
        where THandler : class, IConfigurationSelectionModeHandler
    {
        services.AddSingleton<IConfigurationSelectionModeHandler, THandler>();
        return services;
    }

    /// <summary>
    /// Registers a custom selection mode handler instance for configuration files.
    /// Call this before AddExperimentFrameworkFromConfiguration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="handler">The handler instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConfigurationSelectionModeHandler(
        this IServiceCollection services,
        IConfigurationSelectionModeHandler handler)
    {
        services.AddSingleton(handler);
        return services;
    }

    /// <summary>
    /// Registers a custom backplane handler for configuration files.
    /// Call this before AddExperimentFrameworkFromConfiguration.
    /// </summary>
    /// <typeparam name="THandler">The backplane handler type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConfigurationBackplaneHandler<THandler>(this IServiceCollection services)
        where THandler : class, IConfigurationBackplaneHandler
    {
        services.AddSingleton<IConfigurationBackplaneHandler, THandler>();
        return services;
    }

    /// <summary>
    /// Registers a custom backplane handler instance for configuration files.
    /// Call this before AddExperimentFrameworkFromConfiguration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="handler">The handler instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConfigurationBackplaneHandler(
        this IServiceCollection services,
        IConfigurationBackplaneHandler handler)
    {
        services.AddSingleton(handler);
        return services;
    }
}
