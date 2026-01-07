using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ExperimentFramework.Dashboard.Abstractions;

namespace ExperimentFramework.Dashboard.Api.Endpoints;

/// <summary>
/// Provides minimal API endpoints for analytics and statistical analysis.
/// </summary>
public static class AnalyticsEndpoints
{
    /// <summary>
    /// Maps analytics endpoints to the specified route group.
    /// </summary>
    public static RouteGroupBuilder MapAnalyticsEndpoints(
        this IEndpointRouteBuilder endpoints,
        string prefix = "/api/analytics")
    {
        var group = endpoints.MapGroup(prefix)
            .WithTags("Analytics");

        group.MapGet("/{experimentName}/statistics", GetStatistics)
            .WithName("Dashboard_GetStatistics");

        group.MapPost("/{experimentName}/compare", CompareVariants)
            .WithName("Dashboard_CompareVariants");

        group.MapGet("/{experimentName}/export/{format}", ExportData)
            .WithName("Dashboard_ExportAnalytics");

        return group;
    }

    private static async Task<IResult> GetStatistics(
        string experimentName,
        HttpContext context,
        IServiceProvider sp)
    {
        var analyticsProvider = sp.GetService<IAnalyticsProvider>();
        if (analyticsProvider == null)
        {
            return Results.Ok(new
            {
                experimentName,
                message = "Analytics provider not available (Phase 5)"
            });
        }

        // TODO: Implement statistical analysis in Phase 5
        var stats = new
        {
            experimentName,
            variants = new[]
            {
                new { name = "control", conversions = 0, samples = 0 },
                new { name = "variant-a", conversions = 0, samples = 0 }
            },
            message = "Statistical analysis implementation pending (Phase 5)"
        };

        return Results.Ok(stats);
    }

    private static IResult CompareVariants(
        string experimentName,
        IServiceProvider sp,
        CompareVariantsRequest request)
    {
        // TODO: Implement variant comparison in Phase 5
        return Results.Ok(new
        {
            experimentName,
            variants = request.Variants,
            message = "Variant comparison implementation pending (Phase 5)"
        });
    }

    private static IResult ExportData(
        string experimentName,
        string format,
        IServiceProvider sp)
    {
        if (format.ToLower() != "csv" && format.ToLower() != "json")
        {
            return Results.BadRequest(new { error = "Format must be 'csv' or 'json'" });
        }

        // TODO: Implement data export in Phase 5
        return Results.Ok(new
        {
            experimentName,
            format,
            message = "Data export implementation pending (Phase 5)"
        });
    }
}

public record CompareVariantsRequest(string[] Variants);
