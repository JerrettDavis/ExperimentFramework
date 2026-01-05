using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
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

        // Create dashboard options
        var options = new DashboardOptions { PathBase = pathPrefix };
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
