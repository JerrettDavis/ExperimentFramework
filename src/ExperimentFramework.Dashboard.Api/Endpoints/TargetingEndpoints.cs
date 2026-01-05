using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

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

    private static IResult GetTargetingRules(string experimentName, IServiceProvider sp)
    {
        // TODO: Implement targeting rule retrieval
        var rules = new[]
        {
            new
            {
                id = "rule-1",
                condition = "user.country == 'US'",
                variant = "variant-a",
                enabled = true
            }
        };

        return Results.Ok(new
        {
            experimentName,
            rules,
            message = "Targeting rules implementation pending"
        });
    }

    private static IResult UpdateTargetingRules(
        string experimentName,
        IServiceProvider sp,
        UpdateTargetingRequest request)
    {
        // TODO: Implement targeting rule update
        return Results.Ok(new
        {
            experimentName,
            message = "Targeting rules updated (implementation pending)"
        });
    }

    private static IResult EvaluateTargeting(
        string experimentName,
        IServiceProvider sp,
        EvaluateTargetingRequest request)
    {
        // TODO: Implement targeting evaluation
        return Results.Ok(new
        {
            experimentName,
            matched = true,
            variant = "variant-a",
            message = "Targeting evaluation (implementation pending)"
        });
    }
}

public record UpdateTargetingRequest(object[] Rules);
public record EvaluateTargetingRequest(Dictionary<string, object> Context);
