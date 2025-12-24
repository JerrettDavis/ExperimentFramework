# Kill Switch

Kill switch provides manual control to immediately disable problematic experiments or trials without code deployment. Use it for emergency shutdowns when automated circuit breakers aren't fast enough or when you need explicit control.

## Overview

Kill switch enables:

- **Emergency shutdown**: Instantly disable failing experiments
- **Surgical control**: Disable specific trials while keeping others running
- **Gradual rollback**: Disable trials incrementally during issues
- **Manual override**: Take control when automation isn't appropriate

## Installation

No additional package required - built into core ExperimentFramework.

## Basic Setup

```csharp
using ExperimentFramework.KillSwitch;

var killSwitch = new InMemoryKillSwitchProvider();

var experiments = ExperimentFrameworkBuilder.Create()
    .Define<IDatabase>(c => c
        .UsingFeatureFlag("UseCloudDb")
        .AddDefaultTrial<LocalDb>("false")
        .AddTrial<CloudDb>("true")
        .OnErrorRedirectAndReplayDefault())
    .WithKillSwitch(killSwitch)
    .UseDispatchProxy();

builder.Services.AddExperimentFramework(experiments);

// Make kill switch accessible
builder.Services.AddSingleton<IKillSwitchProvider>(killSwitch);
```

## Operations

### Disable Entire Experiment

Disables all trials, throws `ExperimentDisabledException`:

```csharp
killSwitch.DisableExperiment(typeof(IDatabase));

// All calls to IDatabase now throw ExperimentDisabledException
await database.GetDataAsync(); // Throws
```

### Disable Specific Trial

Disables one trial, falls back according to error policy:

```csharp
killSwitch.DisableTrial(typeof(IDatabase), "cloud");

// Calls to "cloud" trial fall back to default
// (if OnErrorRedirectAndReplayDefault is configured)
```

### Re-enable

```csharp
// Re-enable specific trial
killSwitch.EnableTrial(typeof(IDatabase), "cloud");

// Re-enable entire experiment
killSwitch.EnableExperiment(typeof(IDatabase));
```

### Check Status

```csharp
bool isDisabled = killSwitch.IsExperimentDisabled(typeof(IDatabase));
bool isTrialDisabled = killSwitch.IsTrialDisabled(typeof(IDatabase), "cloud");
```

## Admin API

Create HTTP endpoints for operational control:

```csharp
// SECURITY: Create a whitelist of allowed experiment types to prevent type injection attacks
var experimentRegistry = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
{
    ["IDatabase"] = typeof(IDatabase),
    ["ITaxProvider"] = typeof(ITaxProvider),
    ["IPaymentProcessor"] = typeof(IPaymentProcessor)
    // Add all your experiment service types here
};

var app = builder.Build();

// Disable entire experiment
app.MapPost("/admin/experiments/disable", (
    [FromQuery] string experimentName,
    IKillSwitchProvider killSwitch) =>
{
    if (!experimentRegistry.TryGetValue(experimentName, out var type))
        return Results.NotFound($"Experiment '{experimentName}' not found");

    killSwitch.DisableExperiment(type);
    return Results.Ok($"Experiment {experimentName} disabled");
})
.RequireAuthorization("Admin");

// Disable specific trial
app.MapPost("/admin/experiments/disable-trial", (
    [FromQuery] string experimentName,
    [FromQuery] string trialKey,
    IKillSwitchProvider killSwitch) =>
{
    if (!experimentRegistry.TryGetValue(experimentName, out var type))
        return Results.NotFound($"Experiment '{experimentName}' not found");

    killSwitch.DisableTrial(type, trialKey);
    return Results.Ok($"Trial '{trialKey}' of {experimentName} disabled");
})
.RequireAuthorization("Admin");

// Enable experiment
app.MapPost("/admin/experiments/enable", (
    [FromQuery] string experimentName,
    IKillSwitchProvider killSwitch) =>
{
    if (!experimentRegistry.TryGetValue(experimentName, out var type))
        return Results.NotFound($"Experiment '{experimentName}' not found");

    killSwitch.EnableExperiment(type);
    return Results.Ok($"Experiment {experimentName} enabled");
})
.RequireAuthorization("Admin");

// Get status
app.MapGet("/admin/experiments/status", (
    [FromQuery] string experimentName,
    IKillSwitchProvider killSwitch) =>
{
    if (!experimentRegistry.TryGetValue(experimentName, out var type))
        return Results.NotFound($"Experiment '{experimentName}' not found");

    return Results.Ok(new
    {
        experiment = experimentName,
        experimentDisabled = killSwitch.IsExperimentDisabled(type),
        trials = new
        {
            cloud = killSwitch.IsTrialDisabled(type, "cloud"),
            local = killSwitch.IsTrialDisabled(type, "local")
        }
    });
})
.RequireAuthorization("Admin");
```

### Usage

```bash
# Disable cloud trial
curl -X POST "https://api.example.com/admin/experiments/disable-trial?experimentName=IDatabase&trialKey=cloud" \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Check status
curl "https://api.example.com/admin/experiments/status?experimentName=IDatabase" \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Re-enable
curl -X POST "https://api.example.com/admin/experiments/enable?experimentName=IDatabase" \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

## Distributed Kill Switch (Redis)

For multi-instance deployments, use Redis for shared state:

```bash
dotnet add package StackExchange.Redis
```

```csharp
using StackExchange.Redis;

public class RedisKillSwitchProvider : IKillSwitchProvider
{
    private readonly IConnectionMultiplexer _redis;

    public RedisKillSwitchProvider(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public bool IsTrialDisabled(Type serviceType, string trialKey)
    {
        var db = _redis.GetDatabase();
        var key = $"killswitch:trial:{serviceType.FullName}:{trialKey}";
        return db.KeyExists(key);
    }

    public bool IsExperimentDisabled(Type serviceType)
    {
        var db = _redis.GetDatabase();
        var key = $"killswitch:experiment:{serviceType.FullName}";
        return db.KeyExists(key);
    }

    public void DisableTrial(Type serviceType, string trialKey)
    {
        var db = _redis.GetDatabase();
        var key = $"killswitch:trial:{serviceType.FullName}:{trialKey}";
        db.StringSet(key, "disabled", TimeSpan.FromHours(24));
    }

    public void DisableExperiment(Type serviceType)
    {
        var db = _redis.GetDatabase();
        var key = $"killswitch:experiment:{serviceType.FullName}";
        db.StringSet(key, "disabled", TimeSpan.FromHours(24));
    }

    public void EnableTrial(Type serviceType, string trialKey)
    {
        var db = _redis.GetDatabase();
        var key = $"killswitch:trial:{serviceType.FullName}:{trialKey}";
        db.KeyDelete(key);
    }

    public void EnableExperiment(Type serviceType)
    {
        var db = _redis.GetDatabase();
        var key = $"killswitch:experiment:{serviceType.FullName}";
        db.KeyDelete(key);
    }
}

// Registration
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect("localhost:6379"));

builder.Services.AddSingleton<IKillSwitchProvider, RedisKillSwitchProvider>();

var killSwitch = builder.Services.BuildServiceProvider()
    .GetRequiredService<IKillSwitchProvider>();

var experiments = ExperimentFrameworkBuilder.Create()
    .Define<IDatabase>(c => c.UsingFeatureFlag("UseCloudDb")...)
    .WithKillSwitch(killSwitch)
    .UseDispatchProxy();

builder.Services.AddExperimentFramework(experiments);
```

## Combining with Circuit Breaker

Use both for defense in depth:

```csharp
var killSwitch = new InMemoryKillSwitchProvider();

var experiments = ExperimentFrameworkBuilder.Create()
    .Define<IPaymentGateway>(c => c
        .UsingFeatureFlag("UseNewGateway")
        .AddDefaultTrial<StableGateway>("false")
        .AddTrial<NewGateway>("true")
        .OnErrorRedirectAndReplayDefault())
    .WithCircuitBreaker(options =>
    {
        options.FailureRatioThreshold = 0.5;
        options.MinimumThroughput = 10;
        options.OnCircuitOpen = CircuitBreakerAction.FallbackToDefault;
    })
    .WithKillSwitch(killSwitch)
    .UseDispatchProxy();
```

**Decision tree:**
1. Kill switch checks first (fastest)
2. If not killed, circuit breaker evaluates
3. If circuit open, falls back
4. If circuit closed, executes trial

**When to use each:**
- **Circuit breaker**: Automatic protection against failing trials
- **Kill switch**: Manual intervention when automation insufficient

## Real-World Example

Payment gateway with kill switch and monitoring:

```csharp
using ExperimentFramework.KillSwitch;
using ExperimentFramework.Metrics.Exporters;

// Setup
var killSwitch = new InMemoryKillSwitchProvider();
var metrics = new PrometheusExperimentMetrics();

var experiments = ExperimentFrameworkBuilder.Create()
    .Define<IPaymentGateway>(c => c
        .UsingFeatureFlag("UseNewPaymentGateway")
        .AddDefaultTrial<StableGateway>("false")
        .AddTrial<NewGateway>("true")
        .OnErrorRedirectAndReplayDefault())
    .WithCircuitBreaker(options =>
    {
        options.FailureRatioThreshold = 0.2;
        options.MinimumThroughput = 5;
        options.OnCircuitOpen = CircuitBreakerAction.FallbackToDefault;
    })
    .WithMetrics(metrics)
    .WithKillSwitch(killSwitch)
    .UseDispatchProxy();

builder.Services.AddExperimentFramework(experiments);
builder.Services.AddSingleton<IKillSwitchProvider>(killSwitch);

var app = builder.Build();

// Admin endpoints
app.MapPost("/admin/kill-switch/disable-new-gateway",
    (IKillSwitchProvider ks) =>
    {
        ks.DisableTrial(typeof(IPaymentGateway), "true");
        return Results.Ok("New gateway disabled");
    })
    .RequireAuthorization("Admin");

app.MapGet("/admin/kill-switch/status",
    (IKillSwitchProvider ks) =>
    {
        return Results.Ok(new
        {
            newGatewayDisabled = ks.IsTrialDisabled(typeof(IPaymentGateway), "true"),
            experimentDisabled = ks.IsExperimentDisabled(typeof(IPaymentGateway))
        });
    });

// Metrics endpoint
app.MapGet("/metrics", () => metrics.GeneratePrometheusOutput());

app.Run();
```

**Scenario:**
1. New payment gateway deployed at 10% traffic
2. Error rate spikes to 15% (metrics show issue)
3. Circuit breaker hasn't opened yet (< 20% threshold)
4. Admin manually disables new gateway via kill switch
5. All traffic routes to stable gateway immediately
6. Team investigates issue
7. After fix, admin re-enables new gateway
8. Gradual rollout resumes

## Best Practices

### 1. Require Authorization

Always protect admin endpoints:

```csharp
app.MapPost("/admin/experiments/disable", ...)
    .RequireAuthorization("Admin")
    .RequireHost("internal.example.com"); // Extra safety
```

### 2. Add Audit Logging

Track who disabled what and when:

```csharp
app.MapPost("/admin/experiments/disable-trial", (
    string experimentName,
    string trialKey,
    IKillSwitchProvider killSwitch,
    ILogger<Program> logger,
    HttpContext context) =>
{
    if (!experimentRegistry.TryGetValue(experimentName, out var type))
        return Results.NotFound();

    var user = context.User.Identity?.Name ?? "Unknown";

    killSwitch.DisableTrial(type, trialKey);

    logger.LogWarning(
        "Kill switch activated: User {User} disabled trial {Trial} of {Experiment}",
        user, trialKey, experimentName);

    return Results.Ok();
})
.RequireAuthorization("Admin");
```

### 3. Set Expiration on Distributed Kill Switches

Prevent "forgotten" disabled experiments:

```csharp
public void DisableTrial(Type serviceType, string trialKey)
{
    var db = _redis.GetDatabase();
    var key = $"killswitch:trial:{serviceType.FullName}:{trialKey}";
    db.StringSet(key, "disabled", TimeSpan.FromHours(24)); // Auto-expire
}
```

### 4. Create Runbooks

Document when to use kill switch:

```markdown
# Kill Switch Runbook

## When to Use
- Error rate > 10% and increasing
- Circuit breaker not opening fast enough
- Security incident involving experiment
- Data corruption from experimental trial

## How to Use
1. Verify metrics confirm issue
2. Run: `curl -X POST .../disable-trial?experimentType=...&trialKey=...`
3. Verify traffic shifted via metrics
4. Create incident ticket
5. Investigate root cause
6. After fix, re-enable gradually

## Contacts
- On-call: pager duty rotation
- Experiment owner: @team-experiments
```

### 5. Test Kill Switch

Verify it works before you need it:

```csharp
[Fact]
public async Task KillSwitch_DisablesTrial()
{
    // Arrange
    var killSwitch = new InMemoryKillSwitchProvider();
    var experiments = ExperimentFrameworkBuilder.Create()
        .Define<IDatabase>(c => c
            .UsingFeatureFlag("UseCloudDb")
            .AddDefaultTrial<LocalDb>("false")
            .AddTrial<CloudDb>("true")
            .OnErrorRedirectAndReplayDefault())
        .WithKillSwitch(killSwitch)
        .UseDispatchProxy();

    // Act
    killSwitch.DisableTrial(typeof(IDatabase), "true");

    // Assert - falls back to default
    var result = await database.GetDataAsync();
    Assert.Equal("Local", result.Source);
}
```

## Monitoring

Track kill switch usage:

```csharp
app.MapPost("/admin/experiments/disable-trial", (
    string experimentName,
    string trialKey,
    IKillSwitchProvider killSwitch,
    PrometheusExperimentMetrics metrics) =>
{
    if (!experimentRegistry.TryGetValue(experimentName, out var type))
        return Results.NotFound();

    killSwitch.DisableTrial(type, trialKey);

    // Increment custom metric
    metrics.IncrementCounter("killswitch_activations_total",
        tags: new[]
        {
            new KeyValuePair<string, object>("experiment", experimentName),
            new KeyValuePair<string, object>("trial", trialKey)
        });

    return Results.Ok();
});
```

## Troubleshooting

### Kill Switch Not Working

**Symptom**: Trial still executes after disabling.

**Solutions:**
1. Verify same `IKillSwitchProvider` instance used in experiments and admin API
2. Check `WithKillSwitch()` called before `UseDispatchProxy()`
3. Ensure correct service type and trial key (case-sensitive)
4. For distributed: verify Redis connection

### Experiment Stays Disabled

**Symptom**: Can't re-enable experiment.

**Solutions:**
1. Call `EnableExperiment()` or `EnableTrial()`
2. For distributed: check Redis keys manually
3. Verify no conflicting feature flag settings
4. Check if auto-expiration set (distributed mode)

### Admin API Unauthorized

**Symptom**: 401 when calling admin endpoints.

**Solutions:**
1. Verify authentication configured correctly
2. Check user has "Admin" role
3. Ensure bearer token valid and not expired

## See Also

- [Circuit Breaker](circuit-breaker.md) - Automatic failure protection
- [Metrics](metrics.md) - Monitor experiment health
- [Error Handling](error-handling.md) - Fallback strategies
