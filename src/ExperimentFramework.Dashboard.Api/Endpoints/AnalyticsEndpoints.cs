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
        IServiceProvider sp,
        CancellationToken ct)
    {
        var analyticsProvider = sp.GetService<IAnalyticsProvider>();
        if (analyticsProvider == null)
        {
            return Results.StatusCode(501);
        }

        var tenantId = GetTenantId(context);

        var assignments = await analyticsProvider.GetAssignmentsAsync(experimentName, tenantId, cancellationToken: ct);
        var exposures = await analyticsProvider.GetExposuresAsync(experimentName, tenantId, cancellationToken: ct);
        var signals = await analyticsProvider.GetAnalysisSignalsAsync(experimentName, tenantId, cancellationToken: ct);

        var assignmentsByVariant = assignments.GroupBy(a => a.TrialKey)
            .ToDictionary(g => g.Key, g => g.Count());

        var exposuresByVariant = exposures.GroupBy(e => e.TrialKey)
            .ToDictionary(g => g.Key, g => g.Count());

        var signalsByVariant = signals.GroupBy(s => s.TrialKey)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(s => s.MetricName)
                      .ToDictionary(
                          mg => mg.Key,
                          mg => new
                          {
                              count = mg.Count(),
                              sum = mg.Sum(s => s.Value),
                              mean = mg.Average(s => s.Value),
                              min = mg.Min(s => s.Value),
                              max = mg.Max(s => s.Value)
                          }));

        var allVariants = assignmentsByVariant.Keys
            .Union(exposuresByVariant.Keys)
            .Union(signalsByVariant.Keys)
            .Distinct();

        var variantStats = allVariants.Select(variant => new
        {
            variant,
            assignments = assignmentsByVariant.GetValueOrDefault(variant, 0),
            exposures = exposuresByVariant.GetValueOrDefault(variant, 0),
            metrics = signalsByVariant.TryGetValue(variant, out var m) ? m : null
        });

        return Results.Ok(new
        {
            experimentName,
            variants = variantStats
        });
    }

    private static async Task<IResult> CompareVariants(
        string experimentName,
        IServiceProvider sp,
        CompareVariantsRequest request,
        CancellationToken ct)
    {
        var analyticsProvider = sp.GetService<IAnalyticsProvider>();
        if (analyticsProvider == null)
        {
            return Results.StatusCode(501);
        }

        if (request.Variants == null || request.Variants.Length < 2)
        {
            return Results.BadRequest(new { error = "At least two variants are required for comparison" });
        }

        var signals = await analyticsProvider.GetAnalysisSignalsAsync(experimentName, cancellationToken: ct);
        var relevant = signals.Where(s => request.Variants.Contains(s.TrialKey)).ToList();

        var comparison = request.Variants.Select(variant =>
        {
            var variantSignals = relevant.Where(s => s.TrialKey == variant).ToList();
            return new
            {
                variant,
                sampleSize = variantSignals.Select(s => s.SubjectId).Distinct().Count(),
                metrics = variantSignals
                    .GroupBy(s => s.MetricName)
                    .ToDictionary(
                        g => g.Key,
                        g => new
                        {
                            mean = g.Average(s => s.Value),
                            count = g.Count()
                        })
            };
        });

        return Results.Ok(new
        {
            experimentName,
            variants = comparison
        });
    }

    private static async Task<IResult> ExportData(
        string experimentName,
        string format,
        IServiceProvider sp,
        CancellationToken ct)
    {
        var normalizedFormat = format.ToLowerInvariant();
        if (normalizedFormat != "csv" && normalizedFormat != "json")
        {
            return Results.BadRequest(new { error = "Format must be 'csv' or 'json'" });
        }

        var analyticsProvider = sp.GetService<IAnalyticsProvider>();
        if (analyticsProvider == null)
        {
            return Results.StatusCode(501);
        }

        var assignments = await analyticsProvider.GetAssignmentsAsync(experimentName, cancellationToken: ct);
        var signals = await analyticsProvider.GetAnalysisSignalsAsync(experimentName, cancellationToken: ct);

        if (normalizedFormat == "json")
        {
            return Results.Ok(new
            {
                experimentName,
                exportedAt = DateTimeOffset.UtcNow,
                assignments,
                signals
            });
        }

        // CSV format
        var rows = new System.Text.StringBuilder();
        rows.AppendLine("type,experimentName,subjectId,trialKey,metricName,value,timestamp,tenantId");

        foreach (var a in assignments)
        {
            rows.AppendLine($"assignment,{CsvEscape(a.ExperimentName)},{CsvEscape(a.SubjectId)},{CsvEscape(a.TrialKey)},,, {a.Timestamp:O},{CsvEscape(a.TenantId)}");
        }

        foreach (var s in signals)
        {
            rows.AppendLine($"signal,{CsvEscape(s.ExperimentName)},{CsvEscape(s.SubjectId)},{CsvEscape(s.TrialKey)},{CsvEscape(s.MetricName)},{s.Value},{s.Timestamp:O},{CsvEscape(s.TenantId)}");
        }

        return Results.Text(rows.ToString(), "text/csv");
    }

    private static string? GetTenantId(HttpContext context)
    {
        if (context.Items.TryGetValue("TenantContext", out var obj) && obj is TenantContext tc)
            return tc.TenantId;
        return null;
    }

    private static string CsvEscape(string? value)
    {
        if (value == null) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}

/// <summary>Request to compare analytics across multiple experiment variants.</summary>
/// <param name="Variants">The variant keys to compare.</param>
public record CompareVariantsRequest(string[] Variants);
