using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Diagnostics;

/// <summary>
/// Extension methods for configuring experiment event sinks.
/// </summary>
public static class ExperimentDiagnosticsExtensions
{
    /// <summary>
    /// Adds an experiment event sink to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="sink">The sink to add.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExperimentEventSink(
        this IServiceCollection services,
        IExperimentEventSink sink)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (sink == null)
            throw new ArgumentNullException(nameof(sink));

        services.AddSingleton(sink);
        return services;
    }

    /// <summary>
    /// Adds an experiment event sink to the service collection using a factory.
    /// </summary>
    /// <typeparam name="TSink">The sink type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="implementationFactory">The factory function to create the sink.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExperimentEventSink<TSink>(
        this IServiceCollection services,
        Func<IServiceProvider, TSink> implementationFactory)
        where TSink : class, IExperimentEventSink
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (implementationFactory == null)
            throw new ArgumentNullException(nameof(implementationFactory));

        services.AddSingleton<IExperimentEventSink>(implementationFactory);
        return services;
    }

    /// <summary>
    /// Adds an experiment event sink of the specified type.
    /// </summary>
    /// <typeparam name="TSink">The sink type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExperimentEventSink<TSink>(
        this IServiceCollection services)
        where TSink : class, IExperimentEventSink
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        services.AddSingleton<IExperimentEventSink, TSink>();
        return services;
    }

    /// <summary>
    /// Adds an in-memory event sink for testing and diagnostics.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="maxCapacity">Optional maximum capacity (null for unbounded).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryExperimentEventSink(
        this IServiceCollection services,
        int? maxCapacity = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        var sink = maxCapacity.HasValue
            ? new InMemoryExperimentEventSink(maxCapacity.Value)
            : new InMemoryExperimentEventSink();

        services.AddSingleton<IExperimentEventSink>(sink);
        services.AddSingleton(sink); // Also register concrete type for direct access
        return services;
    }

    /// <summary>
    /// Adds a logger-based event sink.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="categoryName">Optional logger category name (defaults to "ExperimentFramework.Diagnostics").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLoggerExperimentEventSink(
        this IServiceCollection services,
        string? categoryName = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        services.AddSingleton<IExperimentEventSink>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(categoryName ?? "ExperimentFramework.Diagnostics");
            return new LoggerExperimentEventSink(logger);
        });

        return services;
    }

    /// <summary>
    /// Adds an OpenTelemetry event sink for activities and metrics.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOpenTelemetryExperimentEventSink(
        this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        services.AddSingleton<IExperimentEventSink, OpenTelemetryExperimentEventSink>();
        return services;
    }

    /// <summary>
    /// Gets all registered experiment event sinks as a composite sink.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>A composite sink containing all registered sinks, or null if none are registered.</returns>
    public static IExperimentEventSink? GetExperimentEventSinks(this IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));

        var sinks = serviceProvider.GetServices<IExperimentEventSink>().ToArray();
        
        return sinks.Length switch
        {
            0 => null,
            1 => sinks[0],
            _ => new CompositeExperimentEventSink(sinks)
        };
    }
}
