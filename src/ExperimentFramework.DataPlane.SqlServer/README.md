# ExperimentFramework.DataPlane.SqlServer

SQL Server-based durable data backplane for ExperimentFramework using EF Core 10, providing queryable, relational storage for experimentation telemetry.

## Features

- **Relational Storage**: Store events in SQL Server with full query capabilities
- **EF Core 10**: Modern Entity Framework Core with migrations support
- **Idempotency**: Optional duplicate detection using event IDs
- **Batching**: Efficient bulk inserts for improved throughput
- **Indexing**: Optimized indexes for common query patterns
- **Transactions**: ACID compliance with transactional writes
- **Auto-Migration**: Optional automatic database migration on startup
- **DSL Integration**: Full support for declarative YAML/JSON configuration

## Installation

```bash
dotnet add package ExperimentFramework.DataPlane.SqlServer
```

## Quick Start

### Programmatic Configuration

```csharp
using ExperimentFramework.DataPlane.SqlServer;

services.AddSqlServerDataBackplane(options =>
{
    options.ConnectionString = "Server=localhost;Database=ExperimentFramework;...";
    options.Schema = "dbo";
    options.TableName = "ExperimentEvents";
    options.BatchSize = 100;
    options.EnableIdempotency = true;
    options.AutoMigrate = false;  // Set to true for automatic migrations
});
```

### DSL Configuration

```yaml
experimentFramework:
  dataPlane:
    backplane:
      type: sqlServer
      options:
        connectionString: "Server=localhost;Database=ExperimentFramework;..."
        schema: dbo
        tableName: ExperimentEvents
        batchSize: 100
        enableIdempotency: true
        autoMigrate: false
```

Then register the handler:

```csharp
services.AddConfigurationBackplaneHandler<SqlServerBackplaneConfigurationHandler>();
services.AddExperimentFrameworkFromConfiguration(configuration);
```

## Database Schema

The backplane creates a table with the following structure:

```sql
CREATE TABLE [dbo].[ExperimentEvents] (
    [Id] bigint NOT NULL IDENTITY,
    [EventId] nvarchar(100) NOT NULL,
    [Timestamp] datetimeoffset NOT NULL,
    [EventType] nvarchar(50) NOT NULL,
    [SchemaVersion] nvarchar(20) NOT NULL,
    [PayloadJson] nvarchar(max) NOT NULL,
    [CorrelationId] nvarchar(100) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [PK_ExperimentEvents] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_ExperimentEvents_EventId] ON [ExperimentEvents] ([EventId]);
CREATE INDEX [IX_ExperimentEvents_EventType] ON [ExperimentEvents] ([EventType]);
CREATE INDEX [IX_ExperimentEvents_Timestamp] ON [ExperimentEvents] ([Timestamp]);
CREATE INDEX [IX_ExperimentEvents_CorrelationId] ON [ExperimentEvents] ([CorrelationId]);
CREATE INDEX [IX_ExperimentEvents_CreatedAt] ON [ExperimentEvents] ([CreatedAt]);
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ConnectionString` | `string` | **Required** | SQL Server connection string |
| `Schema` | `string` | `"dbo"` | Database schema name |
| `TableName` | `string` | `"ExperimentEvents"` | Table name for events |
| `BatchSize` | `int` | `100` | Batch size for bulk inserts |
| `EnableIdempotency` | `bool` | `true` | Check for duplicate event IDs |
| `AutoMigrate` | `bool` | `false` | Auto-apply migrations on startup |
| `CommandTimeoutSeconds` | `int` | `30` | SQL command timeout |

## Migrations

### Apply Migrations

**Option 1: Automatic (Development)**
```csharp
options.AutoMigrate = true;  // Applies migrations on startup
```

**Option 2: Manual (Production)**
```bash
cd src/ExperimentFramework.DataPlane.SqlServer
dotnet ef database update
```

**Option 3: Generate SQL Script**
```bash
dotnet ef migrations script --output migrations.sql
```

### Create New Migrations

If you customize the schema:

```bash
dotnet ef migrations add YourMigrationName
```

## Querying Events

Access the DbContext to query stored events:

```csharp
using var scope = serviceProvider.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ExperimentDataContext>();

// Query recent exposures
var recentExposures = await context.ExperimentEvents
    .Where(e => e.EventType == "Exposure" && e.Timestamp > DateTimeOffset.UtcNow.AddDays(-7))
    .OrderByDescending(e => e.Timestamp)
    .ToListAsync();

// Query by experiment
var experimentEvents = await context.ExperimentEvents
    .Where(e => e.PayloadJson.Contains("\"experimentName\":\"IRecommendationService\""))
    .ToListAsync();

// Query by correlation ID
var relatedEvents = await context.ExperimentEvents
    .Where(e => e.CorrelationId == "request-abc-123")
    .OrderBy(e => e.Timestamp)
    .ToListAsync();
```

## Event Schema

Events are stored with JSON payloads:

```json
{
  "id": 12345,
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2025-12-30T06:00:00Z",
  "eventType": "Exposure",
  "schemaVersion": "1.0.0",
  "payloadJson": "{\"experimentName\":\"IRecommendationService\",...}",
  "correlationId": "request-abc-123",
  "metadataJson": "{}",
  "createdAt": "2025-12-30T06:00:01Z"
}
```

## Idempotency

When `EnableIdempotency = true`, the backplane:
- Checks for existing `EventId` before inserting
- Skips duplicate events silently
- Logs the number of skipped duplicates

This prevents double-counting in analytics scenarios.

## Health Checks

```csharp
var backplane = serviceProvider.GetRequiredService<IDataBackplane>();
var health = await backplane.HealthAsync();

if (health.IsHealthy)
{
    Console.WriteLine($"SQL Server backplane is healthy: {health.Message}");
}
```

## Connection Strings

### Windows Authentication
```
Server=localhost;Database=ExperimentFramework;Trusted_Connection=True;
```

### SQL Authentication
```
Server=localhost;Database=ExperimentFramework;User Id=sa;Password=YourPassword;
```

### Azure SQL
```
Server=tcp:yourserver.database.windows.net,1433;Database=ExperimentFramework;User ID=yourusername;Password=yourpassword;Encrypt=True;TrustServerCertificate=False;
```

## Performance Considerations

- **Batching**: Adjust `BatchSize` based on your workload (100-1000)
- **Indexes**: Additional indexes can be added for custom queries
- **Partitioning**: Consider table partitioning for large-scale deployments
- **Retention**: Implement archival/cleanup for old events
- **Connection Pooling**: Enabled by default in SQL Server

## Use Cases

- Direct SQL querying of experiment events
- Long-term audit and compliance storage
- Smaller deployments without streaming infrastructure
- Integration with existing SQL-based analytics
- Join experiments with other business data

## Requirements

- .NET 10.0+
- Entity Framework Core 10
- SQL Server 2016+ or Azure SQL Database

## License

Same as ExperimentFramework main library.
