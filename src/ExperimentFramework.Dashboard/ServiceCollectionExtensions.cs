using ExperimentFramework.Dashboard.Abstractions;
using ExperimentFramework.Dashboard.Authorization;
using ExperimentFramework.Dashboard.Data;
using ExperimentFramework.Dashboard.Theming;
using ExperimentFramework.Dashboard.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExperimentFramework.Dashboard;

/// <summary>
/// Extension methods for registering dashboard services with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the experiment dashboard services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExperimentDashboard(
        this IServiceCollection services,
        Action<DashboardOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register default implementations
        services.TryAddSingleton<IAuthorizationProvider, ClaimsPrincipalAuthProvider>();
        services.TryAddSingleton<IDashboardDataProvider, DefaultDashboardDataProvider>();
        services.TryAddSingleton<IDashboardThemeProvider, DefaultThemeProvider>();

        // Register UI services
        services.TryAddScoped<DashboardStateService>();
        services.TryAddScoped<ThemeService>();
        services.TryAddScoped<ExperimentCodeGenerator>();

        // Register dashboard options
        if (configure != null)
        {
            services.Configure(configure);
        }

        return services;
    }
}
