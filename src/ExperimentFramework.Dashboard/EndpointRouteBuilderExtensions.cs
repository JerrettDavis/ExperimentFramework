using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ExperimentFramework.Dashboard.Api;

namespace ExperimentFramework.Dashboard;

/// <summary>
/// Extension methods for mapping the experiment dashboard.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the experiment dashboard to the specified path prefix.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pathPrefix">The path prefix for the dashboard (default: "/dashboard").</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>A route group builder for further configuration.</returns>
    public static RouteGroupBuilder MapExperimentDashboard(
        this IEndpointRouteBuilder endpoints,
        string pathPrefix = "/dashboard",
        Action<DashboardOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Get the DashboardOptions from DI (if registered), otherwise create new one
        DashboardOptions? options = null;

        if (endpoints is IApplicationBuilder appBuilder)
        {
            // Try to get options from DI
            var services = appBuilder.ApplicationServices;
            options = services.GetService(typeof(DashboardOptions)) as DashboardOptions;
        }

        // If no options in DI, create new one
        if (options == null)
        {
            options = new DashboardOptions { PathBase = pathPrefix };
        }
        else
        {
            // Update path base if different
            options.PathBase = pathPrefix;
        }

        // Apply any additional configuration
        configure?.Invoke(options);

        // Register middleware (gets the IApplicationBuilder from endpoints)
        if (endpoints is IApplicationBuilder app)
        {
            app.UseMiddleware<DashboardMiddleware>(options);
        }

        // Create route group for dashboard APIs
        var group = endpoints.MapGroup(pathPrefix);

        // Map all dashboard API endpoints
        group.MapDashboardApi("/api");

        return group;
    }
}
