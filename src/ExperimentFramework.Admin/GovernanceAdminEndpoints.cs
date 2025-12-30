using ExperimentFramework.Governance;
using ExperimentFramework.Governance.Policy;
using ExperimentFramework.Governance.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Admin;

/// <summary>
/// Provides minimal API endpoints for experiment governance.
/// </summary>
public static class GovernanceAdminEndpoints
{
    /// <summary>
    /// Maps governance administration endpoints to the specified route group.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="prefix">The route prefix (defaults to "/api/governance").</param>
    /// <returns>A route group builder for further configuration.</returns>
    public static RouteGroupBuilder MapGovernanceAdminApi(
        this IEndpointRouteBuilder endpoints,
        string prefix = "/api/governance")
    {
        var group = endpoints.MapGroup(prefix);

        // Lifecycle endpoints
        group.MapGet("/{experimentName}/lifecycle/state", GetLifecycleState)
            .WithName("GetLifecycleState")
            .WithTags("Governance", "Lifecycle");

        group.MapGet("/{experimentName}/lifecycle/history", GetLifecycleHistory)
            .WithName("GetLifecycleHistory")
            .WithTags("Governance", "Lifecycle");

        group.MapGet("/{experimentName}/lifecycle/allowed-transitions", GetAllowedTransitions)
            .WithName("GetAllowedTransitions")
            .WithTags("Governance", "Lifecycle");

        group.MapPost("/{experimentName}/lifecycle/transition", TransitionLifecycleState)
            .WithName("TransitionLifecycleState")
            .WithTags("Governance", "Lifecycle");

        // Version endpoints
        group.MapGet("/{experimentName}/versions", GetVersions)
            .WithName("GetVersions")
            .WithTags("Governance", "Versions");

        group.MapGet("/{experimentName}/versions/latest", GetLatestVersion)
            .WithName("GetLatestVersion")
            .WithTags("Governance", "Versions");

        group.MapGet("/{experimentName}/versions/{versionNumber:int}", GetVersion)
            .WithName("GetVersion")
            .WithTags("Governance", "Versions");

        group.MapGet("/{experimentName}/versions/diff", GetVersionDiff)
            .WithName("GetVersionDiff")
            .WithTags("Governance", "Versions");

        group.MapPost("/{experimentName}/versions", CreateVersion)
            .WithName("CreateVersion")
            .WithTags("Governance", "Versions");

        group.MapPost("/{experimentName}/versions/rollback", RollbackVersion)
            .WithName("RollbackVersion")
            .WithTags("Governance", "Versions");

        // Policy endpoints
        group.MapPost("/{experimentName}/policies/evaluate", EvaluatePolicies)
            .WithName("EvaluatePolicies")
            .WithTags("Governance", "Policies");

        // Approval endpoints
        group.MapPost("/{experimentName}/approvals/evaluate", EvaluateApprovals)
            .WithName("EvaluateApprovals")
            .WithTags("Governance", "Approvals");

        return group;
    }

    #region Lifecycle Endpoints

    private static IResult GetLifecycleState(string experimentName, IServiceProvider sp)
    {
        var manager = sp.GetService<ILifecycleManager>();
        if (manager == null)
        {
            return Results.NotFound(new { error = "Lifecycle manager not available" });
        }

        var state = manager.GetState(experimentName);
        if (state == null)
        {
            return Results.NotFound(new { error = $"No lifecycle state found for experiment '{experimentName}'" });
        }

        return Results.Ok(new
        {
            experimentName,
            state = state.ToString(),
            allowedTransitions = manager.GetAllowedTransitions(experimentName).Select(s => s.ToString())
        });
    }

    private static IResult GetLifecycleHistory(string experimentName, IServiceProvider sp)
    {
        var manager = sp.GetService<ILifecycleManager>();
        if (manager == null)
        {
            return Results.NotFound(new { error = "Lifecycle manager not available" });
        }

        var history = manager.GetHistory(experimentName);
        return Results.Ok(new
        {
            experimentName,
            transitions = history.Select(t => new
            {
                from = t.FromState.ToString(),
                to = t.ToState.ToString(),
                timestamp = t.Timestamp,
                actor = t.Actor,
                reason = t.Reason
            })
        });
    }

    private static IResult GetAllowedTransitions(string experimentName, IServiceProvider sp)
    {
        var manager = sp.GetService<ILifecycleManager>();
        if (manager == null)
        {
            return Results.NotFound(new { error = "Lifecycle manager not available" });
        }

        var allowed = manager.GetAllowedTransitions(experimentName);
        return Results.Ok(new
        {
            experimentName,
            currentState = manager.GetState(experimentName)?.ToString(),
            allowedTransitions = allowed.Select(s => s.ToString())
        });
    }

    private static async Task<IResult> TransitionLifecycleState(
        string experimentName,
        TransitionRequest request,
        IServiceProvider sp)
    {
        var manager = sp.GetService<ILifecycleManager>();
        if (manager == null)
        {
            return Results.NotFound(new { error = "Lifecycle manager not available" });
        }

        if (!Enum.TryParse<ExperimentLifecycleState>(request.TargetState, true, out var targetState))
        {
            return Results.BadRequest(new { error = $"Invalid target state: {request.TargetState}" });
        }

        try
        {
            await manager.TransitionAsync(experimentName, targetState, request.Actor, request.Reason);
            return Results.Ok(new
            {
                experimentName,
                newState = targetState.ToString(),
                actor = request.Actor,
                reason = request.Reason
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Version Endpoints

    private static IResult GetVersions(string experimentName, IServiceProvider sp)
    {
        var manager = sp.GetService<IVersionManager>();
        if (manager == null)
        {
            return Results.NotFound(new { error = "Version manager not available" });
        }

        var versions = manager.GetAllVersions(experimentName);
        return Results.Ok(new
        {
            experimentName,
            versions = versions.Select(v => new
            {
                versionNumber = v.VersionNumber,
                createdAt = v.CreatedAt,
                createdBy = v.CreatedBy,
                changeDescription = v.ChangeDescription,
                lifecycleState = v.LifecycleState?.ToString()
            })
        });
    }

    private static IResult GetLatestVersion(string experimentName, IServiceProvider sp)
    {
        var manager = sp.GetService<IVersionManager>();
        if (manager == null)
        {
            return Results.NotFound(new { error = "Version manager not available" });
        }

        var version = manager.GetLatestVersion(experimentName);
        if (version == null)
        {
            return Results.NotFound(new { error = $"No versions found for experiment '{experimentName}'" });
        }

        return Results.Ok(new
        {
            versionNumber = version.VersionNumber,
            experimentName = version.ExperimentName,
            configuration = version.Configuration,
            createdAt = version.CreatedAt,
            createdBy = version.CreatedBy,
            changeDescription = version.ChangeDescription,
            lifecycleState = version.LifecycleState?.ToString()
        });
    }

    private static IResult GetVersion(string experimentName, int versionNumber, IServiceProvider sp)
    {
        var manager = sp.GetService<IVersionManager>();
        if (manager == null)
        {
            return Results.NotFound(new { error = "Version manager not available" });
        }

        var version = manager.GetVersion(experimentName, versionNumber);
        if (version == null)
        {
            return Results.NotFound(new { error = $"Version {versionNumber} not found for experiment '{experimentName}'" });
        }

        return Results.Ok(new
        {
            versionNumber = version.VersionNumber,
            experimentName = version.ExperimentName,
            configuration = version.Configuration,
            createdAt = version.CreatedAt,
            createdBy = version.CreatedBy,
            changeDescription = version.ChangeDescription,
            lifecycleState = version.LifecycleState?.ToString()
        });
    }

    private static IResult GetVersionDiff(string experimentName, int fromVersion, int toVersion, IServiceProvider sp)
    {
        var manager = sp.GetService<IVersionManager>();
        if (manager == null)
        {
            return Results.NotFound(new { error = "Version manager not available" });
        }

        var diff = manager.GetDiff(experimentName, fromVersion, toVersion);
        if (diff == null)
        {
            return Results.NotFound(new { error = "One or both versions not found" });
        }

        return Results.Ok(new
        {
            experimentName,
            fromVersion = diff.FromVersion,
            toVersion = diff.ToVersion,
            changes = diff.Changes.Select(c => new
            {
                type = c.Type.ToString(),
                path = c.Path,
                oldValue = c.OldValue,
                newValue = c.NewValue
            })
        });
    }

    private static async Task<IResult> CreateVersion(
        string experimentName,
        CreateVersionRequest request,
        IServiceProvider sp)
    {
        var manager = sp.GetService<IVersionManager>();
        if (manager == null)
        {
            return Results.NotFound(new { error = "Version manager not available" });
        }

        ExperimentLifecycleState? lifecycleState = null;
        if (request.LifecycleState != null &&
            Enum.TryParse<ExperimentLifecycleState>(request.LifecycleState, true, out var state))
        {
            lifecycleState = state;
        }

        var version = await manager.CreateVersionAsync(
            experimentName,
            request.Configuration,
            request.Actor,
            request.ChangeDescription,
            lifecycleState);

        return Results.Created($"/api/governance/{experimentName}/versions/{version.VersionNumber}", new
        {
            versionNumber = version.VersionNumber,
            experimentName = version.ExperimentName,
            createdAt = version.CreatedAt,
            createdBy = version.CreatedBy,
            changeDescription = version.ChangeDescription
        });
    }

    private static async Task<IResult> RollbackVersion(
        string experimentName,
        RollbackRequest request,
        IServiceProvider sp)
    {
        var manager = sp.GetService<IVersionManager>();
        if (manager == null)
        {
            return Results.NotFound(new { error = "Version manager not available" });
        }

        try
        {
            var version = await manager.RollbackToVersionAsync(experimentName, request.TargetVersion, request.Actor);
            return Results.Ok(new
            {
                newVersion = version.VersionNumber,
                rolledBackTo = request.TargetVersion,
                experimentName = version.ExperimentName,
                actor = request.Actor
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Policy Endpoints

    private static async Task<IResult> EvaluatePolicies(
        string experimentName,
        PolicyEvaluationRequest request,
        IServiceProvider sp)
    {
        var evaluator = sp.GetService<IPolicyEvaluator>();
        if (evaluator == null)
        {
            return Results.NotFound(new { error = "Policy evaluator not available" });
        }

        ExperimentLifecycleState? currentState = null;
        if (request.CurrentState != null &&
            Enum.TryParse<ExperimentLifecycleState>(request.CurrentState, true, out var cs))
        {
            currentState = cs;
        }

        ExperimentLifecycleState? targetState = null;
        if (request.TargetState != null &&
            Enum.TryParse<ExperimentLifecycleState>(request.TargetState, true, out var ts))
        {
            targetState = ts;
        }

        var context = new PolicyContext
        {
            ExperimentName = experimentName,
            CurrentState = currentState,
            TargetState = targetState,
            Telemetry = request.Telemetry,
            Metadata = request.Metadata
        };

        var results = await evaluator.EvaluateAllAsync(context);
        var allCompliant = await evaluator.AreAllCriticalPoliciesCompliantAsync(context);

        return Results.Ok(new
        {
            experimentName,
            allCriticalPoliciesCompliant = allCompliant,
            evaluations = results.Select(r => new
            {
                policyName = r.PolicyName,
                isCompliant = r.IsCompliant,
                reason = r.Reason,
                severity = r.Severity.ToString(),
                timestamp = r.Timestamp
            })
        });
    }

    #endregion

    #region Approval Endpoints

    private static async Task<IResult> EvaluateApprovals(
        string experimentName,
        ApprovalEvaluationRequest request,
        IServiceProvider sp)
    {
        var manager = sp.GetService<IApprovalManager>();
        if (manager == null)
        {
            return Results.NotFound(new { error = "Approval manager not available" });
        }

        if (!Enum.TryParse<ExperimentLifecycleState>(request.CurrentState, true, out var currentState))
        {
            return Results.BadRequest(new { error = $"Invalid current state: {request.CurrentState}" });
        }

        if (!Enum.TryParse<ExperimentLifecycleState>(request.TargetState, true, out var targetState))
        {
            return Results.BadRequest(new { error = $"Invalid target state: {request.TargetState}" });
        }

        var context = new ApprovalContext
        {
            ExperimentName = experimentName,
            CurrentState = currentState,
            TargetState = targetState,
            Actor = request.Actor,
            Reason = request.Reason,
            Metadata = request.Metadata
        };

        var results = await manager.EvaluateAsync(context);
        var isApproved = await manager.IsApprovedAsync(context);

        return Results.Ok(new
        {
            experimentName,
            isApproved,
            evaluations = results.Select(r => new
            {
                isApproved = r.IsApproved,
                approver = r.Approver,
                reason = r.Reason,
                timestamp = r.Timestamp
            })
        });
    }

    #endregion

    #region Request Models

    private record TransitionRequest(string TargetState, string? Actor, string? Reason);
    private record CreateVersionRequest(object Configuration, string? Actor, string? ChangeDescription, string? LifecycleState);
    private record RollbackRequest(int TargetVersion, string? Actor);
    private record PolicyEvaluationRequest(string? CurrentState, string? TargetState, IReadOnlyDictionary<string, object>? Telemetry, IReadOnlyDictionary<string, object>? Metadata);
    private record ApprovalEvaluationRequest(string CurrentState, string TargetState, string? Actor, string? Reason, IReadOnlyDictionary<string, object>? Metadata);

    #endregion
}
