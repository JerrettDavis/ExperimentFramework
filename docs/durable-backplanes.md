# Durable Data Backplane Connectors

This document describes the durable, production-ready backplane connectors available for ExperimentFramework.

## Overview

Durable backplane connectors allow experimentation telemetry, state, and analysis events to be persisted and streamed reliably to external systems. These connectors are fully compatible with the **ExperimentBuilder YAML/JSON DSL**, enabling declarative configuration of data pipelines.

## Available Backplanes

### 1. Kafka Backplane

**Package**: `ExperimentFramework.DataPlane.Kafka`

A stream-oriented backplane for high-throughput, durable event pipelines.

#### Features

- Configurable topics per event type or unified topic
- Partitioning strategies (by experiment key, subject ID, tenant ID, or round-robin)
- Idempotent producer support
- Batching and async flush behavior
- Schema version propagation for downstream consumers

#### Configuration Example

```yaml
dataPlane:
  backplane:
    type: kafka
    options:
      brokers:
        - broker1:9092
        - broker2:9092
      topic: experiment-events  # Optional: use single topic for all events
      partitionBy: experimentKey  # experimentKey, subjectId, tenantId, roundRobin
      batchSize: 500
      lingerMs: 100
      enableIdempotence: true
      compressionType: snappy  # none, gzip, snappy, lz4, zstd
      acks: all  # all, 1, 0
      clientId: my-experiment-app
```

#### Programmatic Configuration

```csharp
services.AddKafkaDataBackplane(options =>
{
    options.Brokers = new List<string> { "localhost:9092" };
    options.PartitionStrategy = KafkaPartitionStrategy.ByExperimentKey;
    options.BatchSize = 500;
    options.LingerMs = 100;
});
```

#### Topic Naming

If no unified `topic` is specified, events are routed to type-specific topics:

- `experiment-exposures` - Exposure events
- `experiment-assignments` - Assignment events
- `experiment-outcomes` - Outcome events
- `experiment-analysis-signals` - Analysis signals
- `experiment-errors` - Error events

#### Use Cases

- Real-time analytics pipelines
- Data warehouse ingestion
- Cross-service experimentation
- Event sourcing

---

### 2. Azure Service Bus Backplane

**Package**: `ExperimentFramework.DataPlane.AzureServiceBus`

A cloud-hosted durable messaging backplane for Azure environments.

#### Features

- Queue and topic/subscription support
- Message grouping with sessions for ordering guarantees
- Dead-letter handling and retry semantics with exponential backoff
- Integration with Azure-native data processing
- Configurable TTL and retention policies
- Batching for improved throughput

#### Configuration Example

```yaml
dataPlane:
  backplane:
    type: azureServiceBus
    options:
      connectionString: "Endpoint=sb://myns.servicebus.windows.net/;..."
      queueName: experiment-events  # or use topicName
      batchSize: 100
      maxRetryAttempts: 3
      messageTimeToLiveMinutes: 1440  # 24 hours
      enableSessions: false
      sessionStrategy: experimentKey  # experimentKey, subjectId, tenantId
```

#### Programmatic Configuration

```csharp
services.AddAzureServiceBusDataBackplane(options =>
{
    options.ConnectionString = "Endpoint=sb://...";
    options.QueueName = "experiment-events";
    options.BatchSize = 100;
    options.MaxRetryAttempts = 3;
});
```

#### Destination Modes

**Queue Mode** (single consumer):
```yaml
options:
  queueName: experiment-events
```

**Topic Mode** (multiple subscribers):
```yaml
options:
  topicName: experiment-events
```

**Type-Specific Destinations**:
```yaml
options:
  useTypeSpecificDestinations: true
```

#### Use Cases

- Reliable delivery across Azure services
- Integration with Azure-native workflows (Functions, Logic Apps)
- Multi-subscriber scenarios with topics
- Guaranteed message ordering with sessions

---

### 3. SQL Server Backplane

**Package**: `ExperimentFramework.DataPlane.SqlServer`

**Status**: Coming Soon

A relational, append-only persistence backplane for queryable event storage.

#### Planned Features

- Normalized or denormalized tables for events
- Explicit schema versioning
- Indexing strategies for common joins
- Retention and partitioning guidance
- Transactional writes with idempotency keys

#### Example Configuration (Preview)

```yaml
dataPlane:
  backplane:
    type: sqlServer
    options:
      connectionString: "Server=...;Database=ExperimentData;..."
      schema: dbo
      eventsTable: ExperimentEvents
      batchInsertSize: 100
      enableIdempotency: true
```

---

## DSL Integration

All backplanes are configured through the ExperimentBuilder YAML/JSON DSL. The configuration is validated at parse time, providing early feedback on configuration errors.

### Registering Custom Backplane Handlers

Extension packages can register custom backplane handlers:

```csharp
services.AddConfigurationBackplaneHandler<MyCustomBackplaneHandler>();
```

Your handler must implement `IConfigurationBackplaneHandler`:

```csharp
public class MyCustomBackplaneHandler : IConfigurationBackplaneHandler
{
    public string BackplaneType => "myCustomBackplane";

    public void ConfigureServices(IServiceCollection services, 
        DataPlaneBackplaneConfig config, ILogger? logger)
    {
        // Extract options and configure services
    }

    public IEnumerable<ConfigurationValidationError> Validate(
        DataPlaneBackplaneConfig config, string path)
    {
        // Validate configuration
        return Enumerable.Empty<ConfigurationValidationError>();
    }
}
```

---

## Data Plane Options

Common data plane options that apply to all backplanes:

```yaml
dataPlane:
  # Event type toggles
  enableExposureEvents: true
  enableAssignmentEvents: true
  enableOutcomeEvents: true
  enableAnalysisSignals: true
  enableErrorEvents: true
  
  # Sampling and performance
  samplingRate: 1.0  # 0.0-1.0 (1.0 = 100%)
  batchSize: 500
  flushIntervalMs: 1000
  
  # Backplane selection
  backplane:
    type: kafka  # or azureServiceBus, sqlServer, inMemory, logging, openTelemetry
    options:
      # Backplane-specific options
```

---

## Event Schemas

All backplanes preserve standardized event envelopes and schema versions:

### DataPlaneEnvelope

```csharp
public sealed class DataPlaneEnvelope
{
    public string EventId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public DataPlaneEventType EventType { get; init; }
    public string SchemaVersion { get; init; }
    public object Payload { get; init; }
    public string? CorrelationId { get; init; }
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
```

### Event Types

- **Exposure**: Subject exposed to a variant
- **Assignment**: Subject's assignment changed
- **Outcome**: Experiment outcome recorded
- **AnalysisSignal**: Statistical or science signal
- **Error**: Error during experiment execution

---

## Health Checks

All backplanes implement health checking:

```csharp
var health = await backplane.HealthAsync();
Console.WriteLine($"Healthy: {health.IsHealthy}, Message: {health.Message}");
```

---

## Examples

See the [samples directory](/samples/ExperimentDefinitions/) for complete examples:

- `kafka-backplane-example.yaml` - Kafka configuration
- `azure-service-bus-backplane-example.yaml` - Azure Service Bus configuration
- More examples coming soon

---

## Installation

```bash
# Kafka backplane
dotnet add package ExperimentFramework.DataPlane.Kafka

# Azure Service Bus
dotnet add package ExperimentFramework.DataPlane.AzureServiceBus

# SQL Server (coming soon)
dotnet add package ExperimentFramework.DataPlane.SqlServer
```

---

## Requirements

- .NET 10.0 or later
- ExperimentFramework.DataPlane.Abstractions
- ExperimentFramework.Configuration (for DSL support)

For Kafka:
- Confluent.Kafka NuGet package
- Access to Kafka broker(s)

---

## License

Same as ExperimentFramework main library.
