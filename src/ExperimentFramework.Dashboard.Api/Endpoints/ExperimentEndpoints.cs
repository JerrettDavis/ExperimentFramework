using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ExperimentFramework;
using ExperimentFramework.Admin;
using ExperimentFramework.Dashboard.Abstractions;

namespace ExperimentFramework.Dashboard.Api.Endpoints;

/// <summary>
/// Provides minimal API endpoints for experiment management.
/// </summary>
public static class ExperimentEndpoints
{
    /// <summary>
    /// Maps experiment endpoints to the specified route group.
    /// </summary>
    public static RouteGroupBuilder MapExperimentEndpoints(
        this IEndpointRouteBuilder endpoints,
        string prefix = "/api/experiments")
    {
        var group = endpoints.MapGroup(prefix)
            .WithTags("Experiments");

        group.MapGet("/", GetExperiments)
            .WithName("Dashboard_GetExperiments");

        group.MapGet("/{name}", GetExperiment)
            .WithName("Dashboard_GetExperiment");

        group.MapPost("/{name}/toggle", ToggleExperiment)
            .WithName("Dashboard_ToggleExperiment");

        group.MapPost("/{name}/activate-variant", ActivateVariant)
            .WithName("Dashboard_ActivateVariant");

        return group;
    }

    private static async Task<IResult> GetExperiments(
        HttpContext context,
        IServiceProvider sp)
    {
        var dataProvider = sp.GetService<IDashboardDataProvider>();
        if (dataProvider == null)
        {
            return Results.Ok(new { experiments = Array.Empty<object>() });
        }

        var tenantContext = context.Items["TenantContext"] as TenantContext;
        var experiments = await dataProvider.GetExperimentsAsync(tenantContext?.TenantId);

        return Results.Ok(new { experiments });
    }

    private static async Task<IResult> GetExperiment(
        string name,
        HttpContext context,
        IServiceProvider sp)
    {
        var dataProvider = sp.GetService<IDashboardDataProvider>();
        if (dataProvider == null)
        {
            return Results.NotFound(new { error = "Data provider not available" });
        }

        var tenantContext = context.Items["TenantContext"] as TenantContext;
        var experiment = await dataProvider.GetExperimentAsync(name, tenantContext?.TenantId);

        if (experiment == null)
        {
            return Results.NotFound(new { error = $"Experiment '{name}' not found" });
        }

        return Results.Ok(experiment);
    }

    private static IResult ToggleExperiment(
        string name,
        HttpContext context,
        IServiceProvider sp)
    {
        var registry = sp.GetService<IExperimentRegistry>();
        if (registry == null)
        {
            return Results.NotFound(new { error = "Experiment registry not available" });
        }

        var experiment = registry.GetExperiment(name);
        if (experiment == null)
        {
            return Results.NotFound(new { error = $"Experiment '{name}' not found" });
        }

        if (registry is IMutableExperimentRegistry mutableRegistry)
        {
            var newState = !experiment.IsActive;
            mutableRegistry.SetExperimentActive(name, newState);

            return Results.Ok(new
            {
                name,
                isActive = newState,
                status = newState ? "Active" : "Inactive"
            });
        }

        return Results.BadRequest(new { error = "Registry does not support modifications" });
    }

    private static IResult ActivateVariant(
        string name,
        HttpContext context,
        IServiceProvider sp,
        ActivateVariantRequest request)
    {
        var registry = sp.GetService<IExperimentRegistry>();
        if (registry == null)
        {
            return Results.NotFound(new { error = "Experiment registry not available" });
        }

        var experiment = registry.GetExperiment(name);
        if (experiment == null)
        {
            return Results.NotFound(new { error = $"Experiment '{name}' not found" });
        }

        if (string.IsNullOrWhiteSpace(request.VariantKey))
        {
            return Results.BadRequest(new { error = "VariantKey must be provided" });
        }

        var variantExists = experiment.Trials?.Any(t =>
            string.Equals(t.Key, request.VariantKey, StringComparison.OrdinalIgnoreCase)) ?? false;

        if (!variantExists)
        {
            return Results.NotFound(new { error = $"Variant '{request.VariantKey}' not found in experiment '{name}'" });
        }

        // Use IDashboardDataProvider to check availability, then activate via mutable registry
        if (registry is IMutableExperimentRegistry mutableRegistry)
        {
            // Ensure the experiment is active
            if (!experiment.IsActive)
            {
                mutableRegistry.SetExperimentActive(name, true);
            }

            // Check for a variant override service
            var variantOverride = sp.GetService<IVariantOverrideService>();
            if (variantOverride != null)
            {
                variantOverride.SetActiveVariant(name, request.VariantKey);
            }

            return Results.Ok(new
            {
                experimentName = name,
                activatedVariant = request.VariantKey,
                experimentActive = true
            });
        }

        return Results.BadRequest(new { error = "Registry does not support modifications" });
    }
}

/// <summary>Request to activate a specific experiment variant.</summary>
/// <param name="VariantKey">The key of the variant to activate.</param>
public record ActivateVariantRequest(string VariantKey);
