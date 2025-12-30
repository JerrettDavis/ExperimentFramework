using ExperimentFramework.Audit;
using ExperimentFramework.Governance;
using ExperimentFramework.Governance.Persistence;
using ExperimentFramework.Governance.Persistence.Sql;
using ExperimentFramework.Governance.Policy;
using ExperimentFramework.Governance.Versioning;
using ExperimentFramework.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddFeatureManagement();

// Register audit sink
builder.Services.AddSingleton<IAuditSink, ConsoleAuditSink>();

// Register governance with approval gates, policies, and persistence
builder.Services.AddExperimentGovernance(gov =>
{
    // Configure persistence backplane
    // For demo purposes, using in-memory EF Core
    // In production, use SQL Server or PostgreSQL with a real connection string
    gov.UsePersistence(p =>
    {
        p.AddSqlGovernancePersistence(options =>
            options.UseInMemoryDatabase("GovernanceDemo"));
    });
    
    // Automatic approval for Draft → PendingApproval
    gov.WithAutomaticApproval(
        ExperimentLifecycleState.Draft,
        ExperimentLifecycleState.PendingApproval);
    
    // Role-based approval for activation
    gov.WithRoleBasedApproval(
        ExperimentLifecycleState.Approved,
        ExperimentLifecycleState.Running,
        "operator", "sre");
    
    // SRE only for ramping
    gov.WithRoleBasedApproval(
        ExperimentLifecycleState.Running,
        ExperimentLifecycleState.Ramping,
        "sre");
    
    // Add safety policies
    gov.WithTrafficLimitPolicy(
        maxTrafficPercentage: 10.0,
        minStableTime: TimeSpan.FromMinutes(30));
    
    gov.WithErrorRatePolicy(maxErrorRate: 0.05);
    
    gov.WithTimeWindowPolicy(
        allowedStartTime: TimeSpan.FromHours(9),
        allowedEndTime: TimeSpan.FromHours(17));
});

// Initialize database
using (var scope = builder.Services.BuildServiceProvider().CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GovernanceDbContext>();
    dbContext.Database.EnsureCreated();
}

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Map governance API endpoints
app.MapGovernanceAdminApi("/api/governance");

// Demo endpoints
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapPost("/demo/lifecycle", async (
    string experimentName,
    string targetState,
    ILifecycleManager lifecycle) =>
{
    try
    {
        if (!Enum.TryParse<ExperimentLifecycleState>(targetState, true, out var state))
        {
            return Results.BadRequest($"Invalid state: {targetState}");
        }

        await lifecycle.TransitionAsync(
            experimentName,
            state,
            actor: "demo-user",
            reason: "Demo transition");

        return Results.Ok(new
        {
            experimentName,
            newState = state.ToString(),
            message = "Transition successful"
        });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("DemoLifecycleTransition")
.WithTags("Demo");

app.MapPost("/demo/version", async (
    string experimentName,
    object configuration,
    IVersionManager versionManager) =>
{
    var version = await versionManager.CreateVersionAsync(
        experimentName,
        configuration,
        actor: "demo-user",
        changeDescription: "Demo version creation");

    return Results.Ok(new
    {
        versionNumber = version.VersionNumber,
        experimentName = version.ExperimentName,
        createdAt = version.CreatedAt,
        createdBy = version.CreatedBy
    });
})
.WithName("DemoCreateVersion")
.WithTags("Demo");

app.MapPost("/demo/policy", async (
    string experimentName,
    PolicyRequest request,
    IPolicyEvaluator evaluator) =>
{
    var context = new PolicyContext
    {
        ExperimentName = experimentName,
        CurrentState = request.CurrentState,
        Telemetry = request.Telemetry,
        Metadata = request.Metadata
    };

    var results = await evaluator.EvaluateAllAsync(context);
    var allCritical = await evaluator.AreAllCriticalPoliciesCompliantAsync(context);

    return Results.Ok(new
    {
        experimentName,
        allCriticalPoliciesCompliant = allCritical,
        evaluations = results.Select(r => new
        {
            policyName = r.PolicyName,
            isCompliant = r.IsCompliant,
            reason = r.Reason,
            severity = r.Severity.ToString()
        })
    });
})
.WithName("DemoEvaluatePolicies")
.WithTags("Demo");

// Persistence demo endpoints
app.MapGet("/demo/persistence/state/{experimentName}", async (
    string experimentName,
    IGovernancePersistenceBackplane backplane) =>
{
    var state = await backplane.GetExperimentStateAsync(experimentName);
    
    if (state == null)
        return Results.NotFound(new { error = $"Experiment '{experimentName}' not found" });
    
    return Results.Ok(new
    {
        experimentName = state.ExperimentName,
        currentState = state.CurrentState.ToString(),
        configurationVersion = state.ConfigurationVersion,
        lastModified = state.LastModified,
        lastModifiedBy = state.LastModifiedBy,
        etag = state.ETag
    });
})
.WithName("GetPersistedState")
.WithTags("Persistence Demo");

app.MapGet("/demo/persistence/history/{experimentName}", async (
    string experimentName,
    IGovernancePersistenceBackplane backplane) =>
{
    var transitions = await backplane.GetStateTransitionHistoryAsync(experimentName);
    
    return Results.Ok(new
    {
        experimentName,
        totalTransitions = transitions.Count,
        history = transitions.Select(t => new
        {
            transitionId = t.TransitionId,
            fromState = t.FromState.ToString(),
            toState = t.ToState.ToString(),
            timestamp = t.Timestamp,
            actor = t.Actor,
            reason = t.Reason
        })
    });
})
.WithName("GetStateHistory")
.WithTags("Persistence Demo");

app.MapGet("/demo/persistence/versions/{experimentName}", async (
    string experimentName,
    IGovernancePersistenceBackplane backplane) =>
{
    var versions = await backplane.GetAllConfigurationVersionsAsync(experimentName);
    
    return Results.Ok(new
    {
        experimentName,
        totalVersions = versions.Count,
        versions = versions.Select(v => new
        {
            versionNumber = v.VersionNumber,
            createdAt = v.CreatedAt,
            createdBy = v.CreatedBy,
            changeDescription = v.ChangeDescription,
            isRollback = v.IsRollback,
            rolledBackFrom = v.RolledBackFrom,
            lifecycleState = v.LifecycleState?.ToString()
        })
    });
})
.WithName("GetConfigurationVersions")
.WithTags("Persistence Demo");

app.MapGet("/demo/persistence/approvals/{experimentName}", async (
    string experimentName,
    IGovernancePersistenceBackplane backplane) =>
{
    var approvals = await backplane.GetApprovalRecordsAsync(experimentName);
    
    return Results.Ok(new
    {
        experimentName,
        totalApprovals = approvals.Count,
        approvals = approvals.Select(a => new
        {
            approvalId = a.ApprovalId,
            transitionId = a.TransitionId,
            fromState = a.FromState?.ToString(),
            toState = a.ToState.ToString(),
            isApproved = a.IsApproved,
            approver = a.Approver,
            reason = a.Reason,
            timestamp = a.Timestamp,
            gateName = a.GateName
        })
    });
})
.WithName("GetApprovalHistory")
.WithTags("Persistence Demo");

app.Run();

// Request models
public record PolicyRequest(
    ExperimentLifecycleState? CurrentState,
    Dictionary<string, object>? Telemetry,
    Dictionary<string, object>? Metadata);

// Console audit sink for demo
public class ConsoleAuditSink : IAuditSink
{
    public ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("=== AUDIT EVENT ===");
        Console.WriteLine($"EventId: {auditEvent.EventId}");
        Console.WriteLine($"Timestamp: {auditEvent.Timestamp}");
        Console.WriteLine($"EventType: {auditEvent.EventType}");
        Console.WriteLine($"ExperimentName: {auditEvent.ExperimentName}");
        Console.WriteLine($"Actor: {auditEvent.Actor}");
        
        if (auditEvent.Details != null)
        {
            Console.WriteLine("Details:");
            foreach (var kvp in auditEvent.Details)
            {
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
            }
        }
        
        Console.WriteLine("==================");
        Console.WriteLine();
        
        return ValueTask.CompletedTask;
    }
}
