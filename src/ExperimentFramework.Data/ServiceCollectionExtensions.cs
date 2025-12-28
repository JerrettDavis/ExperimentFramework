using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Data.Configuration;
using ExperimentFramework.Data.Recording;
using ExperimentFramework.Data.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExperimentFramework.Data;

/// <summary>
/// Extension methods for registering experiment data collection services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds experiment data collection services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for the outcome store.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This registers:
    /// <list type="bullet">
    /// <item><description><see cref="IOutcomeStore"/> - In-memory store by default</description></item>
    /// <item><description><see cref="IOutcomeRecorder"/> - High-level recording interface</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// To use a different store implementation, register your implementation before calling this method
    /// or use the overload that accepts a store factory.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddExperimentDataCollection(
        this IServiceCollection services,
        Action<OutcomeRecorderOptions>? configure = null)
    {
        var options = new OutcomeRecorderOptions();
        configure?.Invoke(options);

        // Register options
        services.TryAddSingleton(options);

        // Register in-memory store as default (can be overridden)
        services.TryAddSingleton<IOutcomeStore, InMemoryOutcomeStore>();

        // Register recorder
        services.TryAddSingleton<IOutcomeRecorder>(sp =>
        {
            var store = sp.GetRequiredService<IOutcomeStore>();
            var opts = sp.GetRequiredService<OutcomeRecorderOptions>();
            return new OutcomeRecorder(store, opts);
        });

        return services;
    }

    /// <summary>
    /// Adds experiment data collection services with a custom store.
    /// </summary>
    /// <typeparam name="TStore">The store implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for the outcome store.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExperimentDataCollection<TStore>(
        this IServiceCollection services,
        Action<OutcomeRecorderOptions>? configure = null)
        where TStore : class, IOutcomeStore
    {
        var options = new OutcomeRecorderOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<IOutcomeStore, TStore>();

        services.TryAddSingleton<IOutcomeRecorder>(sp =>
        {
            var store = sp.GetRequiredService<IOutcomeStore>();
            var opts = sp.GetRequiredService<OutcomeRecorderOptions>();
            return new OutcomeRecorder(store, opts);
        });

        return services;
    }

    /// <summary>
    /// Adds experiment data collection with a no-op store (zero overhead).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Use this when you want outcome collection infrastructure available but disabled.
    /// All recorded outcomes will be discarded.
    /// </remarks>
    public static IServiceCollection AddExperimentDataCollectionNoop(this IServiceCollection services)
    {
        services.TryAddSingleton<OutcomeRecorderOptions>();
        services.TryAddSingleton<IOutcomeStore>(NoopOutcomeStore.Instance);
        services.TryAddSingleton<IOutcomeRecorder>(sp =>
        {
            var store = sp.GetRequiredService<IOutcomeStore>();
            return new OutcomeRecorder(store);
        });

        return services;
    }

    /// <summary>
    /// Adds data collection configuration handlers to the experiment framework.
    /// This enables the 'outcomeCollection' decorator type in YAML/JSON configuration files.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddExperimentDataConfiguration();
    /// services.AddExperimentDataCollection();
    /// services.AddExperimentFrameworkFromConfiguration(configuration);
    /// </code>
    ///
    /// Configuration file example:
    /// <code>
    /// experimentFramework:
    ///   decorators:
    ///     - type: outcomeCollection
    ///       options:
    ///         collectDuration: true
    ///         collectErrors: true
    ///         enableBatching: true
    ///         maxBatchSize: 100
    /// </code>
    /// </example>
    public static IServiceCollection AddExperimentDataConfiguration(this IServiceCollection services)
    {
        // Register the outcome collection handler with the configuration system
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigurationDecoratorHandler, OutcomeCollectionDecoratorHandler>());

        return services;
    }
}
