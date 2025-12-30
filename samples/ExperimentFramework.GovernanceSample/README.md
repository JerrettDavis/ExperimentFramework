# Experiment Governance Sample

This sample demonstrates the governance features of ExperimentFramework, including lifecycle management, approval gates, configuration versioning, and policy-as-code guardrails.

## Features Demonstrated

- **Lifecycle Management**: State transitions with validation
- **Approval Gates**: Role-based and automatic approvals
- **Configuration Versioning**: Immutable versions with history
- **Policy-as-Code**: Traffic limits, error rates, and time windows
- **Audit Trail**: All actions logged to console

## Running the Sample

```bash
cd samples/ExperimentFramework.GovernanceSample
dotnet run
```

Then open your browser to `https://localhost:5001/swagger` to explore the API.

## Example Workflow

### 1. Transition Experiment Through Lifecycle

```bash
# Submit for approval (Draft → PendingApproval)
curl -X POST "https://localhost:5001/demo/lifecycle?experimentName=checkout-test&targetState=PendingApproval"

# Get current state
curl "https://localhost:5001/api/governance/checkout-test/lifecycle/state"

# Approve (PendingApproval → Approved) - manual approval needed first
curl -X POST "https://localhost:5001/api/governance/checkout-test/lifecycle/transition" \
  -H "Content-Type: application/json" \
  -d '{
    "targetState": "Approved",
    "actor": "manager@example.com",
    "reason": "Approved"
  }'

# Activate (Approved → Running) - requires operator role
curl -X POST "https://localhost:5001/api/governance/checkout-test/lifecycle/transition" \
  -H "Content-Type: application/json" \
  -d '{
    "targetState": "Running",
    "actor": "operator@example.com",
    "reason": "Activating",
    "metadata": {
      "actorRole": "operator"
    }
  }'
```

### 2. Create Configuration Versions

```bash
# Create initial version
curl -X POST "https://localhost:5001/demo/version?experimentName=checkout-test" \
  -H "Content-Type: application/json" \
  -d '{
    "trafficPercentage": 5,
    "features": ["express-checkout"]
  }'

# Create updated version
curl -X POST "https://localhost:5001/api/governance/checkout-test/versions" \
  -H "Content-Type: application/json" \
  -d '{
    "configuration": {
      "trafficPercentage": 25,
      "features": ["express-checkout", "save-payment"]
    },
    "actor": "sre@example.com",
    "changeDescription": "Ramping to 25% and adding save-payment feature"
  }'

# Get all versions
curl "https://localhost:5001/api/governance/checkout-test/versions"

# Get diff between versions
curl "https://localhost:5001/api/governance/checkout-test/versions/diff?fromVersion=1&toVersion=2"
```

### 3. Evaluate Policies

```bash
# Evaluate with good metrics
curl -X POST "https://localhost:5001/demo/policy?experimentName=checkout-test" \
  -H "Content-Type: application/json" \
  -d '{
    "currentState": "Running",
    "telemetry": {
      "trafficPercentage": 5.0,
      "errorRate": 0.02,
      "runningDuration": "01:30:00"
    }
  }'

# Evaluate with policy violations
curl -X POST "https://localhost:5001/api/governance/checkout-test/policies/evaluate" \
  -H "Content-Type: application/json" \
  -d '{
    "currentState": "Running",
    "telemetry": {
      "trafficPercentage": 50.0,
      "errorRate": 0.08
    }
  }'
```

### 4. View State History

```bash
# Get complete transition history
curl "https://localhost:5001/api/governance/checkout-test/lifecycle/history"

# Get allowed next states
curl "https://localhost:5001/api/governance/checkout-test/lifecycle/allowed-transitions"
```

## Configured Approval Gates

1. **Automatic**: Draft → PendingApproval (no approval needed)
2. **RoleBased**: Approved → Running (requires "operator" or "sre" role)
3. **RoleBased**: Running → Ramping (requires "sre" role)

## Configured Policies

1. **TrafficLimitPolicy**: Max 10% traffic until 30 minutes stable
2. **ErrorRatePolicy**: Max 5% error rate
3. **TimeWindowPolicy**: Operations only allowed 09:00-17:00 UTC

## Audit Trail

All lifecycle transitions and version changes are logged to the console via the `ConsoleAuditSink`. In production, implement `IAuditSink` to send events to your logging infrastructure (Splunk, DataDog, etc.).

## What to Try

1. **Valid Workflow**: Follow the lifecycle from Draft → PendingApproval → Approved → Running
2. **Invalid Transition**: Try to go directly from Draft to Running (should fail)
3. **Policy Violation**: Try ramping to 50% traffic immediately (should fail policy check)
4. **Version Rollback**: Create multiple versions, then rollback to an earlier one
5. **State History**: View the complete audit trail of an experiment

## Next Steps

- Implement custom approval gates for integration with your change management system
- Create domain-specific policies for your use cases
- Connect to a real audit sink (database, logging service)
- Add authentication/authorization for the API endpoints
