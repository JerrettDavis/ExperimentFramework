using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Simulation;

/// <summary>
/// Extension methods for adding simulation support to the service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds simulation services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSimulation(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // Currently no services need to be registered
        // This is here for future extensibility
        return services;
    }
}
