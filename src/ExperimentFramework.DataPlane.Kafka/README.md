# ExperimentFramework.DataPlane.Kafka

Kafka-based durable data backplane for ExperimentFramework, providing high-throughput, scalable event streaming for experimentation telemetry.

## Features

- **Durable Event Streaming**: Reliable delivery with Kafka's persistence guarantees
- **Configurable Partitioning**: Route events by experiment key, subject ID, tenant ID, or round-robin
- **Batching & Compression**: Optimized throughput with configurable batching and compression
- **Idempotent Producer**: Ensure exactly-once semantics
- **DSL Integration**: Full support for declarative YAML/JSON configuration
- **Health Monitoring**: Built-in health checks for operational visibility

## Installation

```bash
dotnet add package ExperimentFramework.DataPlane.Kafka
```

## Quick Start

### Programmatic Configuration

```csharp
using ExperimentFramework.DataPlane.Kafka;

services.AddKafkaDataBackplane(options =>
{
    options.Brokers = new List<string> { "localhost:9092" };
    options.PartitionStrategy = KafkaPartitionStrategy.ByExperimentKey;
    options.BatchSize = 500;
    options.LingerMs = 100;
    options.EnableIdempotence = true;
});
```

### DSL Configuration

```yaml
experimentFramework:
  dataPlane:
    backplane:
      type: kafka
      options:
        brokers:
          - localhost:9092
          - broker2:9092
        partitionBy: experimentKey
        batchSize: 500
        lingerMs: 100
        compressionType: snappy
        acks: all
```

Then register the handler in your application:

```csharp
services.AddConfigurationBackplaneHandler<KafkaBackplaneConfigurationHandler>();
services.AddExperimentFrameworkFromConfiguration(configuration);
```

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Brokers` | `List<string>` | **Required** | Kafka broker addresses |
| `Topic` | `string?` | `null` | Unified topic name (if null, uses type-specific topics) |
| `PartitionStrategy` | `KafkaPartitionStrategy` | `ByExperimentKey` | Partitioning strategy |
| `BatchSize` | `int` | `500` | Producer batch size |
| `LingerMs` | `int` | `100` | Linger time for batching |
| `EnableIdempotence` | `bool` | `true` | Enable idempotent producer |
| `CompressionType` | `string` | `"snappy"` | Compression type (none, gzip, snappy, lz4, zstd) |
| `Acks` | `string` | `"all"` | Acknowledgement mode (all, 1, 0) |
| `ClientId` | `string?` | `null` | Client identifier |

### Partitioning Strategies

- **ByExperimentKey**: Partition by experiment name (ensures all events for an experiment go to same partition)
- **BySubjectId**: Partition by subject ID (ensures all events for a subject go to same partition)
- **ByTenantId**: Partition by tenant ID (multi-tenant scenarios)
- **RoundRobin**: Distribute events evenly across partitions

## Topic Naming

If `Topic` is not specified, events are automatically routed to type-specific topics:

- `experiment-exposures`
- `experiment-assignments`
- `experiment-outcomes`
- `experiment-analysis-signals`
- `experiment-errors`

## Event Schema

All events are serialized as JSON within a `DataPlaneEnvelope`:

```json
{
  "eventId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2025-12-30T06:00:00Z",
  "eventType": "Exposure",
  "schemaVersion": "1.0.0",
  "payload": {
    "experimentName": "IRecommendationService",
    "variantKey": "ml-algorithm",
    "subjectId": "user-12345",
    ...
  },
  "correlationId": "request-abc-123",
  "metadata": {}
}
```

Headers are also added to Kafka messages:
- `event-id`
- `event-type`
- `schema-version`
- `correlation-id` (if present)

## Health Checks

The backplane supports health checking:

```csharp
var backplane = serviceProvider.GetRequiredService<IDataBackplane>();
var health = await backplane.HealthAsync();

if (health.IsHealthy)
{
    Console.WriteLine($"Kafka backplane is healthy: {health.Message}");
}
```

## Examples

See [kafka-backplane-example.yaml](/samples/ExperimentDefinitions/kafka-backplane-example.yaml) for a complete example.

## Requirements

- .NET 10.0+
- Confluent.Kafka
- Access to Kafka broker(s)

## License

Same as ExperimentFramework main library.
