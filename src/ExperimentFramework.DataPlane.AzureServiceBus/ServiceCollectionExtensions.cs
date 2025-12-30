using ExperimentFramework.DataPlane.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExperimentFramework.DataPlane.AzureServiceBus;

/// <summary>
/// Extension methods for registering Azure Service Bus data backplane services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds an Azure Service Bus data backplane with the specified configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action for Azure Service Bus options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAzureServiceBusDataBackplane(
        this IServiceCollection services,
        Action<AzureServiceBusDataBackplaneOptions> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        services.Configure(configure);
        services.TryAddSingleton<IDataBackplane, AzureServiceBusDataBackplane>();

        return services;
    }

    /// <summary>
    /// Adds an Azure Service Bus data backplane with options from configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Azure Service Bus backplane options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAzureServiceBusDataBackplane(
        this IServiceCollection services,
        AzureServiceBusDataBackplaneOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        services.Configure<AzureServiceBusDataBackplaneOptions>(opts =>
        {
            opts.ConnectionString = options.ConnectionString;
            opts.QueueName = options.QueueName;
            opts.TopicName = options.TopicName;
            opts.UseTypeSpecificDestinations = options.UseTypeSpecificDestinations;
            opts.MessageTimeToLiveMinutes = options.MessageTimeToLiveMinutes;
            opts.MaxRetryAttempts = options.MaxRetryAttempts;
            opts.BatchSize = options.BatchSize;
            opts.EnableSessions = options.EnableSessions;
            opts.SessionStrategy = options.SessionStrategy;
            opts.ClientId = options.ClientId;
        });

        services.TryAddSingleton<IDataBackplane, AzureServiceBusDataBackplane>();

        return services;
    }
}
