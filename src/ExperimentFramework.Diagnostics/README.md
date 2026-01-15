# ExperimentFramework.Diagnostics

Standardized event capture and observability for ExperimentFramework experiments.

## Features

- **Event Model**: Discriminated union-style events for experiment lifecycle
- **Event Sinks**:
  - `InMemoryExperimentEventSink` - For testing and assertions
  - `LoggerExperimentEventSink` - Structured logging with ILogger
  - `OpenTelemetryExperimentEventSink` - Activities and metrics
- **Composite Sinks**: Combine multiple sinks with deterministic ordering
- **Minimal Allocations**: `in` parameter passing for high performance

## Quick Start

```bash
dotnet add package ExperimentFramework.Diagnostics
```

```csharp
using ExperimentFramework.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Add event sinks
services.AddLoggerExperimentEventSink();
services.AddOpenTelemetryExperimentEventSink();
services.AddInMemoryExperimentEventSink(maxCapacity: 1000);

// Build and use
var provider = services.BuildServiceProvider();
var sinks = provider.GetExperimentEventSinks();
```

## Event Types

- `TrialStarted` / `TrialEnded` - Trial lifecycle
- `RouteSelected` - Trial key selection
- `FallbackOccurred` - Error fallback
- `ExceptionThrown` - Exception capture
- `MethodInvoked` / `MethodCompleted` - Method-level tracking (optional)

## Testing Example

```csharp
[Fact]
public async Task Experiment_RecordsFallback()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddInMemoryExperimentEventSink();
    services.AddExperimentFramework()
        .AddExperiment<IService>()
        .WithFallback("control", "experimental")
        .OnError(OnErrorPolicy.RedirectAndReplayDefault);

    var provider = services.BuildServiceProvider();
    var sink = provider.GetRequiredService<InMemoryExperimentEventSink>();
    var service = provider.GetRequiredService<IService>();

    // Act
    await service.DoWorkAsync();

    // Assert
    var fallbacks = sink.GetEventsByKind(ExperimentEventKind.FallbackOccurred);
    Assert.Single(fallbacks);
}
```

## Documentation

See [Diagnostics & Tracing Guide](../../docs/user-guide/diagnostics.md) for detailed documentation.

## License

MIT
