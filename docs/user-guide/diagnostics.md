# Diagnostics & Tracing

The `ExperimentFramework.Diagnostics` package provides standardized event capture and observability for experiments. It enables consistent logging, test assertions, and telemetry across your experiments.

## Overview

The Diagnostics package provides:

- **Event Model**: Discriminated union-style `ExperimentEvent` types for capturing experiment lifecycle events
- **Event Sinks**: Pluggable sinks for capturing events:
  - `InMemoryExperimentEventSink` - For testing and assertions
  - `LoggerExperimentEventSink` - Structured logging with ILogger
  - `OpenTelemetryExperimentEventSink` - Activities and metrics for OpenTelemetry
- **Composite Sinks**: Combine multiple sinks with deterministic ordering
- **Minimal Allocations**: Designed for high-performance with `in` parameter passing

## Quick Start

### Installation

```bash
dotnet add package ExperimentFramework.Diagnostics
```

### Basic Usage

```csharp
using ExperimentFramework.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Add event sinks
services.AddLoggerExperimentEventSink();
services.AddOpenTelemetryExperimentEventSink();
services.AddInMemoryExperimentEventSink(maxCapacity: 1000);

// Build service provider
var provider = services.BuildServiceProvider();

// Get composite sink (all sinks combined)
var sinks = provider.GetExperimentEventSinks();
```

## Event Types

The `ExperimentEvent` type captures various experiment lifecycle events:

### Event Kinds

| Event Kind | Description | When Emitted |
|------------|-------------|--------------|
| `TrialStarted` | Trial invocation started | When experiment proxy begins execution |
| `TrialEnded` | Trial invocation ended | When experiment proxy completes (success or failure) |
| `RouteSelected` | Route (trial key) selected | When selection mode determines which trial to use |
| `FallbackOccurred` | Fallback to another trial | When error policy triggers fallback |
| `ExceptionThrown` | Exception during execution | When a trial throws an exception |
| `MethodInvoked` | Method invocation started | Decorator-level tracking (optional) |
| `MethodCompleted` | Method invocation completed | Decorator-level tracking (optional) |

### Event Properties

```csharp
public sealed record ExperimentEvent
{
    public ExperimentEventKind Kind { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public Type ServiceType { get; init; }
    public string MethodName { get; init; }
    public string TrialKey { get; init; }
    public string? SelectorName { get; init; }
    public Exception? Exception { get; init; }
    public string? FallbackKey { get; init; }
    public TimeSpan? Duration { get; init; }
    public bool? Success { get; init; }
    public IReadOnlyDictionary<string, object?>? Context { get; init; }
}
```

## Event Sinks

### InMemoryExperimentEventSink

In-memory sink for testing and assertions. Supports both bounded (ring buffer) and unbounded storage.

**Unbounded Mode** (for tests):

```csharp
services.AddInMemoryExperimentEventSink();
```

**Bounded Mode** (ring buffer):

```csharp
services.AddInMemoryExperimentEventSink(maxCapacity: 1000);
```

**Usage in Tests**:

```csharp
var sink = provider.GetRequiredService<InMemoryExperimentEventSink>();

// Wait for events
await Task.Delay(100);

// Assert on captured events
var startedEvents = sink.GetEventsByKind(ExperimentEventKind.TrialStarted);
Assert.Equal(2, startedEvents.Count);

var fallbacks = sink.GetEvents(e => e.Kind == ExperimentEventKind.FallbackOccurred);
Assert.Single(fallbacks);

// Clear for next test
sink.Clear();
```

### LoggerExperimentEventSink

Structured logging sink that writes events to ILogger with proper event IDs and log levels.

```csharp
services.AddLogging();
services.AddLoggerExperimentEventSink();

// Or with custom category
services.AddLoggerExperimentEventSink("MyApp.Experiments");
```

**Event ID Mapping**:

| Event Kind | Event ID | Log Level |
|------------|----------|-----------|
| `TrialStarted` | 1001 | Debug |
| `TrialEnded` (success) | 1002 | Information |
| `TrialEnded` (failure) | 1002 | Warning |
| `RouteSelected` | 1003 | Debug |
| `FallbackOccurred` | 1004 | Warning |
| `ExceptionThrown` | 1005 | Error |
| `MethodInvoked` | 1006 | Trace |
| `MethodCompleted` | 1007 | Trace |

**Structured Properties**:

All events include structured properties for filtering and analysis:
- `EventKind`
- `ServiceType`
- `MethodName`
- `TrialKey`
- `SelectorName`
- `DurationMs` (when applicable)
- `Success` (when applicable)
- `FallbackKey` (when applicable)
- `Context.*` (custom context properties)

### OpenTelemetryExperimentEventSink

Emits OpenTelemetry activities and metrics using BCL types (no external dependencies required).

```csharp
services.AddOpenTelemetryExperimentEventSink();
```

**Metrics Emitted**:

| Metric Name | Type | Description |
|-------------|------|-------------|
| `experiment.trial.started` | Counter | Number of trial invocations started |
| `experiment.trial.ended` | Counter | Number of trial invocations ended |
| `experiment.trial.duration` | Histogram | Duration of trial invocations (ms) |
| `experiment.route.selected` | Counter | Number of routes selected |
| `experiment.fallback.occurred` | Counter | Number of fallback occurrences |
| `experiment.exception.thrown` | Counter | Number of exceptions thrown |

**Activity Tags**:

All activities include tags for correlation:
- `event.kind`
- `service.type`
- `method.name`
- `trial.key`
- `selector.name`
- `event.timestamp`
- `duration.ms` (when applicable)
- `success` (when applicable)

**Performance Note**: Activities are only emitted for significant events (FallbackOccurred, ExceptionThrown) to reduce overhead. All events emit metrics.

## Composite Sinks

Combine multiple sinks for multi-channel observability:

```csharp
// Explicit composite
var compositeSink = new CompositeExperimentEventSink(
    new InMemoryExperimentEventSink(),
    new LoggerExperimentEventSink(logger),
    new OpenTelemetryExperimentEventSink()
);

// Or use extension method (recommended)
services.AddInMemoryExperimentEventSink();
services.AddLoggerExperimentEventSink();
services.AddOpenTelemetryExperimentEventSink();

var provider = services.BuildServiceProvider();
var sinks = provider.GetExperimentEventSinks(); // Returns CompositeExperimentEventSink
```

**Ordering**: Events are forwarded to sinks in registration order. If a sink throws an exception, other sinks continue to receive events.

## Advanced Scenarios

### Custom Event Sinks

Implement `IExperimentEventSink` for custom behavior:

```csharp
public class CustomEventSink : IExperimentEventSink
{
    public void OnEvent(in ExperimentEvent e)
    {
        // Custom handling
        if (e.Kind == ExperimentEventKind.FallbackOccurred)
        {
            // Send alert, update metrics, etc.
        }
    }
}

services.AddExperimentEventSink<CustomEventSink>();
```

### Filtering Events

Filter events at the sink level:

```csharp
public class FilteredEventSink : IExperimentEventSink
{
    private readonly IExperimentEventSink _innerSink;

    public FilteredEventSink(IExperimentEventSink innerSink)
    {
        _innerSink = innerSink;
    }

    public void OnEvent(in ExperimentEvent e)
    {
        // Only forward high-priority events
        if (e.Kind is ExperimentEventKind.FallbackOccurred 
                    or ExperimentEventKind.ExceptionThrown)
        {
            _innerSink.OnEvent(e);
        }
    }
}
```

### Adding Context Data

Enrich events with custom context:

```csharp
var context = new Dictionary<string, object?>
{
    ["userId"] = currentUser.Id,
    ["requestId"] = httpContext.TraceIdentifier,
    ["environment"] = "production"
};

var evt = new ExperimentEvent
{
    Kind = ExperimentEventKind.TrialStarted,
    Timestamp = DateTimeOffset.UtcNow,
    ServiceType = typeof(IMyService),
    MethodName = "ProcessOrder",
    TrialKey = "new-checkout",
    Context = context
};

sink.OnEvent(evt);
```

## Integration with Existing Telemetry

The Diagnostics package complements the existing `IExperimentTelemetry` infrastructure:

- **IExperimentTelemetry**: High-level experiment tracking (scopes, success/failure)
- **IExperimentEventSink**: Fine-grained event capture (lifecycle events, fallbacks)

Use both for comprehensive observability:

```csharp
// Existing telemetry for high-level tracking
services.AddSingleton<IExperimentTelemetry, OpenTelemetryExperimentTelemetry>();

// New Diagnostics for detailed event capture
services.AddOpenTelemetryExperimentEventSink();
services.AddLoggerExperimentEventSink();
```

## Performance Considerations

### Minimal Allocations

Events are passed by reference using `in` parameters:

```csharp
void OnEvent(in ExperimentEvent e); // Passed by reference, no copy
```

### Bounded Storage

Use bounded sinks in production to limit memory:

```csharp
services.AddInMemoryExperimentEventSink(maxCapacity: 1000); // Ring buffer
```

### Selective Activity Emission

OpenTelemetry sink emits activities only for significant events to reduce overhead:

```csharp
// Emits activity: FallbackOccurred, ExceptionThrown
// Emits metric only: TrialStarted, TrialEnded, RouteSelected
```

## Testing

### Example Test

```csharp
[Fact]
public async Task Experiment_WithFallback_RecordsFallbackEvent()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddInMemoryExperimentEventSink();
    services.AddExperimentFramework()
        .AddExperiment<IPaymentService>()
        .WithTrial("new-processor", typeof(NewPaymentProcessor))
        .WithFallback("legacy-processor", typeof(LegacyPaymentProcessor))
        .OnError(OnErrorPolicy.RedirectAndReplayDefault);

    var provider = services.BuildServiceProvider();
    var sink = provider.GetRequiredService<InMemoryExperimentEventSink>();
    var service = provider.GetRequiredService<IPaymentService>();

    // Act
    await service.ProcessPaymentAsync(order);

    // Assert
    var fallbackEvents = sink.GetEventsByKind(ExperimentEventKind.FallbackOccurred);
    Assert.Single(fallbackEvents);
    Assert.Equal("new-processor", fallbackEvents[0].TrialKey);
    Assert.Equal("legacy-processor", fallbackEvents[0].FallbackKey);
}
```

## Best Practices

1. **Use InMemory sink for tests**: Bounded or unbounded depending on test needs
2. **Use Logger sink for debugging**: Structured logs with proper event IDs
3. **Use OpenTelemetry sink for production**: Metrics and activities for observability
4. **Combine sinks**: Use multiple sinks for multi-channel observability
5. **Add context**: Enrich events with user, request, or environment context
6. **Filter at sink level**: Implement custom sinks for event filtering
7. **Bounded storage in production**: Use ring buffers to limit memory usage

## Next Steps

- Learn about [experiment selection modes](/user-guide/selection-modes.md)
- Explore [error policies and fallback strategies](/user-guide/error-policies.md)
- Set up [OpenTelemetry integration](/user-guide/opentelemetry.md)
