# ExperimentFramework.Governance

Provides first-class governance and change-management primitives for experiments and rollouts, including lifecycle management, approval gates, configuration versioning, and policy-as-code guardrails.

## Features

- **Experiment Lifecycle Management** - Explicit state transitions with validation and audit trails
- **Approval Gates** - Configurable approval requirements with separation of duties
- **Configuration Versioning** - Immutable versions with diff, rollback, and attribution
- **Policy-as-Code Guardrails** - Declarative policies that constrain experiment behavior

All features are headless, API-driven, and designed to integrate with existing systems.

## Installation

```bash
dotnet add package ExperimentFramework.Governance
```

## Quick Start

### 1. Register Governance Services

```csharp
builder.Services.AddExperimentGovernance(governance =>
{
    // Configure automatic approval for draft → pending
    governance.WithAutomaticApproval(
        ExperimentLifecycleState.Draft,
        ExperimentLifecycleState.PendingApproval);

    // Require admin role for approval → running
    governance.WithRoleBasedApproval(
        ExperimentLifecycleState.Approved,
        ExperimentLifecycleState.Running,
        "admin", "operator");

    // Add traffic limit policy
    governance.WithTrafficLimitPolicy(
        maxTrafficPercentage: 10.0,
        minStableTime: TimeSpan.FromMinutes(30));

    // Add error rate policy
    governance.WithErrorRatePolicy(maxErrorRate: 0.05);
});
```

### 2. Use Lifecycle Manager

```csharp
public class ExperimentController
{
    private readonly ILifecycleManager _lifecycleManager;
    
    public async Task<IActionResult> StartExperiment(string experimentName)
    {
        // Transition through lifecycle states
        await _lifecycleManager.TransitionAsync(
            experimentName,
            ExperimentLifecycleState.PendingApproval,
            actor: User.Identity.Name,
            reason: "Ready for review");
        
        // Check allowed transitions
        var allowed = _lifecycleManager.GetAllowedTransitions(experimentName);
        
        // Get current state
        var currentState = _lifecycleManager.GetState(experimentName);
        
        return Ok(new { currentState, allowed });
    }
}
```

### 3. Manage Versions

```csharp
public class ConfigurationController
{
    private readonly IVersionManager _versionManager;
    
    public async Task<IActionResult> UpdateConfiguration(
        string experimentName,
        ExperimentConfig config)
    {
        // Create new version
        var version = await _versionManager.CreateVersionAsync(
            experimentName,
            config,
            actor: User.Identity.Name,
            changeDescription: "Updated traffic percentage");
        
        // Get diff between versions
        var diff = _versionManager.GetDiff(experimentName, 1, 2);
        
        // Rollback to previous version if needed
        var rolledBack = await _versionManager.RollbackToVersionAsync(
            experimentName,
            targetVersion: 1,
            actor: User.Identity.Name);
        
        return Ok(version);
    }
}
```

### 4. Evaluate Policies

```csharp
public class PolicyController
{
    private readonly IPolicyEvaluator _policyEvaluator;
    
    public async Task<IActionResult> CheckPolicies(string experimentName)
    {
        var context = new PolicyContext
        {
            ExperimentName = experimentName,
            CurrentState = ExperimentLifecycleState.Running,
            Telemetry = new Dictionary<string, object>
            {
                ["trafficPercentage"] = 25.0,
                ["errorRate"] = 0.03,
                ["runningDuration"] = TimeSpan.FromHours(2)
            }
        };
        
        // Evaluate all policies
        var results = await _policyEvaluator.EvaluateAllAsync(context);
        
        // Check if critical policies are satisfied
        var canProceed = await _policyEvaluator.AreAllCriticalPoliciesCompliantAsync(context);
        
        return Ok(new { canProceed, results });
    }
}
```

## Experiment Lifecycle States

Experiments progress through well-defined states:

```
Draft → PendingApproval → Approved → Running → Ramping → Archived
                              ↓           ↓         ↓
                          Rejected    Paused  RolledBack
```

### State Transitions

- **Draft**: Initial state, experiment is being defined
- **PendingApproval**: Submitted for review
- **Approved**: Approved and ready to start
- **Running**: Actively running with stable traffic
- **Ramping**: Gradually increasing traffic allocation
- **Paused**: Temporarily suspended
- **RolledBack**: Reverted due to issues
- **Archived**: Completed and archived (terminal state)
- **Rejected**: Rejected during review

## Approval Gates

Configure approval requirements for lifecycle transitions:

### Automatic Approval

```csharp
governance.WithAutomaticApproval(
    fromState: null, // Any state
    toState: ExperimentLifecycleState.Draft);
```

### Manual Approval

```csharp
governance.WithManualApproval(
    ExperimentLifecycleState.PendingApproval,
    ExperimentLifecycleState.Approved);

// Later, record approval
var manualGate = serviceProvider.GetRequiredService<ManualApprovalGate>();
manualGate.RecordApproval(
    "my-experiment",
    ExperimentLifecycleState.Approved,
    ApprovalResult.Approved("approver@example.com", "LGTM"));
```

### Role-Based Approval

```csharp
governance.WithRoleBasedApproval(
    ExperimentLifecycleState.Approved,
    ExperimentLifecycleState.Running,
    "admin", "operator", "sre");
```

### Custom Approval Gates

```csharp
public class JiraApprovalGate : IApprovalGate
{
    public string Name => "JiraTicketApproval";
    
    public async Task<ApprovalResult> EvaluateAsync(
        ApprovalContext context,
        CancellationToken cancellationToken)
    {
        // Check if JIRA ticket is approved
        var ticketId = context.Metadata?["jiraTicket"] as string;
        var isApproved = await CheckJiraTicketAsync(ticketId);
        
        return isApproved
            ? ApprovalResult.Approved("jira-bot", $"Ticket {ticketId} approved")
            : ApprovalResult.Pending($"Ticket {ticketId} not yet approved");
    }
}

// Register
governance.WithApprovalGate(
    ExperimentLifecycleState.PendingApproval,
    ExperimentLifecycleState.Approved,
    new JiraApprovalGate());
```

## Policy-as-Code

Define declarative policies that constrain experiment behavior:

### Built-in Policies

#### Traffic Limit Policy

```csharp
// Don't exceed 10% traffic until experiment runs for 30 minutes
governance.WithTrafficLimitPolicy(
    maxTrafficPercentage: 10.0,
    minStableTime: TimeSpan.FromMinutes(30));
```

#### Error Rate Policy

```csharp
// Automatically flag if error rate exceeds 5%
governance.WithErrorRatePolicy(maxErrorRate: 0.05);
```

#### Time Window Policy

```csharp
// Only allow changes during business hours
governance.WithTimeWindowPolicy(
    allowedStartTime: TimeSpan.FromHours(9),  // 09:00
    allowedEndTime: TimeSpan.FromHours(17));  // 17:00
```

#### Conflict Prevention Policy

```csharp
// Prevent conflicting experiments from running simultaneously
governance.WithConflictPreventionPolicy(
    "checkout-redesign",
    "payment-flow-v2",
    "shipping-calculator-update");
```

### Custom Policies

```csharp
public class LatencyPolicy : IExperimentPolicy
{
    private readonly double _maxP95Latency;
    
    public string Name => "P95LatencyThreshold";
    public string Description => $"Enforces P95 latency < {_maxP95Latency}ms";
    
    public Task<PolicyEvaluationResult> EvaluateAsync(
        PolicyContext context,
        CancellationToken cancellationToken)
    {
        if (context.Telemetry?.TryGetValue("p95Latency", out var latencyObj) is true &&
            latencyObj is double latency &&
            latency > _maxP95Latency)
        {
            return Task.FromResult(new PolicyEvaluationResult
            {
                IsCompliant = false,
                PolicyName = Name,
                Reason = $"P95 latency {latency}ms exceeds limit {_maxP95Latency}ms",
                Severity = PolicyViolationSeverity.Critical
            });
        }
        
        return Task.FromResult(new PolicyEvaluationResult
        {
            IsCompliant = true,
            PolicyName = Name
        });
    }
}

// Register
governance.WithPolicy(new LatencyPolicy(maxP95Latency: 500));
```

## Configuration Versioning

Track configuration changes with immutable versions:

```csharp
// Create version
var v1 = await versionManager.CreateVersionAsync(
    "checkout-experiment",
    new { TrafficPercent = 10, Enabled = true },
    actor: "alice@example.com",
    changeDescription: "Initial rollout at 10%",
    lifecycleState: ExperimentLifecycleState.Running);

// Update configuration
var v2 = await versionManager.CreateVersionAsync(
    "checkout-experiment",
    new { TrafficPercent = 50, Enabled = true },
    actor: "bob@example.com",
    changeDescription: "Increased to 50%");

// Get diff
var diff = versionManager.GetDiff("checkout-experiment", 1, 2);
foreach (var change in diff.Changes)
{
    Console.WriteLine($"{change.Type}: {change.Path}");
    Console.WriteLine($"  Old: {change.OldValue}");
    Console.WriteLine($"  New: {change.NewValue}");
}

// Rollback
var v3 = await versionManager.RollbackToVersionAsync(
    "checkout-experiment",
    targetVersion: 1,
    actor: "charlie@example.com");
```

## Admin API Endpoints

Map governance endpoints in your ASP.NET Core application:

```csharp
var app = builder.Build();

// Map governance endpoints
app.MapGovernanceAdminApi("/api/governance");
```

### Available Endpoints

#### Lifecycle
- `GET /api/governance/{experimentName}/lifecycle/state` - Get current state
- `GET /api/governance/{experimentName}/lifecycle/history` - Get state history
- `GET /api/governance/{experimentName}/lifecycle/allowed-transitions` - Get allowed transitions
- `POST /api/governance/{experimentName}/lifecycle/transition` - Transition to new state

#### Versions
- `GET /api/governance/{experimentName}/versions` - List all versions
- `GET /api/governance/{experimentName}/versions/latest` - Get latest version
- `GET /api/governance/{experimentName}/versions/{versionNumber}` - Get specific version
- `GET /api/governance/{experimentName}/versions/diff?fromVersion=1&toVersion=2` - Get diff
- `POST /api/governance/{experimentName}/versions` - Create new version
- `POST /api/governance/{experimentName}/versions/rollback` - Rollback to version

#### Policies
- `POST /api/governance/{experimentName}/policies/evaluate` - Evaluate policies

#### Approvals
- `POST /api/governance/{experimentName}/approvals/evaluate` - Evaluate approvals

## Integration with Audit System

All lifecycle transitions and version changes are automatically recorded in the audit trail:

```csharp
// Audit events are emitted automatically
services.AddSingleton<IAuditSink, MyAuditSink>();

public class MyAuditSink : IAuditSink
{
    public async Task RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        // Store lifecycle transitions, version changes, etc.
        await _database.SaveAsync(auditEvent);
    }
}
```

## Best Practices

1. **Define Clear Lifecycle Rules** - Use the default state machine or customize it for your organization
2. **Separate Concerns** - Different roles should handle different transitions (developer → propose, operator → activate, SRE → ramp)
3. **Version Everything** - Create a version for every significant configuration change
4. **Use Policies for Safety** - Define critical policies that block dangerous operations
5. **Audit Everything** - Integrate with your audit system to maintain compliance
6. **Test Transitions** - Validate lifecycle transitions in your test environment first

## Example: Complete Workflow

```csharp
// 1. Developer creates experiment in Draft state
var experimentName = "new-checkout-flow";
await lifecycleManager.TransitionAsync(
    experimentName,
    ExperimentLifecycleState.PendingApproval,
    actor: "developer@example.com",
    reason: "Ready for review");

// 2. Version initial configuration
await versionManager.CreateVersionAsync(
    experimentName,
    new { TrafficPercent = 5, Features = new[] { "express-checkout" } },
    actor: "developer@example.com",
    changeDescription: "Initial configuration");

// 3. Operator approves
await lifecycleManager.TransitionAsync(
    experimentName,
    ExperimentLifecycleState.Approved,
    actor: "operator@example.com",
    reason: "Approved for production");

// 4. Check policies before activation
var policyContext = new PolicyContext
{
    ExperimentName = experimentName,
    CurrentState = ExperimentLifecycleState.Approved,
    TargetState = ExperimentLifecycleState.Running
};
var policiesOk = await policyEvaluator.AreAllCriticalPoliciesCompliantAsync(policyContext);

// 5. Start experiment
if (policiesOk)
{
    await lifecycleManager.TransitionAsync(
        experimentName,
        ExperimentLifecycleState.Running,
        actor: "operator@example.com");
}

// 6. Ramp up traffic
await versionManager.CreateVersionAsync(
    experimentName,
    new { TrafficPercent = 50, Features = new[] { "express-checkout" } },
    actor: "sre@example.com",
    changeDescription: "Ramping to 50%");

await lifecycleManager.TransitionAsync(
    experimentName,
    ExperimentLifecycleState.Ramping,
    actor: "sre@example.com");

// 7. Monitor and react to issues
if (errorRateTooHigh)
{
    // Rollback configuration
    await versionManager.RollbackToVersionAsync(experimentName, 1, "sre@example.com");
    
    // Update lifecycle state
    await lifecycleManager.TransitionAsync(
        experimentName,
        ExperimentLifecycleState.RolledBack,
        actor: "sre@example.com",
        reason: "Error rate exceeded threshold");
}
```

## License

MIT
