using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExperimentFramework.Governance.Persistence;

/// <summary>
/// Extension methods for registering governance persistence services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds in-memory governance persistence backplane.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryGovernancePersistence(this IServiceCollection services)
    {
        services.TryAddSingleton<IGovernancePersistenceBackplane, InMemoryGovernancePersistenceBackplane>();
        return services;
    }

    /// <summary>
    /// Adds a custom governance persistence backplane.
    /// </summary>
    /// <typeparam name="TImplementation">The backplane implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGovernancePersistence<TImplementation>(this IServiceCollection services)
        where TImplementation : class, IGovernancePersistenceBackplane
    {
        services.TryAddSingleton<IGovernancePersistenceBackplane, TImplementation>();
        return services;
    }

    /// <summary>
    /// Adds a custom governance persistence backplane with factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="implementationFactory">The factory to create the backplane.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddGovernancePersistence(
        this IServiceCollection services,
        Func<IServiceProvider, IGovernancePersistenceBackplane> implementationFactory)
    {
        services.TryAddSingleton(implementationFactory);
        return services;
    }
}
