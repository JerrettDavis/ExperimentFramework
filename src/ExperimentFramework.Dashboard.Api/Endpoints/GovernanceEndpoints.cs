using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ExperimentFramework.Governance;
using ExperimentFramework.Governance.Persistence;
using ExperimentFramework.Governance.Persistence.Models;

namespace ExperimentFramework.Dashboard.Api.Endpoints;

/// <summary>
/// Provides minimal API endpoints for governance workflows.
/// </summary>
public static class GovernanceEndpoints
{
    /// <summary>
    /// Maps governance endpoints to the specified route group.
    /// </summary>
    public static RouteGroupBuilder MapGovernanceEndpoints(
        this IEndpointRouteBuilder endpoints,
        string prefix = "/api/governance")
    {
        var group = endpoints.MapGroup(prefix)
            .WithTags("Governance");

        group.MapGet("/{experimentName}/state", GetLifecycleState)
            .WithName("Dashboard_GetLifecycleState");

        group.MapPost("/{experimentName}/transition", TransitionState)
            .WithName("Dashboard_TransitionState");

        group.MapGet("/approvals/pending", GetPendingApprovals)
            .WithName("Dashboard_GetPendingApprovals");

        group.MapPost("/approvals/{id}/approve", ApproveTransition)
            .WithName("Dashboard_ApproveTransition");

        group.MapPost("/approvals/{id}/reject", RejectTransition)
            .WithName("Dashboard_RejectTransition");

        group.MapGet("/{experimentName}/policies", GetPolicies)
            .WithName("Dashboard_GetPolicies");

        group.MapGet("/{experimentName}/versions", GetVersions)
            .WithName("Dashboard_GetVersions");

        group.MapGet("/{experimentName}/versions/{version}", GetVersion)
            .WithName("Dashboard_GetVersion");

        group.MapPost("/{experimentName}/versions/{version}/rollback", RollbackVersion)
            .WithName("Dashboard_RollbackVersion");

        group.MapGet("/{experimentName}/audit", GetAuditLog)
            .WithName("Dashboard_GetAuditLog");

        group.MapGet("/{experimentName}/transitions", GetStateTransitionHistory)
            .WithName("Dashboard_GetStateTransitionHistory");

        return group;
    }

    private static async Task<IResult> GetLifecycleState(string experimentName, IServiceProvider sp, CancellationToken ct)
    {
        var backplane = sp.GetService<IGovernancePersistenceBackplane>();
        if (backplane == null)
        {
            return Results.Ok(new
            {
                experimentName,
                state = "Draft",
                transitions = new[] { "Submit", "Archive" },
                message = "Governance persistence not configured"
            });
        }

        var state = await backplane.GetExperimentStateAsync(experimentName, cancellationToken: ct);
        if (state == null)
        {
            return Results.NotFound(new { message = $"No governance state found for experiment '{experimentName}'" });
        }

        // Get available transitions based on current state
        var transitions = GetAvailableTransitions(state.CurrentState);

        return Results.Ok(new
        {
            experimentName = state.ExperimentName,
            state = state.CurrentState.ToString(),
            configurationVersion = state.ConfigurationVersion,
            lastModified = state.LastModified,
            lastModifiedBy = state.LastModifiedBy,
            transitions,
            tenantId = state.TenantId,
            environment = state.Environment
        });
    }

    private static string[] GetAvailableTransitions(ExperimentLifecycleState currentState)
    {
        return currentState switch
        {
            ExperimentLifecycleState.Draft => new[] { "PendingApproval", "Archived" },
            ExperimentLifecycleState.PendingApproval => new[] { "Approved", "Rejected", "Draft" },
            ExperimentLifecycleState.Approved => new[] { "Running", "Ramping", "Rejected" },
            ExperimentLifecycleState.Running => new[] { "Paused", "Archived", "RolledBack" },
            ExperimentLifecycleState.Ramping => new[] { "Running", "RolledBack", "Paused" },
            ExperimentLifecycleState.Paused => new[] { "Running", "Archived", "RolledBack" },
            ExperimentLifecycleState.RolledBack => new[] { "Draft", "Archived" },
            ExperimentLifecycleState.Rejected => new[] { "Draft", "Archived" },
            ExperimentLifecycleState.Archived => Array.Empty<string>(),
            _ => Array.Empty<string>()
        };
    }

    private static async Task<IResult> TransitionState(
        string experimentName,
        IServiceProvider sp,
        TransitionStateRequest request,
        CancellationToken ct)
    {
        var backplane = sp.GetService<IGovernancePersistenceBackplane>();
        if (backplane == null)
        {
            return Results.BadRequest(new { message = "Governance persistence not configured" });
        }

        // Parse target state
        if (!Enum.TryParse<ExperimentLifecycleState>(request.TargetState, out var targetState))
        {
            return Results.BadRequest(new { message = $"Invalid target state: {request.TargetState}" });
        }

        // Get current state
        var currentState = await backplane.GetExperimentStateAsync(experimentName, cancellationToken: ct);
        if (currentState == null)
        {
            return Results.NotFound(new { message = $"No governance state found for experiment '{experimentName}'" });
        }

        // Validate transition is allowed
        var allowedTransitions = GetAvailableTransitions(currentState.CurrentState);
        if (!allowedTransitions.Contains(targetState.ToString()))
        {
            return Results.BadRequest(new
            {
                message = $"Transition from {currentState.CurrentState} to {targetState} is not allowed",
                currentState = currentState.CurrentState.ToString(),
                allowedTransitions
            });
        }

        // Record the transition
        var transition = new PersistedStateTransition
        {
            TransitionId = Guid.NewGuid().ToString(),
            ExperimentName = experimentName,
            FromState = currentState.CurrentState,
            ToState = targetState,
            Timestamp = DateTimeOffset.UtcNow,
            Actor = request.Actor ?? "unknown",
            Reason = request.Reason,
            TenantId = currentState.TenantId,
            Environment = currentState.Environment
        };

        await backplane.AppendStateTransitionAsync(transition, ct);

        // Update current state
        var newState = new PersistedExperimentState
        {
            ExperimentName = currentState.ExperimentName,
            CurrentState = targetState,
            ConfigurationVersion = currentState.ConfigurationVersion,
            LastModified = DateTimeOffset.UtcNow,
            LastModifiedBy = request.Actor ?? "unknown",
            ETag = Guid.NewGuid().ToString(),
            Metadata = currentState.Metadata,
            TenantId = currentState.TenantId,
            Environment = currentState.Environment
        };

        var result = await backplane.SaveExperimentStateAsync(newState, currentState.ETag, ct);
        if (!result.Success)
        {
            return Results.Conflict(new { message = "State transition failed due to concurrency conflict" });
        }

        return Results.Ok(new
        {
            experimentName,
            previousState = currentState.CurrentState.ToString(),
            newState = targetState.ToString(),
            transitionId = transition.TransitionId,
            timestamp = transition.Timestamp
        });
    }

    private static async Task<IResult> GetPendingApprovals(IServiceProvider sp, CancellationToken ct)
    {
        var backplane = sp.GetService<IGovernancePersistenceBackplane>();
        if (backplane == null)
        {
            return Results.Ok(new { approvals = Array.Empty<object>() });
        }

        // Note: This is a simplified implementation. In a full system, you'd track pending approvals separately
        // For now, we'll return approval records that are pending (IsApproved = false)
        // This would typically query a separate "pending approvals" table/collection

        return Results.Ok(new
        {
            approvals = Array.Empty<object>(),
            message = "Pending approvals tracking requires additional infrastructure"
        });
    }

    private static IResult ApproveTransition(string id, IServiceProvider sp)
    {
        // TODO: Approve transition (Phase 4)
        return Results.Ok(new
        {
            id,
            status = "Approved",
            message = "Approval implementation pending (Phase 4)"
        });
    }

    private static IResult RejectTransition(string id, IServiceProvider sp, RejectRequest request)
    {
        // TODO: Reject transition (Phase 4)
        return Results.Ok(new
        {
            id,
            status = "Rejected",
            reason = request.Reason,
            message = "Rejection implementation pending (Phase 4)"
        });
    }

    private static async Task<IResult> GetPolicies(string experimentName, IServiceProvider sp, CancellationToken ct)
    {
        var backplane = sp.GetService<IGovernancePersistenceBackplane>();
        if (backplane == null)
        {
            return Results.Ok(new { experimentName, policies = Array.Empty<object>() });
        }

        var evaluations = await backplane.GetPolicyEvaluationsAsync(experimentName, cancellationToken: ct);

        return Results.Ok(new
        {
            experimentName,
            policies = evaluations.Select(p => new
            {
                evaluationId = p.EvaluationId,
                policyName = p.PolicyName,
                isCompliant = p.IsCompliant,
                reason = p.Reason,
                severity = p.Severity.ToString(),
                timestamp = p.Timestamp,
                currentState = p.CurrentState?.ToString(),
                targetState = p.TargetState?.ToString()
            })
        });
    }

    private static async Task<IResult> GetVersions(string experimentName, IServiceProvider sp, CancellationToken ct)
    {
        var backplane = sp.GetService<IGovernancePersistenceBackplane>();
        if (backplane == null)
        {
            return Results.Ok(new { experimentName, versions = Array.Empty<object>() });
        }

        var versions = await backplane.GetAllConfigurationVersionsAsync(experimentName, cancellationToken: ct);

        return Results.Ok(new
        {
            experimentName,
            versions = versions.Select(v => new
            {
                versionNumber = v.VersionNumber,
                createdAt = v.CreatedAt,
                createdBy = v.CreatedBy,
                changeDescription = v.ChangeDescription,
                lifecycleState = v.LifecycleState?.ToString(),
                isRollback = v.IsRollback,
                rolledBackFrom = v.RolledBackFrom,
                configurationHash = v.ConfigurationHash
            })
        });
    }

    private static async Task<IResult> GetVersion(string experimentName, int version, IServiceProvider sp, CancellationToken ct)
    {
        var backplane = sp.GetService<IGovernancePersistenceBackplane>();
        if (backplane == null)
        {
            return Results.NotFound(new { message = "Governance persistence not configured" });
        }

        var configVersion = await backplane.GetConfigurationVersionAsync(experimentName, version, cancellationToken: ct);
        if (configVersion == null)
        {
            return Results.NotFound(new { message = $"Version {version} not found for experiment '{experimentName}'" });
        }

        return Results.Ok(new
        {
            experimentName,
            versionNumber = configVersion.VersionNumber,
            configurationJson = configVersion.ConfigurationJson,
            createdAt = configVersion.CreatedAt,
            createdBy = configVersion.CreatedBy,
            changeDescription = configVersion.ChangeDescription,
            lifecycleState = configVersion.LifecycleState?.ToString(),
            configurationHash = configVersion.ConfigurationHash,
            isRollback = configVersion.IsRollback,
            rolledBackFrom = configVersion.RolledBackFrom
        });
    }

    private static async Task<IResult> RollbackVersion(string experimentName, int version, IServiceProvider sp, CancellationToken ct)
    {
        var backplane = sp.GetService<IGovernancePersistenceBackplane>();
        if (backplane == null)
        {
            return Results.BadRequest(new { message = "Governance persistence not configured" });
        }

        // Get the version to rollback to
        var targetVersion = await backplane.GetConfigurationVersionAsync(experimentName, version, cancellationToken: ct);
        if (targetVersion == null)
        {
            return Results.NotFound(new { message = $"Version {version} not found for experiment '{experimentName}'" });
        }

        // Get latest version to determine next version number
        var latestVersion = await backplane.GetLatestConfigurationVersionAsync(experimentName, cancellationToken: ct);
        var nextVersionNumber = (latestVersion?.VersionNumber ?? 0) + 1;

        // Create new version as rollback
        var rollbackVersion = new PersistedConfigurationVersion
        {
            ExperimentName = experimentName,
            VersionNumber = nextVersionNumber,
            ConfigurationJson = targetVersion.ConfigurationJson,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "system",
            ChangeDescription = $"Rollback to version {version}",
            LifecycleState = targetVersion.LifecycleState,
            ConfigurationHash = targetVersion.ConfigurationHash,
            IsRollback = true,
            RolledBackFrom = version,
            TenantId = targetVersion.TenantId,
            Environment = targetVersion.Environment
        };

        await backplane.AppendConfigurationVersionAsync(rollbackVersion, ct);

        return Results.Ok(new
        {
            experimentName,
            rolledBackToVersion = version,
            newVersionNumber = nextVersionNumber,
            timestamp = rollbackVersion.CreatedAt
        });
    }

    private static async Task<IResult> GetAuditLog(string experimentName, IServiceProvider sp, CancellationToken ct)
    {
        var backplane = sp.GetService<IGovernancePersistenceBackplane>();
        if (backplane == null)
        {
            return Results.Ok(new { experimentName, auditLog = Array.Empty<object>() });
        }

        // Get state transitions
        var transitions = await backplane.GetStateTransitionHistoryAsync(experimentName, cancellationToken: ct);

        // Get approval records
        var approvals = await backplane.GetApprovalRecordsAsync(experimentName, cancellationToken: ct);

        // Combine and sort by timestamp
        var auditLog = transitions.Select(t => new
        {
            type = "StateTransition",
            timestamp = t.Timestamp,
            actor = t.Actor,
            details = $"Transitioned from {t.FromState} to {t.ToState}",
            reason = t.Reason,
            transitionId = t.TransitionId
        })
        .Concat(approvals.Select(a => new
        {
            type = "Approval",
            timestamp = a.Timestamp,
            actor = a.Approver,
            details = $"{(a.IsApproved ? "Approved" : "Rejected")} transition to {a.ToState}",
            reason = a.Reason,
            transitionId = a.TransitionId
        }))
        .OrderByDescending(x => x.timestamp);

        return Results.Ok(new
        {
            experimentName,
            auditLog
        });
    }

    private static async Task<IResult> GetStateTransitionHistory(string experimentName, IServiceProvider sp, CancellationToken ct)
    {
    var backplane = sp.GetService<IGovernancePersistenceBackplane>();
    if (backplane == null)
    {
        return Results.Ok(new { experimentName, transitions = Array.Empty<object>() });
    }

    var transitions = await backplane.GetStateTransitionHistoryAsync(experimentName, cancellationToken: ct);

    return Results.Ok(new
    {
        experimentName,
        transitions = transitions.Select(t => new
        {
            transitionId = t.TransitionId,
            fromState = t.FromState.ToString(),
            toState = t.ToState.ToString(),
            timestamp = t.Timestamp,
            actor = t.Actor,
            reason = t.Reason,
            tenantId = t.TenantId,
            environment = t.Environment
        })
    });
}
}

public record TransitionStateRequest(string TargetState, string? Actor = null, string? Reason = null);
public record RejectRequest(string Reason);
