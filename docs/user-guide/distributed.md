# Distributed State Management

The Distributed package provides shared state management for experiments running across multiple application instances. This is essential for consistent behavior in load-balanced, horizontally-scaled, or microservices environments.

## Installation

```bash
# Core abstractions and in-memory implementation
dotnet add package ExperimentFramework.Distributed

# Redis implementation for production
dotnet add package ExperimentFramework.Distributed.Redis
```

## Quick Start

### Development (In-Memory)

```csharp
// For development and testing
services.AddExperimentDistributedInMemory();
```

### Production (Redis)

```csharp
// For production with Redis
services.AddExperimentDistributedRedis(options =>
{
    options.ConnectionString = "localhost:6379";
    options.InstanceName = "experiments:";
});
```

## Core Interfaces

### IDistributedExperimentState

Provides key-value state storage with optional expiration:

```csharp
public interface IDistributedExperimentState
{
    // Get a value
    ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    // Set a value with optional expiration
    ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    // Remove a value
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);

    // Atomic increment (for counters)
    ValueTask<long> IncrementAsync(string key, long delta = 1,
        CancellationToken cancellationToken = default);
}
```

### IDistributedLockProvider

Provides distributed locking for coordinated operations:

```csharp
public interface IDistributedLockProvider
{
    // Try to acquire a lock immediately
    ValueTask<IDistributedLockHandle?> TryAcquireAsync(
        string lockName,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);

    // Acquire with timeout (waits for lock)
    ValueTask<IDistributedLockHandle?> AcquireAsync(
        string lockName,
        TimeSpan expiration,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

public interface IDistributedLockHandle : IAsyncDisposable
{
    string LockName { get; }
    bool IsAcquired { get; }
}
```

## Use Cases

### Shared Experiment Counters

Track metrics across all instances:

```csharp
public class DistributedMetricsCollector
{
    private readonly IDistributedExperimentState _state;

    public DistributedMetricsCollector(IDistributedExperimentState state)
    {
        _state = state;
    }

    public async Task RecordImpressionAsync(string experimentName, string variantKey)
    {
        var key = $"experiment:{experimentName}:variant:{variantKey}:impressions";
        await _state.IncrementAsync(key);
    }

    public async Task RecordConversionAsync(string experimentName, string variantKey)
    {
        var key = $"experiment:{experimentName}:variant:{variantKey}:conversions";
        await _state.IncrementAsync(key);
    }

    public async Task<ExperimentMetrics> GetMetricsAsync(string experimentName)
    {
        // Aggregate metrics from distributed state
        var impressions = await _state.GetAsync<long>(
            $"experiment:{experimentName}:total:impressions");
        var conversions = await _state.GetAsync<long>(
            $"experiment:{experimentName}:total:conversions");

        return new ExperimentMetrics(impressions, conversions);
    }
}
```

### Coordinated Experiment Updates

Use locks to safely update experiment configuration:

```csharp
public class ExperimentConfigurationUpdater
{
    private readonly IDistributedLockProvider _lockProvider;
    private readonly IDistributedExperimentState _state;

    public async Task UpdateExperimentAsync(string experimentName, ExperimentConfig newConfig)
    {
        var lockHandle = await _lockProvider.AcquireAsync(
            lockName: $"experiment:{experimentName}:config-lock",
            expiration: TimeSpan.FromMinutes(1),
            timeout: TimeSpan.FromSeconds(30));

        if (lockHandle == null)
        {
            throw new InvalidOperationException("Could not acquire lock for experiment update");
        }

        await using (lockHandle)
        {
            // Read current config
            var current = await _state.GetAsync<ExperimentConfig>(
                $"experiment:{experimentName}:config");

            // Validate and update
            if (current?.Version != newConfig.Version - 1)
            {
                throw new ConcurrencyException("Configuration was modified by another process");
            }

            await _state.SetAsync($"experiment:{experimentName}:config", newConfig);
        }
    }
}
```

### Sticky Sessions

Ensure users get consistent variant assignments:

```csharp
public class StickySessionProvider
{
    private readonly IDistributedExperimentState _state;
    private readonly TimeSpan _sessionDuration = TimeSpan.FromDays(30);

    public async Task<string?> GetAssignedVariantAsync(string userId, string experimentName)
    {
        var key = $"user:{userId}:experiment:{experimentName}:assignment";
        return await _state.GetAsync<string>(key);
    }

    public async Task SetAssignedVariantAsync(
        string userId, string experimentName, string variantKey)
    {
        var key = $"user:{userId}:experiment:{experimentName}:assignment";
        await _state.SetAsync(key, variantKey, _sessionDuration);
    }
}
```

## Redis Configuration

### Basic Setup

```csharp
services.AddExperimentDistributedRedis(options =>
{
    options.ConnectionString = "localhost:6379";
    options.InstanceName = "myapp:experiments:";
    options.DefaultExpiration = TimeSpan.FromHours(24);
});
```

### Connection Options

```csharp
services.AddExperimentDistributedRedis(options =>
{
    options.ConnectionString = "redis-primary:6379,redis-replica:6379";
    options.InstanceName = "experiments:";

    // Connection configuration
    options.ConnectTimeout = TimeSpan.FromSeconds(5);
    options.SyncTimeout = TimeSpan.FromSeconds(3);
    options.AbortOnConnectFail = false;

    // Authentication
    options.Password = Environment.GetEnvironmentVariable("REDIS_PASSWORD");

    // SSL/TLS
    options.Ssl = true;
    options.SslHost = "redis.example.com";
});
```

### Sentinel Configuration

```csharp
services.AddExperimentDistributedRedis(options =>
{
    options.ConnectionString = "sentinel1:26379,sentinel2:26379,sentinel3:26379";
    options.ServiceName = "mymaster";
    options.InstanceName = "experiments:";
});
```

## Custom Implementation

Implement the interfaces for other backends:

```csharp
public class CosmosDistributedState : IDistributedExperimentState
{
    private readonly Container _container;

    public CosmosDistributedState(CosmosClient client, string databaseId, string containerId)
    {
        _container = client.GetContainer(databaseId, containerId);
    }

    public async ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<StateDocument<T>>(
                key, new PartitionKey(key), cancellationToken: cancellationToken);
            return response.Resource.Value;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }
    }

    public async ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var ttl = expiration.HasValue ? (int)expiration.Value.TotalSeconds : -1;
        var document = new StateDocument<T>
        {
            Id = key,
            Value = value,
            Ttl = ttl
        };
        await _container.UpsertItemAsync(document, new PartitionKey(key),
            cancellationToken: cancellationToken);
    }

    // ... implement other methods
}

// Register custom implementation
services.AddExperimentDistributedState<CosmosDistributedState>();
```

## Integration with Other Packages

### With Outcome Collection

```csharp
services.AddExperimentDistributedRedis(options =>
{
    options.ConnectionString = "localhost:6379";
});

services.AddExperimentData(options =>
{
    // Use distributed state for outcome collection
    options.UseDistributedState = true;
    options.BatchSize = 100;
    options.FlushInterval = TimeSpan.FromSeconds(10);
});
```

### With Bandit Algorithms

```csharp
// Bandit algorithms can use distributed state for arm statistics
public class DistributedBanditState
{
    private readonly IDistributedExperimentState _state;
    private readonly IDistributedLockProvider _lockProvider;

    public async Task UpdateArmAsync(string experimentName, ArmStatistics arm, double reward)
    {
        var lockHandle = await _lockProvider.AcquireAsync(
            $"bandit:{experimentName}:arm:{arm.Key}",
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10));

        await using (lockHandle)
        {
            // Read current stats
            var key = $"bandit:{experimentName}:arm:{arm.Key}:stats";
            var stats = await _state.GetAsync<ArmStatistics>(key) ?? arm;

            // Update with new reward
            stats.Pulls++;
            stats.TotalReward += reward;
            if (reward > 0.5)
                stats.Successes++;
            else
                stats.Failures++;

            await _state.SetAsync(key, stats);
        }
    }
}
```

## Best Practices

1. **Use appropriate key prefixes**: Organize keys with clear prefixes (`experiment:`, `user:`, etc.)
2. **Set expiration times**: Prevent unbounded state growth
3. **Handle connection failures gracefully**: Implement fallback behavior
4. **Use locks sparingly**: Only for operations that truly require coordination
5. **Monitor Redis memory**: Track memory usage and set eviction policies

## Troubleshooting

### Connection timeouts

**Symptom**: Operations timing out or failing intermittently.

**Cause**: Network issues or Redis server overloaded.

**Solution**: Increase timeouts and add retry logic:

```csharp
services.AddExperimentDistributedRedis(options =>
{
    options.ConnectTimeout = TimeSpan.FromSeconds(10);
    options.SyncTimeout = TimeSpan.FromSeconds(5);
    options.RetryCount = 3;
});
```

### Lock not released

**Symptom**: Operations blocked waiting for locks that never release.

**Cause**: Process crashed while holding lock.

**Solution**: Use appropriate lock expiration:

```csharp
// Lock will auto-expire if not explicitly released
var lockHandle = await _lockProvider.AcquireAsync(
    "my-lock",
    expiration: TimeSpan.FromMinutes(1),  // Auto-expire after 1 minute
    timeout: TimeSpan.FromSeconds(30));
```

### Memory growing unbounded

**Symptom**: Redis memory usage increasing continuously.

**Cause**: Keys not expiring.

**Solution**: Always set expiration for non-permanent data:

```csharp
// Set expiration for session data
await _state.SetAsync(key, value, TimeSpan.FromHours(24));

// For counters, use periodic cleanup or TTL-based keys
var dateKey = $"metrics:{DateTime.UtcNow:yyyyMMdd}:impressions";
await _state.IncrementAsync(dateKey);
```
