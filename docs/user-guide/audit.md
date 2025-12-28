# Audit Logging

The Audit package provides comprehensive audit trail capabilities for experiment lifecycle events, enabling compliance, debugging, and operational visibility.

## Installation

```bash
dotnet add package ExperimentFramework.Audit
```

## Quick Start

```csharp
// Register audit with logging sink
services.AddExperimentAudit(options =>
{
    options.EnableLoggingAudit = true;
    options.LogLevel = LogLevel.Information;
});
```

## Core Concepts

### Audit Events

All experiment-related actions are captured as audit events:

```csharp
public sealed class AuditEvent
{
    public required string EventType { get; init; }      // e.g., "VariantSelected", "ExperimentStarted"
    public required string ExperimentName { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? VariantKey { get; init; }
    public string? UserId { get; init; }
    public string? CorrelationId { get; init; }
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
```

### Audit Sink Interface

Implement `IAuditSink` to capture events:

```csharp
public interface IAuditSink
{
    ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
```

## Built-in Sinks

### Logging Sink

Writes audit events to `ILogger`:

```csharp
services.AddExperimentAudit(options =>
{
    options.EnableLoggingAudit = true;
    options.LogLevel = LogLevel.Information;
    options.LoggerCategoryName = "ExperimentAudit";
});
```

Output:
```
info: ExperimentAudit[0]
      Experiment event: VariantSelected, Experiment: IPaymentProcessor,
      Variant: stripe-processor, User: user-123, CorrelationId: abc-123
```

### Composite Sink

Combine multiple sinks:

```csharp
services.AddExperimentAudit()
    .AddAuditSink<LoggingAuditSink>()
    .AddAuditSink<DatabaseAuditSink>()
    .AddAuditSink<EventHubAuditSink>();
```

## Custom Audit Sinks

### Database Sink

```csharp
public class DatabaseAuditSink : IAuditSink
{
    private readonly AuditDbContext _dbContext;

    public DatabaseAuditSink(AuditDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        var entity = new AuditRecord
        {
            EventType = auditEvent.EventType,
            ExperimentName = auditEvent.ExperimentName,
            VariantKey = auditEvent.VariantKey,
            UserId = auditEvent.UserId,
            CorrelationId = auditEvent.CorrelationId,
            Timestamp = auditEvent.Timestamp,
            MetadataJson = JsonSerializer.Serialize(auditEvent.Metadata)
        };

        _dbContext.AuditRecords.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

// Register
services.AddExperimentAudit()
    .AddAuditSink<DatabaseAuditSink>();
```

### Event Hub/Kafka Sink

```csharp
public class EventHubAuditSink : IAuditSink
{
    private readonly EventHubProducerClient _client;

    public EventHubAuditSink(EventHubProducerClient client)
    {
        _client = client;
    }

    public async ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        var eventData = new EventData(JsonSerializer.SerializeToUtf8Bytes(auditEvent));
        eventData.Properties["EventType"] = auditEvent.EventType;
        eventData.Properties["ExperimentName"] = auditEvent.ExperimentName;

        await _client.SendAsync(new[] { eventData }, cancellationToken);
    }
}
```

### Application Insights Sink

```csharp
public class ApplicationInsightsAuditSink : IAuditSink
{
    private readonly TelemetryClient _telemetryClient;

    public ApplicationInsightsAuditSink(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }

    public ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        var properties = new Dictionary<string, string>
        {
            ["ExperimentName"] = auditEvent.ExperimentName,
            ["VariantKey"] = auditEvent.VariantKey ?? "",
            ["UserId"] = auditEvent.UserId ?? "",
            ["CorrelationId"] = auditEvent.CorrelationId ?? ""
        };

        // Add custom metadata
        if (auditEvent.Metadata != null)
        {
            foreach (var (key, value) in auditEvent.Metadata)
            {
                properties[$"Metadata_{key}"] = value?.ToString() ?? "";
            }
        }

        _telemetryClient.TrackEvent($"Experiment_{auditEvent.EventType}", properties);

        return ValueTask.CompletedTask;
    }
}
```

## Event Types

The framework emits the following event types:

| Event Type | Description |
|------------|-------------|
| `ExperimentStarted` | Experiment was activated |
| `ExperimentStopped` | Experiment was deactivated |
| `VariantSelected` | A variant was selected for a request |
| `VariantAssigned` | User was permanently assigned to a variant |
| `OutcomeRecorded` | A conversion or outcome was recorded |
| `ConfigurationChanged` | Experiment configuration was modified |
| `ErrorOccurred` | An error occurred during experiment processing |

## Configuration Options

```csharp
services.AddExperimentAudit(options =>
{
    // General settings
    options.Enabled = true;
    options.IncludeUserContext = true;
    options.IncludeMetadata = true;

    // Logging sink
    options.EnableLoggingAudit = true;
    options.LogLevel = LogLevel.Information;
    options.LoggerCategoryName = "ExperimentFramework.Audit";

    // Filtering
    options.EventFilter = evt => evt.EventType != "VariantSelected"; // Exclude high-volume events
    options.ExperimentFilter = name => !name.StartsWith("Internal."); // Exclude internal experiments

    // Enrichment
    options.Enrichers.Add(new EnvironmentEnricher());
});
```

### Custom Enrichers

Add additional context to all audit events:

```csharp
public class EnvironmentEnricher : IAuditEventEnricher
{
    private readonly IWebHostEnvironment _environment;

    public EnvironmentEnricher(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public void Enrich(AuditEvent auditEvent)
    {
        auditEvent.Metadata["Environment"] = _environment.EnvironmentName;
        auditEvent.Metadata["MachineName"] = Environment.MachineName;
        auditEvent.Metadata["AppVersion"] = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
    }
}

services.AddExperimentAudit(options =>
{
    options.Enrichers.Add<EnvironmentEnricher>();
});
```

## Querying Audit Data

### SQL Example

```sql
-- Find all variant selections for a user
SELECT * FROM AuditRecords
WHERE EventType = 'VariantSelected'
  AND UserId = 'user-123'
ORDER BY Timestamp DESC;

-- Experiment activity summary
SELECT ExperimentName, EventType, COUNT(*) as Count
FROM AuditRecords
WHERE Timestamp > DATEADD(day, -7, GETUTCDATE())
GROUP BY ExperimentName, EventType
ORDER BY ExperimentName, EventType;

-- Find configuration changes
SELECT * FROM AuditRecords
WHERE EventType = 'ConfigurationChanged'
ORDER BY Timestamp DESC;
```

### Cosmos DB Example

```csharp
var query = new QueryDefinition(@"
    SELECT * FROM c
    WHERE c.EventType = 'VariantSelected'
      AND c.ExperimentName = @experimentName
      AND c.Timestamp > @since
    ORDER BY c.Timestamp DESC")
    .WithParameter("@experimentName", "IPaymentProcessor")
    .WithParameter("@since", DateTimeOffset.UtcNow.AddDays(-7));
```

## Real-World Examples

### Compliance Reporting

```csharp
public class ComplianceReportGenerator
{
    private readonly IAuditQueryService _auditService;

    public async Task<ComplianceReport> GenerateReportAsync(
        DateTimeOffset from, DateTimeOffset to)
    {
        var events = await _auditService.QueryAsync(new AuditQuery
        {
            From = from,
            To = to,
            EventTypes = new[] { "VariantAssigned", "ConfigurationChanged" }
        });

        return new ComplianceReport
        {
            TotalAssignments = events.Count(e => e.EventType == "VariantAssigned"),
            ConfigurationChanges = events
                .Where(e => e.EventType == "ConfigurationChanged")
                .Select(e => new ConfigChange
                {
                    Experiment = e.ExperimentName,
                    Timestamp = e.Timestamp,
                    ChangedBy = e.UserId
                })
                .ToList()
        };
    }
}
```

### Debugging User Experience

```csharp
public class UserExperienceDebugger
{
    private readonly IAuditQueryService _auditService;

    public async Task<UserExperimentHistory> GetUserHistoryAsync(string userId)
    {
        var events = await _auditService.QueryAsync(new AuditQuery
        {
            UserId = userId,
            OrderBy = "Timestamp",
            Limit = 100
        });

        return new UserExperimentHistory
        {
            UserId = userId,
            Events = events.Select(e => new UserEvent
            {
                Timestamp = e.Timestamp,
                Experiment = e.ExperimentName,
                Variant = e.VariantKey,
                EventType = e.EventType
            }).ToList()
        };
    }
}
```

### Alerting on Errors

```csharp
public class AuditAlertingService : BackgroundService
{
    private readonly IAuditEventStream _eventStream;
    private readonly IAlertService _alertService;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var auditEvent in _eventStream.StreamAsync(stoppingToken))
        {
            if (auditEvent.EventType == "ErrorOccurred")
            {
                await _alertService.SendAlertAsync(new Alert
                {
                    Severity = AlertSeverity.Warning,
                    Title = $"Experiment Error: {auditEvent.ExperimentName}",
                    Message = auditEvent.Metadata?["ErrorMessage"]?.ToString(),
                    CorrelationId = auditEvent.CorrelationId
                });
            }
        }
    }
}
```

## Best Practices

1. **Choose appropriate sinks**: Use logging for development, dedicated storage for production
2. **Filter high-volume events**: Consider sampling or filtering `VariantSelected` events
3. **Include correlation IDs**: Enable request tracing across services
4. **Set retention policies**: Implement data retention to manage storage costs
5. **Secure sensitive data**: Be careful about PII in metadata fields

## Troubleshooting

### Missing audit events

**Symptom**: Some experiment events not appearing in audit logs.

**Cause**: Sink failing silently or events filtered out.

**Solution**: Enable sink error logging:

```csharp
services.AddExperimentAudit(options =>
{
    options.ThrowOnSinkError = false;  // Don't break the app
    options.LogSinkErrors = true;      // But do log errors
});
```

### Performance impact

**Symptom**: Increased latency on experiment-enabled requests.

**Cause**: Synchronous audit writing.

**Solution**: Use async/buffered sinks:

```csharp
public class BufferedAuditSink : IAuditSink
{
    private readonly Channel<AuditEvent> _channel = Channel.CreateBounded<AuditEvent>(1000);

    public ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        // Non-blocking write to channel
        _channel.Writer.TryWrite(auditEvent);
        return ValueTask.CompletedTask;
    }

    // Background task processes the channel
    public async Task ProcessAsync(CancellationToken stoppingToken)
    {
        await foreach (var batch in _channel.Reader.ReadAllAsync(stoppingToken).Batch(100))
        {
            await PersistBatchAsync(batch);
        }
    }
}
```

### Large metadata payloads

**Symptom**: Audit records consuming excessive storage.

**Cause**: Including large objects in metadata.

**Solution**: Filter and limit metadata:

```csharp
options.MetadataFilter = (key, value) =>
{
    // Exclude large values
    if (value is string s && s.Length > 1000)
        return false;

    // Exclude internal keys
    if (key.StartsWith("_"))
        return false;

    return true;
};
```
