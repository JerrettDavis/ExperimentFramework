# Percentage-Based Rollout

The Rollout package provides deterministic, percentage-based traffic allocation for gradual feature rollouts. This enables safe deployment of new implementations by controlling what percentage of users receive the new variant.

## Installation

```bash
dotnet add package ExperimentFramework.Rollout
```

## Quick Start

```csharp
// 1. Register rollout support
services.AddExperimentRollout();

// 2. Provide an identity provider for consistent assignment
services.AddScoped<IRolloutIdentityProvider, UserIdProvider>();

// 3. Configure your experiment
var experiments = ExperimentFrameworkBuilder.Create()
    .Define<IPaymentProcessor>(exp => exp
        .UsingRollout(percentage: 25)  // 25% get new implementation
        .AddControl<LegacyPaymentProcessor>("legacy")
        .AddCondition<NewPaymentProcessor>("new-v2"));

services.AddExperimentFramework(experiments);
```

## How It Works

### Deterministic Assignment

The rollout allocator uses a hash-based algorithm that ensures:

1. **Consistency**: The same user always gets the same variant
2. **Determinism**: Results are reproducible across application restarts
3. **Uniform distribution**: Traffic is evenly distributed according to the percentage

```csharp
// The algorithm combines identity + experiment name for assignment
public static bool IsIncluded(string identity, string selectorName, int percentage, string? seed = null)
{
    var input = $"{seed ?? selectorName}:{identity}";
    var hash = ComputeHash(input);
    var bucket = (int)(hash % 100);
    return bucket < percentage;
}
```

### Identity Provider

You must implement `IRolloutIdentityProvider` to supply the identity used for bucketing:

```csharp
public interface IRolloutIdentityProvider
{
    ValueTask<string?> GetIdentityAsync(CancellationToken cancellationToken = default);
}

// Example implementation
public class UserIdProvider : IRolloutIdentityProvider
{
    private readonly IHttpContextAccessor _httpContext;

    public UserIdProvider(IHttpContextAccessor httpContext)
    {
        _httpContext = httpContext;
    }

    public ValueTask<string?> GetIdentityAsync(CancellationToken cancellationToken = default)
    {
        var userId = _httpContext.HttpContext?.User.FindFirst("sub")?.Value;
        return ValueTask.FromResult(userId);
    }
}
```

## Configuration Options

### Basic Rollout

```csharp
services.AddExperimentRollout(options =>
{
    options.Percentage = 50;        // 50% rollout
    options.Seed = "my-experiment"; // Optional: override bucket seed
    options.IncludedKey = "new";    // Key for included users
    options.ExcludedKey = "legacy"; // Key for excluded users
});
```

### Fluent Configuration

```csharp
.Define<ISearchService>(exp => exp
    .UsingRollout(percentage: 10, includedKey: "elastic", excludedKey: "legacy")
    .AddControl<SqlSearchService>("legacy")
    .AddCondition<ElasticSearchService>("elastic"))
```

## Staged Rollout

For gradual rollouts over time, use staged rollout with scheduled percentage increases:

```csharp
services.AddExperimentStagedRollout(options =>
{
    options.Stages.Add(new RolloutStage
    {
        StartsAt = DateTimeOffset.UtcNow,
        Percentage = 5
    });
    options.Stages.Add(new RolloutStage
    {
        StartsAt = DateTimeOffset.UtcNow.AddDays(1),
        Percentage = 25
    });
    options.Stages.Add(new RolloutStage
    {
        StartsAt = DateTimeOffset.UtcNow.AddDays(3),
        Percentage = 50
    });
    options.Stages.Add(new RolloutStage
    {
        StartsAt = DateTimeOffset.UtcNow.AddDays(7),
        Percentage = 100
    });
});
```

### Staged Rollout Fluent API

```csharp
.Define<INotificationService>(exp => exp
    .UsingStagedRollout(stages: new[]
    {
        (DateTimeOffset.UtcNow, 10),
        (DateTimeOffset.UtcNow.AddDays(1), 50),
        (DateTimeOffset.UtcNow.AddDays(7), 100)
    })
    .AddControl<EmailNotificationService>("email")
    .AddCondition<PushNotificationService>("push"))
```

## Configuration File Support

Enable YAML/JSON configuration for rollouts:

```csharp
services.AddExperimentRolloutConfiguration();
services.AddExperimentRollout();
services.AddExperimentFrameworkFromConfiguration(configuration);
```

### YAML Configuration

```yaml
experimentFramework:
  trials:
    - serviceType: IPaymentProcessor
      selectionMode:
        type: rollout
        percentage: 50
        seed: payment-v2-experiment
        includedKey: new-processor
        excludedKey: legacy-processor
      variants:
        - key: legacy-processor
          implementationType: LegacyPaymentProcessor
          isControl: true
        - key: new-processor
          implementationType: NewPaymentProcessor
```

### Staged Rollout Configuration

```yaml
experimentFramework:
  trials:
    - serviceType: ISearchService
      selectionMode:
        type: stagedRollout
        stages:
          - startsAt: "2024-01-01T00:00:00Z"
            percentage: 10
          - startsAt: "2024-01-08T00:00:00Z"
            percentage: 50
          - startsAt: "2024-01-15T00:00:00Z"
            percentage: 100
      variants:
        - key: legacy
          implementationType: SqlSearchService
          isControl: true
        - key: elastic
          implementationType: ElasticSearchService
```

## Real-World Examples

### Database Migration

Gradually migrate from one database to another:

```csharp
public class DatabaseMigrationIdentityProvider : IRolloutIdentityProvider
{
    private readonly IHttpContextAccessor _httpContext;

    public ValueTask<string?> GetIdentityAsync(CancellationToken cancellationToken = default)
    {
        // Use tenant ID for B2B scenarios
        var tenantId = _httpContext.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        return ValueTask.FromResult(tenantId);
    }
}

// Configure staged migration
services.AddExperimentStagedRollout(opts =>
{
    opts.Stages.Add(new RolloutStage { StartsAt = DateTimeOffset.UtcNow, Percentage = 1 });
    opts.Stages.Add(new RolloutStage { StartsAt = DateTimeOffset.UtcNow.AddDays(7), Percentage = 10 });
    opts.Stages.Add(new RolloutStage { StartsAt = DateTimeOffset.UtcNow.AddDays(14), Percentage = 50 });
    opts.Stages.Add(new RolloutStage { StartsAt = DateTimeOffset.UtcNow.AddMonths(1), Percentage = 100 });
});

var experiments = ExperimentFrameworkBuilder.Create()
    .Define<IOrderRepository>(exp => exp
        .UsingStagedRollout()
        .AddControl<SqlOrderRepository>("sql")
        .AddCondition<CosmosOrderRepository>("cosmos"));
```

### API Version Migration

```csharp
.Define<IExternalApiClient>(exp => exp
    .UsingRollout(percentage: 20)
    .AddControl<ApiClientV1>("v1")
    .AddCondition<ApiClientV2>("v2"))
```

## Best Practices

1. **Start small**: Begin with 1-5% rollout to catch issues early
2. **Use meaningful seeds**: Seed names help with debugging and reproducibility
3. **Monitor closely**: Combine with telemetry to track variant performance
4. **Plan rollback**: Have a strategy to quickly reduce percentage if issues arise
5. **Consider identity**: Choose identity carefully - user ID, session ID, or tenant ID based on your use case

## Troubleshooting

### Users switching between variants

**Symptom**: Users report inconsistent behavior, seeing different variants across requests.

**Cause**: Identity provider returning different values for the same user.

**Solution**: Ensure your identity provider returns a stable, consistent identifier:

```csharp
// Bad: Session-based identity changes across sessions
return httpContext.Session.Id;

// Good: User ID is consistent
return httpContext.User.FindFirst("sub")?.Value;
```

### Rollout percentage not matching expected

**Symptom**: Observing ~45% rollout when configured for 50%.

**Cause**: Hash distribution has natural variance, especially with small sample sizes.

**Solution**: This is expected statistical behavior. With larger user bases (10,000+), distribution approaches the configured percentage.

### No identity available

**Symptom**: All traffic goes to control variant.

**Cause**: Identity provider returning null.

**Solution**: Handle anonymous users explicitly:

```csharp
public ValueTask<string?> GetIdentityAsync(CancellationToken cancellationToken = default)
{
    var userId = _httpContext.HttpContext?.User.FindFirst("sub")?.Value;

    // Fall back to a session or cookie-based identity for anonymous users
    if (string.IsNullOrEmpty(userId))
    {
        userId = _httpContext.HttpContext?.Request.Cookies["visitor_id"];
    }

    return ValueTask.FromResult(userId);
}
```
