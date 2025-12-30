using ExperimentFramework.DataPlane.Abstractions;
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
    /// Adds a composite data backplane with multiple implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure the backplanes.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCompositeDataBackplane(
        this IServiceCollection services,
        Action<IServiceCollection> configure)
    {
        var backplaneServices = new ServiceCollection();
        configure(backplaneServices);

        services.TryAddSingleton<IDataBackplane>(sp =>
        {
            var backplaneProvider = backplaneServices.BuildServiceProvider();
            var backplanes = backplaneProvider.GetServices<IDataBackplane>();
            return new CompositeDataBackplane(backplanes);
        });

        return services;
    }
}
