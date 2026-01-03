using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace ExperimentFramework.Governance.Persistence.Redis;

/// <summary>
/// Extension methods for registering Redis governance persistence.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Redis-based governance persistence with the specified connection string.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <param name="keyPrefix">Optional key prefix for all Redis keys (default: "governance:").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRedisGovernancePersistence(
        this IServiceCollection services,
        string connectionString,
        string keyPrefix = "governance:")
    {
        // Only add ConnectionMultiplexer if not already registered
        if (!services.Any(x => x.ServiceType == typeof(IConnectionMultiplexer)))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(connectionString));
        }

        services.TryAddSingleton<IGovernancePersistenceBackplane>(sp =>
        {
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RedisGovernancePersistenceBackplane>>();
            return new RedisGovernancePersistenceBackplane(redis, logger, keyPrefix);
        });

        return services;
    }

    /// <summary>
    /// Adds Redis-based governance persistence with an existing IConnectionMultiplexer.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="keyPrefix">Optional key prefix for all Redis keys (default: "governance:").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRedisGovernancePersistence(
        this IServiceCollection services,
        string keyPrefix = "governance:")
    {
        services.TryAddSingleton<IGovernancePersistenceBackplane>(sp =>
        {
            var redis = sp.GetRequiredService<IConnectionMultiplexer>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RedisGovernancePersistenceBackplane>>();
            return new RedisGovernancePersistenceBackplane(redis, logger, keyPrefix);
        });

        return services;
    }
}
