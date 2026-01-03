# Governance Persistence Backplanes

This library provides durable persistence backplanes for experiment governance state, including lifecycle transitions, approvals, configuration versions, and policy evaluations.

## Features

- **Multiple Backplane Options**: In-memory (testing), SQL (durable), and Redis (distributed)
- **Optimistic Concurrency Control**: ETag-based conflict detection
- **Immutable History**: Append-only storage for auditing and replay
- **Multi-tenancy Support**: Optional tenant and environment scoping
- **DSL Configuration**: Declarative YAML/JSON configuration support

## Installation

```bash
# For in-memory (testing/development)
dotnet add package ExperimentFramework.Governance.Persistence

# For SQL persistence
dotnet add package ExperimentFramework.Governance.Persistence.Sql

# For Redis persistence
dotnet add package ExperimentFramework.Governance.Persistence.Redis
```

## Usage

### In-Memory Persistence (Testing/Development)

```csharp
services.AddExperimentGovernance(governance =>
{
    governance.UsePersistence(p => p.AddInMemoryGovernancePersistence());
});
```

### SQL Persistence (Production)

```csharp
services.AddExperimentGovernance(governance =>
{
    governance.UsePersistence(p =>
        p.AddSqlGovernancePersistence("Server=localhost;Database=Governance;...")
    );
});

// Run migrations
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<GovernanceDbContext>();
await dbContext.Database.MigrateAsync();
```

### Redis Persistence (Distributed)

```csharp
services.AddExperimentGovernance(governance =>
{
    governance.UsePersistence(p =>
        p.AddRedisGovernancePersistence("localhost:6379", keyPrefix: "governance:")
    );
});
```

## YAML Configuration

```yaml
experimentFramework:
  governance:
    persistence:
      type: sql
      connectionString: ${GOVERNANCE_DB_CONNECTION}
      optimisticConcurrency: true
      retainHistory: true
```

Or with Redis:

```yaml
experimentFramework:
  governance:
    persistence:
      type: redis
      connectionString: ${REDIS_CONNECTION}
      keyPrefix: "governance:"
```

## Optimistic Concurrency

All persistence backplanes support optimistic concurrency control using ETags:

```csharp
// Get current state
var state = await backplane.GetExperimentStateAsync("my-experiment");

// Modify state
var updatedState = new PersistedExperimentState
{
    ExperimentName = state.ExperimentName,
    CurrentState = ExperimentLifecycleState.Running,
    // ... other properties
    LastModified = DateTimeOffset.UtcNow
};

// Save with concurrency check
var result = await backplane.SaveExperimentStateAsync(
    updatedState,
    expectedETag: state.ETag
);

if (result.ConflictDetected)
{
    // Handle conflict - reload and retry
}
```

## Multi-tenancy and Environment Scoping

All operations support optional tenant and environment identifiers:

```csharp
var state = await backplane.GetExperimentStateAsync(
    "my-experiment",
    tenantId: "tenant-123",
    environment: "production"
);
```

## Immutable History

State transitions, approvals, versions, and policy evaluations are append-only:

```csharp
// Append a state transition
await backplane.AppendStateTransitionAsync(new PersistedStateTransition
{
    TransitionId = Guid.NewGuid().ToString(),
    ExperimentName = "my-experiment",
    FromState = ExperimentLifecycleState.Draft,
    ToState = ExperimentLifecycleState.PendingApproval,
    Timestamp = DateTimeOffset.UtcNow,
    Actor = "user@example.com",
    Reason = "Ready for review"
});

// Get full history
var history = await backplane.GetStateTransitionHistoryAsync("my-experiment");
```

## Database Schema (SQL)

The SQL persistence backplane creates the following tables:

- **ExperimentStates**: Current state with optimistic concurrency
- **StateTransitions**: Immutable lifecycle history
- **ApprovalRecords**: Immutable approval history
- **ConfigurationVersions**: Immutable version history
- **PolicyEvaluations**: Immutable policy evaluation history

All tables support indexing for efficient querying and include tenant/environment scoping.

## Redis Data Structures

The Redis persistence backplane uses the following structures:

- **Strings**: Current experiment state (with atomic updates)
- **Lists**: Append-only history for transitions, approvals, evaluations
- **Hashes**: Configuration versions (keyed by version number)

Keys are prefixed for namespacing and include tenant/environment in the key path.

**Note on Concurrency:** The current Redis implementation provides basic optimistic concurrency checking but has a theoretical race condition between ETag verification and update. For scenarios requiring strict transactional guarantees, use the SQL backplane. Redis is best suited for:
- Multi-instance coordination
- Fast read-heavy workloads
- Complementary caching layer
- Development/testing environments

For production workloads with strict consistency requirements, the SQL backplane is recommended as the authoritative governance store.

## Custom Persistence Backplanes

To implement a custom backplane, implement `IGovernancePersistenceBackplane`:

```csharp
public class MyCustomBackplane : IGovernancePersistenceBackplane
{
    // Implement all required methods
}

// Register
services.AddExperimentGovernance(governance =>
{
    governance.UsePersistence(p =>
        p.AddGovernancePersistence<MyCustomBackplane>()
    );
});
```

## Performance Considerations

### SQL
- Use connection pooling
- Configure appropriate indexes
- Consider read replicas for query-heavy workloads
- Use batch operations for bulk inserts

### Redis
- Use pipeline for batch operations
- Set appropriate TTLs for ephemeral data
- Monitor memory usage
- Consider Redis Cluster for scale

## Security

- **Connection Strings**: Store in secure configuration (Azure Key Vault, AWS Secrets Manager, etc.)
- **Encryption**: Enable TLS for Redis and SQL connections
- **Access Control**: Use least-privilege database accounts
- **Audit**: All mutations are automatically logged with actor information

## Migration and Rollback

The persistence layer supports safe rollback through configuration versioning:

```csharp
// Get a previous version
var oldVersion = await backplane.GetConfigurationVersionAsync("my-experiment", versionNumber: 5);

// Create a new version with the old configuration (rollback)
var rollbackVersion = new PersistedConfigurationVersion
{
    ExperimentName = "my-experiment",
    VersionNumber = 7, // Next version
    ConfigurationJson = oldVersion.ConfigurationJson,
    CreatedAt = DateTimeOffset.UtcNow,
    CreatedBy = "admin@example.com",
    ChangeDescription = "Rolled back to version 5",
    ConfigurationHash = ComputeHash(oldVersion.ConfigurationJson),
    IsRollback = true,
    RolledBackFrom = 6
};

await backplane.AppendConfigurationVersionAsync(rollbackVersion);
```

## Testing

For unit tests, use the in-memory backplane:

```csharp
[Fact]
public async Task TestExperimentLifecycle()
{
    var backplane = new InMemoryGovernancePersistenceBackplane();
    
    // Test governance operations
    var state = new PersistedExperimentState
    {
        ExperimentName = "test-experiment",
        CurrentState = ExperimentLifecycleState.Draft,
        ETag = Guid.NewGuid().ToString(),
        LastModified = DateTimeOffset.UtcNow
    };
    
    var result = await backplane.SaveExperimentStateAsync(state);
    Assert.True(result.Success);
}
```

## See Also

- [Governance Documentation](../ExperimentFramework.Governance/README.md)
- [Configuration DSL Documentation](../ExperimentFramework.Configuration/README.md)
