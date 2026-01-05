using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using ExperimentFramework.Dashboard.Api.Endpoints;

namespace ExperimentFramework.Dashboard.Api;

/// <summary>
/// Extension methods for registering all dashboard API endpoints.
/// </summary>
public static class DashboardApiExtensions
{
    /// <summary>
    /// Maps all dashboard API endpoints to the application.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pathPrefix">Optional path prefix for all dashboard APIs (default: "/dashboard-api").</param>
    /// <returns>A route group builder for further configuration.</returns>
    public static RouteGroupBuilder MapDashboardApi(
        this IEndpointRouteBuilder endpoints,
        string pathPrefix = "/dashboard-api")
    {
        var group = endpoints.MapGroup(pathPrefix);

        // Map all endpoint groups
        group.MapExperimentEndpoints("/experiments");
        group.MapConfigurationEndpoints("/configuration");
        group.MapPluginEndpoints("/plugins");
        group.MapRolloutEndpoints("/rollout");
        group.MapTargetingEndpoints("/targeting");
        group.MapAnalyticsEndpoints("/analytics");
        group.MapGovernanceEndpoints("/governance");

        return group;
    }
}
