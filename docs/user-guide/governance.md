# Experiment Governance

The Governance package provides first-class lifecycle management, approval gates, configuration versioning, and policy-as-code guardrails for experiments. This enables **preventative governance** rather than forensic-only audit trails, allowing organizations to safely scale experimentation with proper controls and oversight.

## Why Governance?

As experimentation scales beyond simple feature flags into coordinated rollouts and long-running trials, teams need:

- **Clear lifecycle states** - Explicit visibility into whether an experiment is a draft, running, or archived
- **Approval workflows** - Separation of duties between who proposes and who activates experiments
- **Configuration history** - Ability to see what changed, when, and why, with rollback capabilities
- **Safety guardrails** - Automated policies that prevent dangerous configurations (e.g., traffic limits, error thresholds)

Without governance, these concerns become operational risks that must be manually reimplemented in each application.

## Installation

```bash
dotnet add package ExperimentFramework.Governance
```

## Quick Start

### Basic Setup

```csharp
using ExperimentFramework.Governance;

var builder = WebApplication.CreateBuilder(args);

// Register governance services
builder.Services.AddExperimentGovernance(gov =>
{
    // Require approval for activation
    gov.WithRoleBasedApproval(
        ExperimentLifecycleState.Approved,
        ExperimentLifecycleState.Running,
        "admin", "operator");

    // Add safety policies
    gov.WithTrafficLimitPolicy(10.0, TimeSpan.FromMinutes(30));
    gov.WithErrorRatePolicy(0.05);
});

var app = builder.Build();

// Optionally expose governance API endpoints
app.MapGovernanceAdminApi("/api/governance");

app.Run();
```

### Managing Lifecycle

```csharp
public class ExperimentController
{
    private readonly ILifecycleManager _lifecycle;
    
    public ExperimentController(ILifecycleManager lifecycle)
    {
        _lifecycle = lifecycle;
    }
    
    public async Task<IActionResult> SubmitForReview(string experimentName)
    {
        // Transition from Draft to PendingApproval
        await _lifecycle.TransitionAsync(
            experimentName,
            ExperimentLifecycleState.PendingApproval,
            actor: User.Identity.Name,
            reason: "Ready for operator review");
        
        return Ok();
    }
}
```

## Lifecycle Management

### State Machine

Experiments progress through well-defined states:

```
Draft → PendingApproval → Approved → Running → Ramping → Archived
           ↓                 ↓          ↓         ↓
        Rejected          Paused    RolledBack
```

### Lifecycle States

| State | Description | Typical Actor |
|-------|-------------|---------------|
| **Draft** | Experiment is being created/modified | Developer |
| **PendingApproval** | Submitted for review | Developer |
| **Approved** | Approved but not yet active | Operator/Manager |
| **Running** | Actively running with stable traffic | Operator |
| **Ramping** | Gradually increasing traffic allocation | SRE |
| **Paused** | Temporarily suspended | Any authorized user |
| **RolledBack** | Reverted due to issues | SRE/Operator |
| **Rejected** | Not approved for activation | Operator/Manager |
| **Archived** | Completed and archived (terminal) | System/Manager |

### Valid Transitions

The lifecycle manager enforces valid state transitions:

```csharp
// From Draft
Draft → PendingApproval  ✓
Draft → Archived         ✓

// From PendingApproval
PendingApproval → Approved   ✓
PendingApproval → Rejected   ✓
PendingApproval → Draft      ✓  (for revisions)

// From Approved
Approved → Running   ✓
Approved → Ramping   ✓
Approved → Archived  ✓

// From Running
Running → Ramping      ✓
Running → Paused       ✓
Running → RolledBack   ✓
Running → Archived     ✓

// etc.
```

Invalid transitions throw `InvalidOperationException`:

```csharp
// This will fail - can't go directly from Draft to Running
await _lifecycle.TransitionAsync(
    "my-experiment",
    ExperimentLifecycleState.Running); // ❌ Throws InvalidOperationException
```

### Using Lifecycle Manager

#### Check Current State

```csharp
var state = _lifecycle.GetState("checkout-experiment");
if (state == ExperimentLifecycleState.Running)
{
    // Experiment is active
}
```

#### Get Transition History

```csharp
var history = _lifecycle.GetHistory("checkout-experiment");
foreach (var transition in history)
{
    Console.WriteLine($"{transition.FromState} → {transition.ToState}");
    Console.WriteLine($"  By: {transition.Actor} at {transition.Timestamp}");
    Console.WriteLine($"  Reason: {transition.Reason}");
}
```

#### Check Allowed Transitions

```csharp
var currentState = _lifecycle.GetState("checkout-experiment");
var allowedStates = _lifecycle.GetAllowedTransitions("checkout-experiment");

Console.WriteLine($"From {currentState}, can transition to:");
foreach (var state in allowedStates)
{
    Console.WriteLine($"  - {state}");
}
```

#### Perform Transition with Metadata

```csharp
await _lifecycle.TransitionAsync(
    "checkout-experiment",
    ExperimentLifecycleState.Running,
    actor: "sre@example.com",
    reason: "Metrics stable, CPU <50%, error rate <1%",
    metadata: new Dictionary<string, object>
    {
        ["jiraTicket"] = "EXP-1234",
        ["approvedBy"] = "manager@example.com"
    });
```

## Approval Gates

Approval gates control who can perform lifecycle transitions, enabling separation of duties.

### Built-in Approval Gates

#### Automatic Approval

Always approves - useful for transitions that don't require review:

```csharp
gov.WithAutomaticApproval(
    fromState: ExperimentLifecycleState.Draft,
    toState: ExperimentLifecycleState.PendingApproval);
```

#### Manual Approval

Requires explicit external approval:

```csharp
gov.WithManualApproval(
    fromState: ExperimentLifecycleState.PendingApproval,
    toState: ExperimentLifecycleState.Approved);

// Later, record approval from external system
var gate = serviceProvider.GetRequiredService<ManualApprovalGate>();
gate.RecordApproval(
    "checkout-experiment",
    ExperimentLifecycleState.Approved,
    ApprovalResult.Approved("manager@example.com", "Approved via Jira EXP-1234"));
```

#### Role-Based Approval

Checks if the actor has required role:

```csharp
gov.WithRoleBasedApproval(
    fromState: ExperimentLifecycleState.Approved,
    toState: ExperimentLifecycleState.Running,
    allowedRoles: "admin", "operator", "sre");
```

The role must be provided in the transition metadata:

```csharp
await _lifecycle.TransitionAsync(
    "checkout-experiment",
    ExperimentLifecycleState.Running,
    actor: "alice@example.com",
    metadata: new Dictionary<string, object>
    {
        ["actorRole"] = "operator"  // Required for RoleBasedApprovalGate
    });
```

### Custom Approval Gates

Create custom gates for integration with external systems:

```csharp
public class JiraApprovalGate : IApprovalGate
{
    private readonly IJiraClient _jira;
    
    public string Name => "JiraTicketApproval";
    
    public JiraApprovalGate(IJiraClient jira)
    {
        _jira = jira;
    }
    
    public async Task<ApprovalResult> EvaluateAsync(
        ApprovalContext context,
        CancellationToken cancellationToken)
    {
        // Extract ticket ID from metadata
        if (!context.Metadata?.TryGetValue("jiraTicket", out var ticketObj) is true ||
            ticketObj is not string ticketId)
        {
            return ApprovalResult.Rejected(
                context.Actor,
                "Missing JIRA ticket reference");
        }
        
        // Check ticket status
        var ticket = await _jira.GetTicketAsync(ticketId, cancellationToken);
        if (ticket.Status == "Approved")
        {
            return ApprovalResult.Approved(
                ticket.ApprovedBy,
                $"Approved via JIRA ticket {ticketId}");
        }
        
        return ApprovalResult.Pending($"JIRA ticket {ticketId} not yet approved");
    }
}

// Register
services.AddSingleton<IJiraClient, JiraClient>();
services.AddExperimentGovernance(gov =>
{
    gov.WithApprovalGate(
        ExperimentLifecycleState.PendingApproval,
        ExperimentLifecycleState.Approved,
        new JiraApprovalGate(sp.GetRequiredService<IJiraClient>()));
});
```

### Multi-Stage Approvals

Configure different approvals for different transitions:

```csharp
services.AddExperimentGovernance(gov =>
{
    // Developer can submit for review (automatic)
    gov.WithAutomaticApproval(
        ExperimentLifecycleState.Draft,
        ExperimentLifecycleState.PendingApproval);
    
    // Manager must approve design (manual)
    gov.WithManualApproval(
        ExperimentLifecycleState.PendingApproval,
        ExperimentLifecycleState.Approved);
    
    // Operator/SRE can activate (role-based)
    gov.WithRoleBasedApproval(
        ExperimentLifecycleState.Approved,
        ExperimentLifecycleState.Running,
        "operator", "sre");
    
    // SRE only can ramp traffic (role-based)
    gov.WithRoleBasedApproval(
        ExperimentLifecycleState.Running,
        ExperimentLifecycleState.Ramping,
        "sre");
});
```

## Configuration Versioning

Track every configuration change with immutable versions.

### Creating Versions

```csharp
var version = await _versionManager.CreateVersionAsync(
    "checkout-experiment",
    configuration: new
    {
        TrafficPercentage = 10,
        EnabledFeatures = new[] { "express-checkout", "save-payment" },
        Targeting = new { Region = "US-WEST" }
    },
    actor: "developer@example.com",
    changeDescription: "Initial rollout at 10% in US-WEST",
    lifecycleState: ExperimentLifecycleState.Running);

Console.WriteLine($"Created version {version.VersionNumber}");
```

### Viewing Version History

```csharp
var versions = _versionManager.GetAllVersions("checkout-experiment");
foreach (var v in versions)
{
    Console.WriteLine($"Version {v.VersionNumber}:");
    Console.WriteLine($"  Created: {v.CreatedAt} by {v.CreatedBy}");
    Console.WriteLine($"  Changes: {v.ChangeDescription}");
    Console.WriteLine($"  State: {v.LifecycleState}");
}
```

### Getting Latest Version

```csharp
var latest = _versionManager.GetLatestVersion("checkout-experiment");
Console.WriteLine($"Current version: {latest.VersionNumber}");
Console.WriteLine($"Configuration: {JsonSerializer.Serialize(latest.Configuration)}");
```

### Comparing Versions (Diff)

```csharp
var diff = _versionManager.GetDiff("checkout-experiment", fromVersion: 1, toVersion: 2);
if (diff != null)
{
    Console.WriteLine($"Changes from v{diff.FromVersion} to v{diff.ToVersion}:");
    foreach (var change in diff.Changes)
    {
        Console.WriteLine($"  {change.Type}: {change.Path}");
        Console.WriteLine($"    Old: {change.OldValue}");
        Console.WriteLine($"    New: {change.NewValue}");
    }
}
```

### Rolling Back

```csharp
// Rollback to version 1 (creates a new version with v1's config)
var rolledBack = await _versionManager.RollbackToVersionAsync(
    "checkout-experiment",
    targetVersion: 1,
    actor: "sre@example.com");

Console.WriteLine($"Rolled back to v1, created new version {rolledBack.VersionNumber}");
```

### Integration with Lifecycle

Versions automatically capture lifecycle state:

```csharp
// Create version when transitioning
await _lifecycle.TransitionAsync(
    "checkout-experiment",
    ExperimentLifecycleState.Running,
    actor: "operator@example.com");

var version = await _versionManager.CreateVersionAsync(
    "checkout-experiment",
    currentConfig,
    actor: "operator@example.com",
    changeDescription: "Activated experiment",
    lifecycleState: ExperimentLifecycleState.Running);

// Later, view versions filtered by state
var versions = _versionManager.GetAllVersions("checkout-experiment");
var runningVersions = versions.Where(v => v.LifecycleState == ExperimentLifecycleState.Running);
```

## Policy-as-Code Guardrails

Policies define constraints that experiments must satisfy, preventing dangerous configurations.

### Built-in Policies

#### Traffic Limit Policy

Prevents exceeding a traffic percentage until stability requirements are met:

```csharp
gov.WithTrafficLimitPolicy(
    maxTrafficPercentage: 10.0,
    minStableTime: TimeSpan.FromMinutes(30));
```

This policy checks telemetry:

```csharp
var context = new PolicyContext
{
    ExperimentName = "checkout-experiment",
    CurrentState = ExperimentLifecycleState.Running,
    Telemetry = new Dictionary<string, object>
    {
        ["trafficPercentage"] = 5.0,        // Current: 5%
        ["runningDuration"] = TimeSpan.FromMinutes(10)  // Running for 10 min
    }
};

var results = await _policyEvaluator.EvaluateAllAsync(context);
// Result: Compliant (5% < 10%, but can't exceed 10% until 30 min stable)
```

#### Error Rate Policy

Flags experiments with high error rates:

```csharp
gov.WithErrorRatePolicy(maxErrorRate: 0.05); // 5% max
```

Usage:

```csharp
var context = new PolicyContext
{
    ExperimentName = "checkout-experiment",
    Telemetry = new Dictionary<string, object>
    {
        ["errorRate"] = 0.08  // 8% error rate
    }
};

var results = await _policyEvaluator.EvaluateAllAsync(context);
// Result: Not compliant - error rate exceeds 5%
```

#### Time Window Policy

Restricts operations to business hours:

```csharp
gov.WithTimeWindowPolicy(
    allowedStartTime: TimeSpan.FromHours(9),   // 09:00
    allowedEndTime: TimeSpan.FromHours(17));   // 17:00
```

This policy automatically checks current UTC time.

#### Conflict Prevention Policy

Prevents multiple experiments from running on the same surface:

```csharp
gov.WithConflictPreventionPolicy(
    "checkout-redesign",
    "payment-flow-v2",
    "shipping-calculator-update");
```

Usage:

```csharp
var context = new PolicyContext
{
    ExperimentName = "new-checkout-test",
    Metadata = new Dictionary<string, object>
    {
        ["runningExperiments"] = new[] { "checkout-redesign", "homepage-banner" }
    }
};

var results = await _policyEvaluator.EvaluateAllAsync(context);
// Result: Not compliant - conflicts with "checkout-redesign"
```

### Custom Policies

Create domain-specific policies:

```csharp
public class LatencyPolicy : IExperimentPolicy
{
    private readonly double _maxP95Latency;
    
    public LatencyPolicy(double maxP95Latency)
    {
        _maxP95Latency = maxP95Latency;
    }
    
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
            PolicyName = Name,
            Reason = "Latency within acceptable range"
        });
    }
}

// Register
services.AddExperimentGovernance(gov =>
{
    gov.WithPolicy(new LatencyPolicy(maxP95Latency: 500));
});
```

### Policy Evaluation

#### Evaluate All Policies

```csharp
var results = await _policyEvaluator.EvaluateAllAsync(context);
foreach (var result in results)
{
    Console.WriteLine($"Policy: {result.PolicyName}");
    Console.WriteLine($"  Compliant: {result.IsCompliant}");
    Console.WriteLine($"  Reason: {result.Reason}");
    Console.WriteLine($"  Severity: {result.Severity}");
}
```

#### Check Critical Policies

```csharp
var canProceed = await _policyEvaluator.AreAllCriticalPoliciesCompliantAsync(context);
if (!canProceed)
{
    // Block the operation
    throw new InvalidOperationException("Critical policy violations detected");
}
```

### Policy Severity Levels

Policies can have different severity levels:

- **Info** - Informational only, no action required
- **Warning** - Action recommended but not required
- **Error** - Action should be taken but operation can proceed
- **Critical** - Operation must be blocked

```csharp
public class CustomPolicy : IExperimentPolicy
{
    public Task<PolicyEvaluationResult> EvaluateAsync(
        PolicyContext context,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new PolicyEvaluationResult
        {
            IsCompliant = false,
            PolicyName = Name,
            Reason = "Issue detected",
            Severity = PolicyViolationSeverity.Critical  // This will block
        });
    }
}
```

## Governance Admin API

Expose governance operations via REST endpoints.

### Setup

```csharp
var app = builder.Build();
app.MapGovernanceAdminApi("/api/governance");
app.Run();
```

### Lifecycle Endpoints

#### Get Current State

```http
GET /api/governance/{experimentName}/lifecycle/state
```

Response:
```json
{
  "experimentName": "checkout-experiment",
  "state": "Running",
  "allowedTransitions": ["Ramping", "Paused", "RolledBack", "Archived"]
}
```

#### Get State History

```http
GET /api/governance/{experimentName}/lifecycle/history
```

Response:
```json
{
  "experimentName": "checkout-experiment",
  "transitions": [
    {
      "from": "Draft",
      "to": "PendingApproval",
      "timestamp": "2024-01-15T10:30:00Z",
      "actor": "dev@example.com",
      "reason": "Ready for review"
    },
    {
      "from": "PendingApproval",
      "to": "Approved",
      "timestamp": "2024-01-15T14:00:00Z",
      "actor": "manager@example.com",
      "reason": "Approved"
    }
  ]
}
```

#### Transition State

```http
POST /api/governance/{experimentName}/lifecycle/transition
Content-Type: application/json

{
  "targetState": "Running",
  "actor": "operator@example.com",
  "reason": "Metrics stable"
}
```

### Version Endpoints

#### List All Versions

```http
GET /api/governance/{experimentName}/versions
```

#### Get Latest Version

```http
GET /api/governance/{experimentName}/versions/latest
```

#### Get Version Diff

```http
GET /api/governance/{experimentName}/versions/diff?fromVersion=1&toVersion=2
```

#### Create Version

```http
POST /api/governance/{experimentName}/versions
Content-Type: application/json

{
  "configuration": {
    "trafficPercentage": 50,
    "features": ["express-checkout"]
  },
  "actor": "sre@example.com",
  "changeDescription": "Ramping to 50%",
  "lifecycleState": "Ramping"
}
```

#### Rollback to Version

```http
POST /api/governance/{experimentName}/versions/rollback
Content-Type: application/json

{
  "targetVersion": 1,
  "actor": "sre@example.com"
}
```

### Policy Endpoints

#### Evaluate Policies

```http
POST /api/governance/{experimentName}/policies/evaluate
Content-Type: application/json

{
  "currentState": "Running",
  "telemetry": {
    "trafficPercentage": 25.0,
    "errorRate": 0.03,
    "runningDuration": "01:30:00"
  }
}
```

Response:
```json
{
  "experimentName": "checkout-experiment",
  "allCriticalPoliciesCompliant": true,
  "evaluations": [
    {
      "policyName": "TrafficLimit",
      "isCompliant": true,
      "reason": "Traffic 25% within limit 100%",
      "severity": "Info"
    },
    {
      "policyName": "ErrorRate",
      "isCompliant": true,
      "reason": "Error rate 3% within limit 5%",
      "severity": "Info"
    }
  ]
}
```

### Approval Endpoints

#### Evaluate Approvals

```http
POST /api/governance/{experimentName}/approvals/evaluate
Content-Type: application/json

{
  "currentState": "Approved",
  "targetState": "Running",
  "actor": "operator@example.com",
  "metadata": {
    "actorRole": "operator"
  }
}
```

## Integration with Audit System

All lifecycle transitions and version changes are automatically recorded in the audit trail:

```csharp
public class MyAuditSink : IAuditSink
{
    private readonly IDatabase _db;
    
    public async Task RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        // Lifecycle transitions create ExperimentModified events
        if (auditEvent.EventType == AuditEventType.ExperimentModified)
        {
            if (auditEvent.Details?.ContainsKey("lifecycleTransition") is true)
            {
                // This is a lifecycle transition
                var transition = auditEvent.Details["lifecycleTransition"];
                await _db.SaveLifecycleTransition(transition);
            }
            else if (auditEvent.Details?.ContainsKey("versionNumber") is true)
            {
                // This is a version change
                var versionNumber = auditEvent.Details["versionNumber"];
                await _db.SaveVersionChange(versionNumber);
            }
        }
        
        // Store all audit events
        await _db.SaveAuditEvent(auditEvent);
    }
}

// Register
services.AddSingleton<IAuditSink, MyAuditSink>();
```

## Complete Workflow Example

Here's a complete end-to-end workflow showing all governance features:

```csharp
// 1. Developer creates experiment in Draft state (implicit initial state)
var experimentName = "checkout-redesign";

// 2. Create initial configuration version
await versionManager.CreateVersionAsync(
    experimentName,
    new
    {
        TrafficPercentage = 5,
        EnabledFeatures = new[] { "express-checkout" },
        Targeting = new { Regions = new[] { "US-WEST" } }
    },
    actor: "developer@example.com",
    changeDescription: "Initial configuration - 5% rollout in US-WEST",
    lifecycleState: ExperimentLifecycleState.Draft);

// 3. Submit for approval
await lifecycleManager.TransitionAsync(
    experimentName,
    ExperimentLifecycleState.PendingApproval,
    actor: "developer@example.com",
    reason: "Ready for operator review",
    metadata: new Dictionary<string, object>
    {
        ["jiraTicket"] = "EXP-1234",
        ["designDoc"] = "https://docs.example.com/checkout-redesign"
    });

// 4. Operator approves (assuming manual gate is configured)
var manualGate = serviceProvider.GetRequiredService<ManualApprovalGate>();
manualGate.RecordApproval(
    experimentName,
    ExperimentLifecycleState.Approved,
    ApprovalResult.Approved("manager@example.com", "Approved - design looks good"));

await lifecycleManager.TransitionAsync(
    experimentName,
    ExperimentLifecycleState.Approved,
    actor: "manager@example.com",
    reason: "Design approved");

// 5. Check policies before activation
var policyContext = new PolicyContext
{
    ExperimentName = experimentName,
    CurrentState = ExperimentLifecycleState.Approved,
    TargetState = ExperimentLifecycleState.Running,
    Telemetry = new Dictionary<string, object>
    {
        ["trafficPercentage"] = 5.0,
        ["errorRate"] = 0.0
    },
    Metadata = new Dictionary<string, object>
    {
        ["runningExperiments"] = new[] { "homepage-banner" }  // No conflicts
    }
};

var policiesOk = await policyEvaluator.AreAllCriticalPoliciesCompliantAsync(policyContext);

// 6. Activate experiment if policies pass
if (policiesOk)
{
    await lifecycleManager.TransitionAsync(
        experimentName,
        ExperimentLifecycleState.Running,
        actor: "operator@example.com",
        reason: "Policies satisfied, activating",
        metadata: new Dictionary<string, object>
        {
            ["actorRole"] = "operator"  // For role-based approval gate
        });
}

// 7. Monitor for 30 minutes...

// 8. Ramp up traffic if metrics are good
await versionManager.CreateVersionAsync(
    experimentName,
    new
    {
        TrafficPercentage = 25,
        EnabledFeatures = new[] { "express-checkout" },
        Targeting = new { Regions = new[] { "US-WEST", "US-EAST" } }
    },
    actor: "sre@example.com",
    changeDescription: "Ramping to 25% and expanding to US-EAST",
    lifecycleState: ExperimentLifecycleState.Ramping);

await lifecycleManager.TransitionAsync(
    experimentName,
    ExperimentLifecycleState.Ramping,
    actor: "sre@example.com",
    reason: "Metrics stable: error rate <1%, p95 latency 200ms",
    metadata: new Dictionary<string, object>
    {
        ["actorRole"] = "sre"
    });

// 9. If issues are detected, rollback
if (errorRateTooHigh)
{
    // Rollback configuration
    var rolledBackVersion = await versionManager.RollbackToVersionAsync(
        experimentName,
        targetVersion: 1,
        actor: "sre@example.com");
    
    // Update lifecycle state
    await lifecycleManager.TransitionAsync(
        experimentName,
        ExperimentLifecycleState.RolledBack,
        actor: "sre@example.com",
        reason: "Error rate exceeded 5% threshold",
        metadata: new Dictionary<string, object>
        {
            ["errorRate"] = 0.08,
            ["rollbackVersion"] = rolledBackVersion.VersionNumber
        });
}

// 10. Eventually archive the experiment
await lifecycleManager.TransitionAsync(
    experimentName,
    ExperimentLifecycleState.Archived,
    actor: "system",
    reason: "Experiment completed successfully");
```

## Best Practices

### 1. Define Clear Lifecycle Rules

Establish organizational policies for lifecycle transitions:

```csharp
// Example: Strict production workflow
services.AddExperimentGovernance(gov =>
{
    // Drafts require approval
    gov.WithManualApproval(
        ExperimentLifecycleState.PendingApproval,
        ExperimentLifecycleState.Approved);
    
    // Only operators can activate
    gov.WithRoleBasedApproval(
        ExperimentLifecycleState.Approved,
        ExperimentLifecycleState.Running,
        "operator", "sre");
    
    // Only SREs can ramp
    gov.WithRoleBasedApproval(
        ExperimentLifecycleState.Running,
        ExperimentLifecycleState.Ramping,
        "sre");
    
    // Add safety policies
    gov.WithTrafficLimitPolicy(10.0, TimeSpan.FromMinutes(30));
    gov.WithErrorRatePolicy(0.05);
    gov.WithTimeWindowPolicy(
        TimeSpan.FromHours(9),
        TimeSpan.FromHours(17));
});
```

### 2. Version Everything

Create a version for every significant configuration change:

```csharp
// Before activating
await versionManager.CreateVersionAsync(
    experimentName,
    currentConfig,
    actor: actor,
    changeDescription: "Activating experiment",
    lifecycleState: ExperimentLifecycleState.Running);

// Before ramping
await versionManager.CreateVersionAsync(
    experimentName,
    updatedConfig,
    actor: actor,
    changeDescription: "Ramping to 50%",
    lifecycleState: ExperimentLifecycleState.Ramping);
```

### 3. Use Policies for Safety

Define critical policies that must pass:

```csharp
services.AddExperimentGovernance(gov =>
{
    // Critical: Never exceed traffic without stability
    gov.WithTrafficLimitPolicy(25.0, TimeSpan.FromHours(1));
    
    // Critical: Pause if error rate spikes
    gov.WithErrorRatePolicy(0.10);
    
    // Warning: Prevent conflicts (non-blocking)
    gov.WithConflictPreventionPolicy(
        "checkout-redesign",
        "payment-flow-v2");
});

// Before proceeding
var critical = await policyEvaluator.AreAllCriticalPoliciesCompliantAsync(context);
if (!critical)
{
    throw new InvalidOperationException("Critical policy violations");
}
```

### 4. Integrate with External Systems

Connect governance to your existing tools:

```csharp
// Custom approval gate for ServiceNow
public class ServiceNowApprovalGate : IApprovalGate
{
    // Check change request status in ServiceNow
}

// Custom policy for Datadog alerts
public class DatadogAlertsPolicy : IExperimentPolicy
{
    // Check if experiment is triggering alerts
}
```

### 5. Audit Everything

Ensure all actions are recorded:

```csharp
public class ComprehensiveAuditSink : IAuditSink
{
    public async Task RecordAsync(AuditEvent auditEvent, CancellationToken ct)
    {
        // Store in database
        await _db.SaveAuditEvent(auditEvent);
        
        // Send to Splunk
        await _splunk.LogEvent(auditEvent);
        
        // Trigger alerts for critical events
        if (auditEvent.EventType == AuditEventType.ExperimentModified &&
            auditEvent.Details?.ContainsKey("lifecycleTransition") is true)
        {
            await _alerting.NotifyLifecycleChange(auditEvent);
        }
    }
}
```

### 6. Test Transitions in Non-Production

Validate lifecycle transitions in your test environment:

```csharp
[Fact]
public async Task CanTransitionThroughCompleteLifecycle()
{
    // Draft → PendingApproval
    await _lifecycle.TransitionAsync(
        "test-experiment",
        ExperimentLifecycleState.PendingApproval);
    
    // PendingApproval → Approved
    await _lifecycle.TransitionAsync(
        "test-experiment",
        ExperimentLifecycleState.Approved);
    
    // Approved → Running
    await _lifecycle.TransitionAsync(
        "test-experiment",
        ExperimentLifecycleState.Running);
    
    // Running → Archived
    await _lifecycle.TransitionAsync(
        "test-experiment",
        ExperimentLifecycleState.Archived);
    
    var history = _lifecycle.GetHistory("test-experiment");
    Assert.Equal(4, history.Count);
}
```

## Next Steps

- Explore [Admin API](admin-api.md) for general experiment management
- Review [Audit Logging](audit.md) for compliance requirements
- Check [Rollout](rollout.md) for percentage-based traffic allocation
- See [Samples](samples.md) for complete examples

## See Also

- [Audit Logging](audit.md) - Forensic audit trail
- [Admin API](admin-api.md) - REST endpoints for experiment management
- [Rollout](rollout.md) - Percentage-based traffic allocation
- [Configuration](configuration.md) - YAML/JSON experiment configuration
