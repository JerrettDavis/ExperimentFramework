using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace ExperimentFramework.Distributed.Redis;

/// <summary>
/// Extension methods for registering Redis-based distributed experiment services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Redis-based distributed state and locking.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <param name="configure">Optional configuration for Redis state options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddExperimentDistributedRedis(
        this IServiceCollection services,
        string connectionString,
        Action<RedisDistributedStateOptions>? configure = null)
    {
        services.TryAddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(connectionString));

        var options = new RedisDistributedStateOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);

        services.TryAddSingleton<IDistributedExperimentState, RedisDistributedState>();
        services.TryAddSingleton<IDistributedLockProvider, RedisDistributedLockProvider>();

        return services;
    }

    /// <summary>
    /// Adds Redis-based distributed state and locking using an existing connection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for Redis state options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// Requires <see cref="IConnectionMultiplexer"/> to be already registered.
    /// </remarks>
    public static IServiceCollection AddExperimentDistributedRedis(
        this IServiceCollection services,
        Action<RedisDistributedStateOptions>? configure = null)
    {
        var options = new RedisDistributedStateOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);

        services.TryAddSingleton<IDistributedExperimentState, RedisDistributedState>();
        services.TryAddSingleton<IDistributedLockProvider, RedisDistributedLockProvider>();

        return services;
    }
}
