using ExperimentFramework.Audit;
using ExperimentFramework.Governance;
using ExperimentFramework.Governance.Policy;
using ExperimentFramework.Governance.Versioning;
using ExperimentFramework.Admin;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddFeatureManagement();

// Register audit sink
builder.Services.AddSingleton<IAuditSink, ConsoleAuditSink>();

// Register governance with approval gates and policies
builder.Services.AddExperimentGovernance(gov =>
{
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
