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
            .WithName("Dashboard_GetAuditLog");

        return group;
    }

    private static async Task<IResult> GetAuditLog(
        int limit = 50,
        IServiceProvider? sp = null,
        CancellationToken ct = default)
    {
        var analyticsProvider = sp?.GetService<IAnalyticsProvider>();
        if (analyticsProvider == null)
        {
            // Return empty audit log when no analytics provider is configured
            return Results.Ok(Array.Empty<object>());
        }

        try
        {
            var registry = sp?.GetService<IExperimentRegistry>();
            var experiments = registry?.GetAllExperiments().ToList() ?? [];

            var auditEntries = new List<object>();

            foreach (var exp in experiments.Take(10))
            {
                try
                {
                    var assignments = await analyticsProvider.GetAssignmentsAsync(exp.Name, cancellationToken: ct);
                    foreach (var assignment in assignments.Take(limit / experiments.Count + 1))
                    {
                        auditEntries.Add(new
                        {
                            timestamp = assignment.Timestamp,
                            eventType = "assignment",
                            experimentName = exp.Name,
                            details = $"User {assignment.SubjectId} assigned to {assignment.TrialKey}"
                        });
                    }
                }
                catch
                {
                    // Skip experiments that fail
                }
            }

            // Return sorted by timestamp descending, limited
            var sorted = auditEntries
                .OrderByDescending(e => ((dynamic)e).timestamp)
                .Take(limit)
                .ToList();

            return Results.Ok(sorted);
        }
        catch
        {
            return Results.Ok(Array.Empty<object>());
        }
    }
}
