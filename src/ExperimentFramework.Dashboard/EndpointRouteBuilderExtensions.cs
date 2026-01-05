using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ExperimentFramework.Dashboard.Api;

namespace ExperimentFramework.Dashboard;

/// <summary>
/// Extension methods for mapping the experiment dashboard.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Adds the dashboard middleware to the application pipeline.
    /// This should be called before MapExperimentDashboard.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseExperimentDashboard(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Get DashboardOptions from DI
        var options = app.ApplicationServices.GetService<DashboardOptions>()
            ?? throw new InvalidOperationException("DashboardOptions not found in DI. Did you call AddExperimentDashboard?");

        app.UseMiddleware<DashboardMiddleware>(options);
        return app;
    }

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

        // Get the DashboardOptions from DI
        DashboardOptions? options = null;

        if (endpoints is IApplicationBuilder appBuilder)
        {
            options = appBuilder.ApplicationServices.GetService<DashboardOptions>();
        }

        // If no options found, create default one
        if (options == null)
        {
            options = new DashboardOptions { PathBase = pathPrefix };
        }
        else
        {
            // Update path base
            options.PathBase = pathPrefix;
        }

        // Apply any additional configuration
        configure?.Invoke(options);

        // Create route group for dashboard APIs
        var group = endpoints.MapGroup(pathPrefix);

        // Map all dashboard API endpoints
        group.MapDashboardApi("/api");

        return group;
    }
}
