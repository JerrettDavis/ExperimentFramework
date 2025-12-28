using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Audit;

/// <summary>
/// Extension methods for registering experiment audit services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds experiment audit logging to the logging infrastructure.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="logLevel">The log level for audit events.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddExperimentAuditLogging(
        this IServiceCollection services,
        LogLevel logLevel = LogLevel.Information)
    {
        services.TryAddSingleton<IAuditSink>(sp =>
            new LoggingAuditSink(
                sp.GetRequiredService<ILogger<LoggingAuditSink>>(),
                logLevel));

        return services;
    }

    /// <summary>
    /// Adds a custom audit sink.
    /// </summary>
    /// <typeparam name="TSink">The audit sink type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddExperimentAuditSink<TSink>(this IServiceCollection services)
        where TSink : class, IAuditSink
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuditSink, TSink>());
        return services;
    }

    /// <summary>
    /// Adds composite audit support that aggregates all registered sinks.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddExperimentAuditComposite(this IServiceCollection services)
    {
        services.TryAddSingleton<IAuditSink>(sp =>
        {
            var sinks = sp.GetServices<IAuditSink>()
                .Where(s => s is not CompositeAuditSink)
                .ToList();

            return sinks.Count == 1 ? sinks[0] : new CompositeAuditSink(sinks);
        });

        return services;
    }
}
