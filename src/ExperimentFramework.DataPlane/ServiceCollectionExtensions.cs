using ExperimentFramework.DataPlane.Abstractions;
using ExperimentFramework.DataPlane.Abstractions.Configuration;
using ExperimentFramework.DataPlane.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExperimentFramework.DataPlane;

/// <summary>
/// Extension methods for registering data backplane services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds data backplane services with default options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDataBackplane(
        this IServiceCollection services,
        Action<DataPlaneOptions>? configure = null)
    {
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<DataPlaneOptions>(options => { });
        }

        return services;
    }

    /// <summary>
    /// Adds an in-memory data backplane.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryDataBackplane(this IServiceCollection services)
    {
        services.TryAddSingleton<IDataBackplane, InMemoryDataBackplane>();
        return services;
    }

    /// <summary>
    /// Adds a logging data backplane.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLoggingDataBackplane(this IServiceCollection services)
    {
        services.TryAddSingleton<IDataBackplane, LoggingDataBackplane>();
        return services;
    }

    /// <summary>
    /// Adds an OpenTelemetry data backplane.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This backplane emits events as OpenTelemetry Activities (spans) and structured logs.
    /// Configure an OpenTelemetry SDK with appropriate exporters to collect these events.
    /// Activity source name: "ExperimentFramework.DataPlane"
    /// </remarks>
    public static IServiceCollection AddOpenTelemetryDataBackplane(this IServiceCollection services)
    {
        services.TryAddSingleton<IDataBackplane, OpenTelemetryDataBackplane>();
        return services;
    }

    /// <summary>
    /// Adds a composite data backplane with multiple implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="backplaneFactories">Factories to create backplane instances.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCompositeDataBackplane(
        this IServiceCollection services,
        params Func<IServiceProvider, IDataBackplane>[] backplaneFactories)
    {
        if (backplaneFactories == null || backplaneFactories.Length == 0)
        {
            throw new ArgumentException("At least one backplane factory must be provided", nameof(backplaneFactories));
        }

        services.TryAddSingleton<IDataBackplane>(sp =>
        {
            var backplanes = backplaneFactories.Select(factory => factory(sp)).ToList();
            return new CompositeDataBackplane(backplanes);
        });

        return services;
    }
}
