# ExperimentFramework.DataPlane.AzureServiceBus

Azure Service Bus-based durable data backplane for ExperimentFramework, providing cloud-native, reliable message delivery for experimentation telemetry.

## Features

- **Durable Message Delivery**: Azure Service Bus persistence and reliability guarantees
- **Flexible Destinations**: Queue or Topic/Subscription modes
- **Session Support**: Message grouping for ordering guarantees
- **Configurable Retry**: Exponential backoff with configurable max attempts
- **Batching**: Efficient message batching for improved throughput
- **DSL Integration**: Full support for declarative YAML/JSON configuration
- **Health Monitoring**: Built-in health checks for operational visibility

## Installation

```bash
dotnet add package ExperimentFramework.DataPlane.AzureServiceBus
```

## Quick Start

### Programmatic Configuration

```csharp
using ExperimentFramework.DataPlane.AzureServiceBus;

services.AddAzureServiceBusDataBackplane(options =>
{
    options.ConnectionString = "Endpoint=sb://...";
    options.QueueName = "experiment-events";  // or use TopicName
    options.BatchSize = 100;
    options.MaxRetryAttempts = 3;
});
```

### DSL Configuration

```yaml
experimentFramework:
  dataPlane:
    backplane:
      type: azureServiceBus
      options:
        connectionString: "Endpoint=sb://myns.servicebus.windows.net/;..."
        queueName: experiment-events
        batchSize: 100
        maxRetryAttempts: 3
        enableSessions: false
```

Then register the handler in your application:

```csharp
services.AddConfigurationBackplaneHandler<AzureServiceBusBackplaneConfigurationHandler>();
services.AddExperimentFrameworkFromConfiguration(configuration);
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ConnectionString` | `string` | **Required** | Azure Service Bus connection string |
| `QueueName` | `string?` | `null` | Queue name (use this OR TopicName) |
| `TopicName` | `string?` | `null` | Topic name (use this OR QueueName) |
| `UseTypeSpecificDestinations` | `bool` | `false` | Route to type-specific queues/topics |
| `MessageTimeToLiveMinutes` | `int?` | `null` | Message TTL in minutes |
| `MaxRetryAttempts` | `int` | `3` | Maximum retry attempts |
| `BatchSize` | `int` | `100` | Batch size for sending messages |
| `EnableSessions` | `bool` | `false` | Enable sessions for ordering |
| `SessionStrategy` | `ServiceBusSessionStrategy` | `ByExperimentKey` | Session grouping strategy |
| `ClientId` | `string?` | `null` | Client identifier |

### Destination Modes

**Queue Mode** (recommended for single consumer):
```yaml
options:
  connectionString: "..."
  queueName: experiment-events
```

**Topic Mode** (for multiple subscribers):
```yaml
options:
  connectionString: "..."
  topicName: experiment-events
```

**Type-Specific Destinations**:
```yaml
options:
  connectionString: "..."
  useTypeSpecificDestinations: true
```

Routes to:
- `experiment-exposures`
- `experiment-assignments`
- `experiment-outcomes`
- `experiment-analysis-signals`
- `experiment-errors`

### Session Strategies

When `enableSessions: true`, messages are grouped by:

- **ByExperimentKey**: All events for an experiment in same session
- **BySubjectId**: All events for a subject in same session
- **ByTenantId**: All events for a tenant in same session

## Event Schema

All events are serialized as JSON within Azure Service Bus messages:

```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2025-12-30T06:00:00Z",
  "eventType": "Exposure",
  "schemaVersion": "1.0.0",
  "payload": {
    "experimentName": "IRecommendationService",
    "variantKey": "ml-algorithm",
    "subjectId": "user-12345"
  },
  "correlationId": "request-abc-123"
}
```

Application properties added to messages:
- `EventType`
- `SchemaVersion`
- `Timestamp`
- `CorrelationId` (if present)

## Health Checks

The backplane supports health checking:

```csharp
var backplane = serviceProvider.GetRequiredService<IDataBackplane>();
var health = await backplane.HealthAsync();

if (health.IsHealthy)
{
    Console.WriteLine($"Azure Service Bus backplane is healthy: {health.Message}");
}
```

## Connection Strings

### From Azure Portal

1. Navigate to your Service Bus namespace
2. Go to "Shared access policies"
3. Select or create a policy with "Send" claims
4. Copy the "Primary Connection String"

### Format

```
Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=<name>;SharedAccessKey=<key>
```

### Using Managed Identity

```csharp
services.AddAzureServiceBusDataBackplane(options =>
{
    options.ConnectionString = "Endpoint=sb://myns.servicebus.windows.net/";
    // Azure SDK will use DefaultAzureCredential
});
```

## Examples

See [azure-service-bus-backplane-example.yaml](/samples/ExperimentDefinitions/azure-service-bus-backplane-example.yaml) for a complete example.

## Requirements

- .NET 10.0+
- Azure.Messaging.ServiceBus
- Azure Service Bus namespace (Standard or Premium tier for sessions)

## License

Same as ExperimentFramework main library.
