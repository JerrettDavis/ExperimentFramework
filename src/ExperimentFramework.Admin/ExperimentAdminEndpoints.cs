using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Admin;

/// <summary>
/// Provides minimal API endpoints for experiment administration.
/// </summary>
public static class ExperimentAdminEndpoints
{
    /// <summary>
    /// Maps experiment administration endpoints to the specified route group.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="prefix">The route prefix (defaults to "/api/experiments").</param>
    /// <returns>A route group builder for further configuration.</returns>
    public static RouteGroupBuilder MapExperimentAdminApi(
        this IEndpointRouteBuilder endpoints,
        string prefix = "/api/experiments")
    {
        var group = endpoints.MapGroup(prefix);

        group.MapGet("/", GetExperiments)
            .WithName("GetExperiments")
            .WithTags("Experiments");

        group.MapGet("/{name}", GetExperiment)
            .WithName("GetExperiment")
            .WithTags("Experiments");

        group.MapGet("/{name}/status", GetExperimentStatus)
            .WithName("GetExperimentStatus")
            .WithTags("Experiments");

        group.MapPost("/{name}/toggle", ToggleExperiment)
            .WithName("ToggleExperiment")
            .WithTags("Experiments");

        return group;
    }

    private static IResult GetExperiments(IServiceProvider sp)
    {
        var registry = sp.GetService<IExperimentRegistry>();
        if (registry == null)
        {
            return Results.Ok(new { experiments = Array.Empty<object>(), message = "No experiment registry available" });
        }

        var experiments = registry.GetAllExperiments()
            .Select(e => new
            {
                e.Name,
                ServiceType = e.ServiceType?.Name,
                e.IsActive,
                TrialCount = e.Trials?.Count ?? 0
            });

        return Results.Ok(new { experiments });
    }

    private static IResult GetExperiment(string name, IServiceProvider sp)
    {
        var registry = sp.GetService<IExperimentRegistry>();
        if (registry == null)
        {
            return Results.NotFound(new { error = "No experiment registry available" });
        }

        var experiment = registry.GetExperiment(name);
        if (experiment == null)
        {
            return Results.NotFound(new { error = $"Experiment '{name}' not found" });
        }

        return Results.Ok(new
        {
            experiment.Name,
            ServiceType = experiment.ServiceType?.Name,
            experiment.IsActive,
            Trials = experiment.Trials?.Select(t => new
            {
                t.Key,
                ImplementationType = t.ImplementationType?.Name,
                t.IsControl
            })
        });
    }

    private static IResult GetExperimentStatus(string name, IServiceProvider sp)
    {
        var registry = sp.GetService<IExperimentRegistry>();
        if (registry == null)
        {
            return Results.NotFound(new { error = "No experiment registry available" });
        }

        var experiment = registry.GetExperiment(name);
        if (experiment == null)
        {
            return Results.NotFound(new { error = $"Experiment '{name}' not found" });
        }

        return Results.Ok(new
        {
            experiment.Name,
            experiment.IsActive,
            Status = experiment.IsActive ? "Active" : "Inactive"
        });
    }

    private static IResult ToggleExperiment(string name, IServiceProvider sp)
    {
        var registry = sp.GetService<IExperimentRegistry>();
        if (registry == null)
        {
            return Results.NotFound(new { error = "No experiment registry available" });
        }

        var experiment = registry.GetExperiment(name);
        if (experiment == null)
        {
            return Results.NotFound(new { error = $"Experiment '{name}' not found" });
        }

        // Toggle requires a mutable registry
        if (registry is IMutableExperimentRegistry mutableRegistry)
        {
            var newState = !experiment.IsActive;
            mutableRegistry.SetExperimentActive(name, newState);

            return Results.Ok(new
            {
                experiment.Name,
                IsActive = newState,
                Status = newState ? "Active" : "Inactive"
            });
        }

        return Results.BadRequest(new { error = "Registry does not support runtime modifications" });
    }
}
