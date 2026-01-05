using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace ExperimentFramework.Dashboard.Api.Endpoints;

/// <summary>
/// Provides minimal API endpoints for rollout management.
/// </summary>
public static class RolloutEndpoints
{
    // In-memory storage for rollout configurations (replace with persistence in production)
    private static readonly ConcurrentDictionary<string, RolloutConfiguration> _rollouts = new();

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

    private static IResult GetRolloutConfig(string experimentName, IServiceProvider sp)
    {
        if (!_rollouts.TryGetValue(experimentName, out var config))
        {
            return Results.NotFound(new { message = $"No rollout configuration found for experiment '{experimentName}'" });
        }

        return Results.Ok(config);
    }

    private static IResult CreateOrUpdateRolloutConfig(
        string experimentName,
        IServiceProvider sp,
        RolloutConfiguration request,
        CancellationToken ct)
    {
        request.ExperimentName = experimentName;
        request.LastModified = DateTimeOffset.UtcNow;

        // Initialize first stage as active if starting
        if (request.Status == RolloutStatus.InProgress && request.Stages.Any())
        {
            request.Stages[0].Status = RolloutStageStatus.Active;
            request.Stages[0].ExecutedDate = DateTimeOffset.UtcNow;
            request.Percentage = request.Stages[0].Percentage;
            request.StartDate = DateTimeOffset.UtcNow;
        }

        _rollouts[experimentName] = request;

        return Results.Ok(request);
    }

    private static IResult AdvanceRollout(string experimentName, IServiceProvider sp)
    {
        if (!_rollouts.TryGetValue(experimentName, out var config))
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

            // Simulate users affected (in real implementation, query from data backplane)
            nextStage.UsersAffected = (int)(config.TotalUsers * (nextStage.Percentage / 100.0));
        }
        else
        {
            // Rollout completed
            config.Status = RolloutStatus.Completed;
            config.Percentage = 100;
        }

        config.LastModified = DateTimeOffset.UtcNow;

        return Results.Ok(config);
    }

    private static IResult PauseRollout(string experimentName, IServiceProvider sp)
    {
        if (!_rollouts.TryGetValue(experimentName, out var config))
        {
            return Results.NotFound(new { message = $"No rollout configuration found for experiment '{experimentName}'" });
        }

        if (config.Status != RolloutStatus.InProgress)
        {
            return Results.BadRequest(new { message = "Rollout is not in progress" });
        }

        config.Status = RolloutStatus.Paused;
        config.LastModified = DateTimeOffset.UtcNow;

        return Results.Ok(config);
    }

    private static IResult ResumeRollout(string experimentName, IServiceProvider sp)
    {
        if (!_rollouts.TryGetValue(experimentName, out var config))
        {
            return Results.NotFound(new { message = $"No rollout configuration found for experiment '{experimentName}'" });
        }

        if (config.Status != RolloutStatus.Paused && config.Status != RolloutStatus.RolledBack)
        {
            return Results.BadRequest(new { message = "Rollout is not paused or rolled back" });
        }

        config.Status = RolloutStatus.InProgress;
        config.LastModified = DateTimeOffset.UtcNow;

        return Results.Ok(config);
    }

    private static IResult RollbackRollout(string experimentName, IServiceProvider sp)
    {
        if (!_rollouts.TryGetValue(experimentName, out var config))
        {
            return Results.NotFound(new { message = $"No rollout configuration found for experiment '{experimentName}'" });
        }

        config.Status = RolloutStatus.RolledBack;
        config.Percentage = 0;

        // Mark all stages as skipped except completed ones
        foreach (var stage in config.Stages)
        {
            if (stage.Status == RolloutStageStatus.Active || stage.Status == RolloutStageStatus.Pending)
            {
                stage.Status = RolloutStageStatus.Skipped;
            }
        }

        config.UsersInRollout = 0;
        config.LastModified = DateTimeOffset.UtcNow;

        return Results.Ok(config);
    }

    private static IResult RestartRollout(string experimentName, IServiceProvider sp)
    {
        if (!_rollouts.TryGetValue(experimentName, out var config))
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
        }

        config.Status = RolloutStatus.InProgress;
        config.StartDate = DateTimeOffset.UtcNow;
        config.UsersInRollout = 0;
        config.LastModified = DateTimeOffset.UtcNow;

        return Results.Ok(config);
    }

    private static IResult DeleteRolloutConfig(string experimentName, IServiceProvider sp)
    {
        _rollouts.TryRemove(experimentName, out _);
        return Results.Ok(new { message = "Rollout configuration deleted" });
    }
}

// DTOs
public class RolloutConfiguration
{
    public string ExperimentName { get; set; } = "";
    public bool Enabled { get; set; }
    public string? TargetVariant { get; set; }
    public int Percentage { get; set; } = 0;
    public List<RolloutStageDto> Stages { get; set; } = [];
    public DateTimeOffset? StartDate { get; set; }
    public RolloutStatus Status { get; set; } = RolloutStatus.NotStarted;
    public int TotalUsers { get; set; } = 10000; // Default estimate
    public int UsersInRollout { get; set; } = 0;
    public DateTimeOffset LastModified { get; set; }
}

public class RolloutStageDto
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int Percentage { get; set; }
    public DateTimeOffset? ScheduledDate { get; set; }
    public DateTimeOffset? ExecutedDate { get; set; }
    public RolloutStageStatus Status { get; set; } = RolloutStageStatus.Pending;
    public int? DurationHours { get; set; }
    public int UsersAffected { get; set; } = 0;
    public Dictionary<string, double> Metrics { get; set; } = [];
}

public enum RolloutStatus
{
    NotStarted,
    InProgress,
    Completed,
    Paused,
    RolledBack
}

public enum RolloutStageStatus
{
    Pending,
    Active,
    Completed,
    Skipped
}
