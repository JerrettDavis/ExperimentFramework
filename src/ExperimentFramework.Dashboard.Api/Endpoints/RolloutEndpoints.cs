using ExperimentFramework.Admin;
using ExperimentFramework.Dashboard.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Dashboard.Api.Endpoints;

/// <summary>
/// Provides minimal API endpoints for rollout management.
/// </summary>
public static class RolloutEndpoints
{
    /// <summary>
    /// Maps rollout endpoints to the specified route group.
    /// </summary>
    public static RouteGroupBuilder MapRolloutEndpoints(
        this IEndpointRouteBuilder endpoints,
        string prefix = "/api/rollout")
    {
        var group = endpoints.MapGroup(prefix)
            .WithTags("Rollout");

        group.MapGet("/{experimentName}/config", GetRolloutConfig)
            .WithName("Dashboard_GetRolloutConfig");

        group.MapPost("/{experimentName}/config", CreateOrUpdateRolloutConfig)
            .WithName("Dashboard_CreateOrUpdateRolloutConfig");

        group.MapPost("/{experimentName}/advance", AdvanceRollout)
            .WithName("Dashboard_AdvanceRollout");

        group.MapPost("/{experimentName}/pause", PauseRollout)
            .WithName("Dashboard_PauseRollout");

        group.MapPost("/{experimentName}/resume", ResumeRollout)
            .WithName("Dashboard_ResumeRollout");

        group.MapPost("/{experimentName}/rollback", RollbackRollout)
            .WithName("Dashboard_RollbackRollout");

        group.MapPost("/{experimentName}/restart", RestartRollout)
            .WithName("Dashboard_RestartRollout");

        group.MapDelete("/{experimentName}/config", DeleteRolloutConfig)
            .WithName("Dashboard_DeleteRolloutConfig");

        return group;
    }

    private static async Task<IResult> GetRolloutConfig(
        string experimentName,
        HttpContext httpContext,
        IServiceProvider sp,
        CancellationToken ct)
    {
        var persistence = sp.GetService<IRolloutPersistenceBackplane>();
        if (persistence == null)
        {
            return Results.Problem(
                "Rollout persistence not configured. Register IRolloutPersistenceBackplane in DI.",
                statusCode: 503);
        }

        var tenantId = GetTenantId(httpContext);
        var config = await persistence.GetRolloutConfigAsync(experimentName, tenantId, ct);

        if (config == null)
        {
            return Results.NotFound(new { message = $"No rollout configuration found for experiment '{experimentName}'" });
        }

        return Results.Ok(config);
    }

    private static async Task<IResult> CreateOrUpdateRolloutConfig(
        string experimentName,
        HttpContext httpContext,
        IServiceProvider sp,
        RolloutConfiguration request,
        CancellationToken ct)
    {
        var persistence = sp.GetService<IRolloutPersistenceBackplane>();
        if (persistence == null)
        {
            return Results.Problem(
                "Rollout persistence not configured. Register IRolloutPersistenceBackplane in DI.",
                statusCode: 503);
        }

        var registry = sp.GetService<IMutableExperimentRegistry>();
        if (registry == null)
        {
            return Results.Problem(
                "Mutable experiment registry not available. Rollout requires IMutableExperimentRegistry.",
                statusCode: 503);
        }

        // Validate experiment exists
        var experiment = registry.GetExperiment(experimentName);
        if (experiment == null)
        {
            return Results.NotFound(new { message = $"Experiment '{experimentName}' not found" });
        }

        // Validate target variant exists
        if (string.IsNullOrEmpty(request.TargetVariant))
        {
            return Results.BadRequest(new { message = "Target variant must be specified" });
        }

        var variantExists = experiment.Trials?.Any(t => t.Key == request.TargetVariant) ?? false;
        if (!variantExists)
        {
            return Results.BadRequest(new { message = $"Variant '{request.TargetVariant}' not found in experiment '{experimentName}'" });
        }

        request.ExperimentName = experimentName;
        var tenantId = GetTenantId(httpContext);

        // Initialize first stage as active if starting
        if (request.Status == RolloutStatus.InProgress && request.Stages.Any())
        {
            request.Stages[0].Status = RolloutStageStatus.Active;
            request.Stages[0].ExecutedDate = DateTimeOffset.UtcNow;
            request.Percentage = request.Stages[0].Percentage;
            request.StartDate = DateTimeOffset.UtcNow;

            // Update experiment rollout percentage
            registry.SetRolloutPercentage(experimentName, request.Percentage);
        }

        await persistence.SaveRolloutConfigAsync(request, tenantId, ct);

        return Results.Ok(request);
    }

    private static async Task<IResult> AdvanceRollout(
        string experimentName,
        HttpContext httpContext,
        IServiceProvider sp,
        CancellationToken ct)
    {
        var persistence = sp.GetService<IRolloutPersistenceBackplane>();
        if (persistence == null)
        {
            return Results.Problem("Rollout persistence not configured", statusCode: 503);
        }

        var registry = sp.GetService<IMutableExperimentRegistry>();
        if (registry == null)
        {
            return Results.Problem("Mutable experiment registry not available", statusCode: 503);
        }

        var tenantId = GetTenantId(httpContext);
        var config = await persistence.GetRolloutConfigAsync(experimentName, tenantId, ct);

        if (config == null)
        {
            return Results.NotFound(new { message = $"No rollout configuration found for experiment '{experimentName}'" });
        }

        if (config.Status != RolloutStatus.InProgress)
        {
            return Results.BadRequest(new { message = "Rollout is not in progress" });
        }

        var activeStageIndex = config.Stages.FindIndex(s => s.Status == RolloutStageStatus.Active);
        if (activeStageIndex < 0)
        {
            return Results.BadRequest(new { message = "No active stage found" });
        }

        // Complete current stage
        config.Stages[activeStageIndex].Status = RolloutStageStatus.Completed;

        // Check if there's a next stage
        if (activeStageIndex < config.Stages.Count - 1)
        {
            // Activate next stage
            var nextStage = config.Stages[activeStageIndex + 1];
            nextStage.Status = RolloutStageStatus.Active;
            nextStage.ExecutedDate = DateTimeOffset.UtcNow;
            config.Percentage = nextStage.Percentage;

            // Update experiment rollout percentage
            registry.SetRolloutPercentage(experimentName, config.Percentage);

            // Simulate users affected (in real implementation, query from data backplane)
            nextStage.UsersAffected = (int)(config.TotalUsers * (nextStage.Percentage / 100.0));
        }
        else
        {
            // Rollout completed - reached 100%
            config.Status = RolloutStatus.Completed;
            config.Percentage = 100;

            // Set rollout to 100%
            registry.SetRolloutPercentage(experimentName, 100);
        }

        await persistence.SaveRolloutConfigAsync(config, tenantId, ct);

        return Results.Ok(config);
    }

    private static async Task<IResult> PauseRollout(
        string experimentName,
        HttpContext httpContext,
        IServiceProvider sp,
        CancellationToken ct)
    {
        var persistence = sp.GetService<IRolloutPersistenceBackplane>();
        if (persistence == null)
        {
            return Results.Problem("Rollout persistence not configured", statusCode: 503);
        }

        var tenantId = GetTenantId(httpContext);
        var config = await persistence.GetRolloutConfigAsync(experimentName, tenantId, ct);

        if (config == null)
        {
            return Results.NotFound(new { message = $"No rollout configuration found for experiment '{experimentName}'" });
        }

        if (config.Status != RolloutStatus.InProgress)
        {
            return Results.BadRequest(new { message = "Rollout is not in progress" });
        }

        config.Status = RolloutStatus.Paused;
        await persistence.SaveRolloutConfigAsync(config, tenantId, ct);

        return Results.Ok(config);
    }

    private static async Task<IResult> ResumeRollout(
        string experimentName,
        HttpContext httpContext,
        IServiceProvider sp,
        CancellationToken ct)
    {
        var persistence = sp.GetService<IRolloutPersistenceBackplane>();
        if (persistence == null)
        {
            return Results.Problem("Rollout persistence not configured", statusCode: 503);
        }

        var registry = sp.GetService<IMutableExperimentRegistry>();
        if (registry == null)
        {
            return Results.Problem("Mutable experiment registry not available", statusCode: 503);
        }

        var tenantId = GetTenantId(httpContext);
        var config = await persistence.GetRolloutConfigAsync(experimentName, tenantId, ct);

        if (config == null)
        {
            return Results.NotFound(new { message = $"No rollout configuration found for experiment '{experimentName}'" });
        }

        if (config.Status != RolloutStatus.Paused && config.Status != RolloutStatus.RolledBack)
        {
            return Results.BadRequest(new { message = "Rollout is not paused or rolled back" });
        }

        config.Status = RolloutStatus.InProgress;

        // Reapply current percentage
        registry.SetRolloutPercentage(experimentName, config.Percentage);

        await persistence.SaveRolloutConfigAsync(config, tenantId, ct);

        return Results.Ok(config);
    }

    private static async Task<IResult> RollbackRollout(
        string experimentName,
        HttpContext httpContext,
        IServiceProvider sp,
        CancellationToken ct)
    {
        var persistence = sp.GetService<IRolloutPersistenceBackplane>();
        if (persistence == null)
        {
            return Results.Problem("Rollout persistence not configured", statusCode: 503);
        }

        var registry = sp.GetService<IMutableExperimentRegistry>();
        if (registry == null)
        {
            return Results.Problem("Mutable experiment registry not available", statusCode: 503);
        }

        var tenantId = GetTenantId(httpContext);
        var config = await persistence.GetRolloutConfigAsync(experimentName, tenantId, ct);

        if (config == null)
        {
            return Results.NotFound(new { message = $"No rollout configuration found for experiment '{experimentName}'" });
        }

        config.Status = RolloutStatus.RolledBack;
        config.Percentage = 0;

        // Rollback experiment to 0%
        registry.SetRolloutPercentage(experimentName, 0);

        // Mark all stages as skipped except completed ones
        foreach (var stage in config.Stages)
        {
            if (stage.Status == RolloutStageStatus.Active || stage.Status == RolloutStageStatus.Pending)
            {
                stage.Status = RolloutStageStatus.Skipped;
            }
        }

        config.UsersInRollout = 0;
        await persistence.SaveRolloutConfigAsync(config, tenantId, ct);

        return Results.Ok(config);
    }

    private static async Task<IResult> RestartRollout(
        string experimentName,
        HttpContext httpContext,
        IServiceProvider sp,
        CancellationToken ct)
    {
        var persistence = sp.GetService<IRolloutPersistenceBackplane>();
        if (persistence == null)
        {
            return Results.Problem("Rollout persistence not configured", statusCode: 503);
        }

        var registry = sp.GetService<IMutableExperimentRegistry>();
        if (registry == null)
        {
            return Results.Problem("Mutable experiment registry not available", statusCode: 503);
        }

        var tenantId = GetTenantId(httpContext);
        var config = await persistence.GetRolloutConfigAsync(experimentName, tenantId, ct);

        if (config == null)
        {
            return Results.NotFound(new { message = $"No rollout configuration found for experiment '{experimentName}'" });
        }

        // Reset all stages
        foreach (var stage in config.Stages)
        {
            stage.Status = RolloutStageStatus.Pending;
            stage.ExecutedDate = null;
            stage.UsersAffected = 0;
        }

        // Activate first stage
        if (config.Stages.Any())
        {
            config.Stages[0].Status = RolloutStageStatus.Active;
            config.Stages[0].ExecutedDate = DateTimeOffset.UtcNow;
            config.Percentage = config.Stages[0].Percentage;

            // Update experiment rollout percentage
            registry.SetRolloutPercentage(experimentName, config.Percentage);
        }

        config.Status = RolloutStatus.InProgress;
        config.StartDate = DateTimeOffset.UtcNow;
        config.UsersInRollout = 0;

        await persistence.SaveRolloutConfigAsync(config, tenantId, ct);

        return Results.Ok(config);
    }

    private static async Task<IResult> DeleteRolloutConfig(
        string experimentName,
        HttpContext httpContext,
        IServiceProvider sp,
        CancellationToken ct)
    {
        var persistence = sp.GetService<IRolloutPersistenceBackplane>();
        if (persistence == null)
        {
            return Results.Problem("Rollout persistence not configured", statusCode: 503);
        }

        var registry = sp.GetService<IMutableExperimentRegistry>();

        var tenantId = GetTenantId(httpContext);
        await persistence.DeleteRolloutConfigAsync(experimentName, tenantId, ct);

        // Reset experiment rollout percentage to 100 (no rollout)
        registry?.SetRolloutPercentage(experimentName, 100);

        return Results.Ok(new { message = "Rollout configuration deleted" });
    }

    private static string? GetTenantId(HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue("TenantContext", out var tenantContext) &&
            tenantContext is TenantContext context)
        {
            return context.TenantId;
        }

        return null;
    }
}
