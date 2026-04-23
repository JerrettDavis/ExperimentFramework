using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ExperimentFramework.Admin;
using ExperimentFramework.Dashboard.Abstractions;

namespace ExperimentFramework.Dashboard.Api.Endpoints;

/// <summary>
/// Provides minimal API endpoints for audit log access.
/// </summary>
public static class AuditEndpoints
{
    /// <summary>
    /// Maps audit endpoints to the specified route group.
    /// </summary>
    public static RouteGroupBuilder MapAuditEndpoints(
        this IEndpointRouteBuilder endpoints,
        string prefix = "/api/audit")
    {
        var group = endpoints.MapGroup(prefix)
            .WithTags("Audit");

        group.MapGet("/", GetAuditLog)
            .WithName("Dashboard_GetAuditLogSummary");

        return group;
    }

    private static async Task<IResult> GetAuditLog(
        IServiceProvider sp,
        int limit = 50,
        CancellationToken ct = default)
    {
        var analyticsProvider = sp.GetService<IAnalyticsProvider>();
        if (analyticsProvider == null)
        {
            // Return empty audit log when no analytics provider is configured
            return Results.Ok(Array.Empty<object>());
        }

        try
        {
            var registry = sp.GetService<IExperimentRegistry>();
            var experiments = registry?.GetAllExperiments().ToList() ?? [];

            var auditEntries = new List<(DateTimeOffset Timestamp, object Entry)>();

            // Take at most 10 experiments but allow at least one if there are fewer.
            var perExperimentCap = experiments.Count > 0
                ? Math.Max(1, limit / experiments.Count + 1)
                : limit;

            foreach (var exp in experiments.Take(10))
            {
                try
                {
                    var assignments = await analyticsProvider.GetAssignmentsAsync(exp.Name, cancellationToken: ct);
                    foreach (var assignment in assignments.Take(perExperimentCap))
                    {
                        auditEntries.Add((assignment.Timestamp, new
                        {
                            timestamp = assignment.Timestamp,
                            eventType = "assignment",
                            experimentName = exp.Name,
                            details = $"User {assignment.SubjectId} assigned to {assignment.TrialKey}"
                        }));
                    }
                }
                catch
                {
                    // Skip experiments that fail
                }
            }

            var sorted = auditEntries
                .OrderByDescending(e => e.Timestamp)
                .Take(limit)
                .Select(e => e.Entry)
                .ToList();

            return Results.Ok(sorted);
        }
        catch
        {
            return Results.Ok(Array.Empty<object>());
        }
    }
}
