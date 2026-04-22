using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ExperimentFramework.Dashboard.Abstractions;

namespace ExperimentFramework.Dashboard.Api.Endpoints;

/// <summary>
/// Provides minimal API endpoints for targeting rule management.
/// </summary>
public static class TargetingEndpoints
{
    /// <summary>
    /// Maps targeting endpoints to the specified route group.
    /// </summary>
    public static RouteGroupBuilder MapTargetingEndpoints(
        this IEndpointRouteBuilder endpoints,
        string prefix = "/api/targeting")
    {
        var group = endpoints.MapGroup(prefix)
            .WithTags("Targeting");

        group.MapGet("/{experimentName}/rules", GetTargetingRules)
            .WithName("Dashboard_GetTargetingRules");

        group.MapPost("/{experimentName}/rules", UpdateTargetingRules)
            .WithName("Dashboard_UpdateTargetingRules");

        group.MapPost("/{experimentName}/evaluate", EvaluateTargeting)
            .WithName("Dashboard_EvaluateTargeting");

        return group;
    }

    private static async Task<IResult> GetTargetingRules(
        string experimentName,
        IServiceProvider sp,
        CancellationToken ct)
    {
        var targeting = sp.GetService<ITargetingManagementService>();
        if (targeting == null)
        {
            return Results.StatusCode(501);
        }

        var rules = await targeting.GetRulesAsync(experimentName, ct);
        if (rules == null)
        {
            return Results.NotFound(new { error = $"No targeting rules found for experiment '{experimentName}'" });
        }

        return Results.Ok(new { experimentName, rules });
    }

    private static async Task<IResult> UpdateTargetingRules(
        string experimentName,
        IServiceProvider sp,
        UpdateTargetingRequest request,
        CancellationToken ct)
    {
        var targeting = sp.GetService<ITargetingManagementService>();
        if (targeting == null)
        {
            return Results.StatusCode(501);
        }

        if (request.Rules == null)
        {
            return Results.BadRequest(new { error = "Rules must be provided" });
        }

        var ruleDtos = request.Rules
            .Select((r, i) =>
            {
                var dict = r as IDictionary<string, object>
                    ?? (r != null
                        ? r.GetType().GetProperties()
                            .ToDictionary(p => p.Name, p => p.GetValue(r)!)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                        : new Dictionary<string, object>());

                return new TargetingRuleDto
                {
                    Id = dict.TryGetValue("id", out var id) ? id?.ToString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
                    Type = dict.TryGetValue("type", out var type) ? type?.ToString() ?? "unknown" : "unknown",
                    VariantKey = dict.TryGetValue("variantKey", out var vk) ? vk?.ToString() ?? "" : (dict.TryGetValue("variant", out var v) ? v?.ToString() ?? "" : ""),
                    Enabled = dict.TryGetValue("enabled", out var enabled) && enabled is bool b ? b : true,
                    Parameters = dict
                        .Where(kvp => kvp.Key != "id" && kvp.Key != "type" && kvp.Key != "variantKey" && kvp.Key != "variant" && kvp.Key != "enabled")
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                };
            })
            .ToList();

        await targeting.SetRulesAsync(experimentName, ruleDtos, ct);

        return Results.Ok(new { experimentName, updatedRuleCount = ruleDtos.Count });
    }

    private static async Task<IResult> EvaluateTargeting(
        string experimentName,
        IServiceProvider sp,
        EvaluateTargetingRequest request,
        CancellationToken ct)
    {
        var targeting = sp.GetService<ITargetingManagementService>();
        if (targeting == null)
        {
            return Results.StatusCode(501);
        }

        if (request.Context == null)
        {
            return Results.BadRequest(new { error = "Context must be provided" });
        }

        var result = await targeting.EvaluateAsync(experimentName, request.Context, ct);

        return Results.Ok(new
        {
            experimentName,
            matched = result.Matched,
            matchedVariant = result.MatchedVariant,
            matchedRuleId = result.MatchedRuleId
        });
    }
}

/// <summary>Request to update targeting rules for an experiment.</summary>
/// <param name="Rules">The targeting rules to apply.</param>
public record UpdateTargetingRequest(object[] Rules);

/// <summary>Request to evaluate targeting rules against a context.</summary>
/// <param name="Context">The evaluation context attributes.</param>
public record EvaluateTargetingRequest(Dictionary<string, object> Context);
