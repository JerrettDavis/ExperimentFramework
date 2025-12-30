using ExperimentFramework.DataPlane.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExperimentFramework.DataPlane.Kafka;

/// <summary>
/// Extension methods for registering Kafka data backplane services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a Kafka data backplane with the specified configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for Kafka options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKafkaDataBackplane(
        this IServiceCollection services,
        Action<KafkaDataBackplaneOptions> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        services.Configure(configure);
        services.TryAddSingleton<IDataBackplane, KafkaDataBackplane>();

        return services;
    }

    /// <summary>
    /// Adds a Kafka data backplane with options from configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Kafka backplane options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddKafkaDataBackplane(
        this IServiceCollection services,
        KafkaDataBackplaneOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        services.Configure<KafkaDataBackplaneOptions>(opts =>
        {
            opts.Brokers = options.Brokers;
            opts.Topic = options.Topic;
            opts.PartitionStrategy = options.PartitionStrategy;
            opts.BatchSize = options.BatchSize;
            opts.LingerMs = options.LingerMs;
            opts.EnableIdempotence = options.EnableIdempotence;
            opts.CompressionType = options.CompressionType;
            opts.Acks = options.Acks;
            opts.RequestTimeoutMs = options.RequestTimeoutMs;
            opts.MaxInFlight = options.MaxInFlight;
            opts.ClientId = options.ClientId;
            opts.AdditionalConfig = options.AdditionalConfig;
        });

        services.TryAddSingleton<IDataBackplane, KafkaDataBackplane>();

        return services;
    }
}
