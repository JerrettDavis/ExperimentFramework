using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExperimentFramework.Distributed;

/// <summary>
/// Extension methods for registering distributed experiment services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds in-memory distributed state and locking for single-instance deployments.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// This is suitable for development and single-instance deployments.
    /// For multi-instance deployments, use <c>AddExperimentDistributedRedis</c> or similar.
    /// </remarks>
    public static IServiceCollection AddExperimentDistributedInMemory(this IServiceCollection services)
    {
        services.TryAddSingleton<IDistributedExperimentState, InMemoryDistributedState>();
        services.TryAddSingleton<IDistributedLockProvider, InMemoryDistributedLockProvider>();
        return services;
    }

    /// <summary>
    /// Adds distributed state with a custom implementation.
    /// </summary>
    /// <typeparam name="TState">The state implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddExperimentDistributedState<TState>(this IServiceCollection services)
        where TState : class, IDistributedExperimentState
    {
        services.TryAddSingleton<IDistributedExperimentState, TState>();
        return services;
    }

    /// <summary>
    /// Adds distributed locking with a custom implementation.
    /// </summary>
    /// <typeparam name="TLockProvider">The lock provider implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddExperimentDistributedLocking<TLockProvider>(this IServiceCollection services)
        where TLockProvider : class, IDistributedLockProvider
    {
        services.TryAddSingleton<IDistributedLockProvider, TLockProvider>();
        return services;
    }
}
