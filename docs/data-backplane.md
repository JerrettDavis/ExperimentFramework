# Data Backplane

The ExperimentFramework Data Backplane provides a pluggable abstraction for capturing and emitting experimentation-related telemetry, state, and analysis signals. This allows you to integrate experiments with your existing observability and analytics pipelines.

## Features

- **Standardized Event Schemas**: Consistent, versioned events for exposures, assignments, outcomes, and analysis signals
- **Pluggable Implementations**: In-memory, logging, or custom backplane integrations
- **Exposure Logging**: Automatic capture of variant exposures with configurable semantics
- **Assignment Consistency**: Track assignment policies and detect consistency violations
- **Sampling**: Configurable sampling rates to reduce data volume
- **Health Checks**: Monitor backplane health and availability
- **Non-Blocking**: Failures are bounded and configurable to prevent blocking the request path

## Quick Start

### 1. Install Packages

```bash
dotnet add package ExperimentFramework.DataPlane
dotnet add package ExperimentFramework.DataPlane.Abstractions
```

### 2. Configure Services

```csharp
// Register a data backplane implementation
builder.Services.AddInMemoryDataBackplane(); // or AddLoggingDataBackplane()

// Configure data plane options
builder.Services.AddDataBackplane(options =>
{
    options.EnableExposureEvents = true;
    options.SamplingRate = 1.0; // Capture 100% of events
    options.ExposureSemantics = ExposureSemantics.OnDecision;
});

// Optionally register a subject identity provider
builder.Services.AddSingleton<ISubjectIdentityProvider, MyIdentityProvider>();
```

### 3. Enable Exposure Logging

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .WithExposureLogging()  // Add exposure logging decorator
    .Trial<IMyService>(t => t
        .UsingFeatureFlag("MyFeature")
        .AddControl<DefaultImpl>()
        .AddCondition<ExperimentalImpl>("true"))
    .UseDispatchProxy();

builder.Services.AddExperimentFramework(experiments);
```

## Event Schemas

### Exposure Event

Captures when a subject was exposed to a variant:

```csharp
public sealed class ExposureEvent
{
    public string ExperimentName { get; init; }      // e.g., "IPaymentProcessor"
    public string VariantKey { get; init; }          // e.g., "control" or "paypal"
    public string SubjectId { get; init; }           // e.g., user/session ID
    public string? SubjectType { get; init; }        // e.g., "user", "session"
    public string? TenantId { get; init; }           // For multi-tenant scenarios
    public DateTimeOffset Timestamp { get; init; }   
    public string SelectionReason { get; init; }     // e.g., "trial-selection"
    public AssignmentPolicy AssignmentPolicy { get; init; }
    public bool IsRepeatExposure { get; init; }
}
```

### Assignment Event

Captures when a subject's variant assignment changed:

```csharp
public sealed class AssignmentEvent
{
    public string ExperimentName { get; init; }
    public string SubjectId { get; init; }
    public string PreviousVariantKey { get; init; }
    public string NewVariantKey { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string ChangeReason { get; init; }
    public AssignmentPolicy AssignmentPolicy { get; init; }
}
```

### Analysis Signal Event

Captures statistical or science signals:

```csharp
public sealed class AnalysisSignalEvent
{
    public string ExperimentName { get; init; }
    public AnalysisSignalType SignalType { get; init; } // SRM, Peeking, etc.
    public SignalSeverity Severity { get; init; }       // Info, Warning, Error
    public DateTimeOffset Timestamp { get; init; }
    public string Message { get; init; }
    public IReadOnlyDictionary<string, object>? Data { get; init; }
}
```

## Backplane Implementations

### In-Memory Backplane

Stores events in memory for testing and development:

```csharp
builder.Services.AddInMemoryDataBackplane();

// Later, access events
var backplane = serviceProvider.GetRequiredService<IDataBackplane>() as InMemoryDataBackplane;
Console.WriteLine($"Captured {backplane.Events.Count} events");
```

### Logging Backplane

Emits events as structured logs:

```csharp
builder.Services.AddLoggingDataBackplane();
```

Events are logged at `Information` level with JSON serialization.

### OpenTelemetry Backplane

Emits events as OpenTelemetry Activities (spans) and structured logs:

```csharp
builder.Services.AddOpenTelemetryDataBackplane();
```

**Features:**
- Emits data plane events as Activities with semantic tags
- Uses activity source: `"ExperimentFramework.DataPlane"`
- Compatible with OpenTelemetry SDK exporters
- No external OpenTelemetry package required (uses BCL types)
- Automatic tagging for exposures, assignments, and analysis signals

**Activity Tags:**
- `dataplane.event.id` - Event identifier
- `dataplane.event.type` - Event type (Exposure, Assignment, etc.)
- `experiment.name` - Experiment name
- `experiment.variant` - Variant key
- `experiment.subject.id` - Subject identifier
- `experiment.selection.reason` - Why variant was selected
- `analysis.signal.type` - Signal type (for analysis signals)
- And many more semantic tags...

**Example OpenTelemetry SDK configuration:**

```csharp
using OpenTelemetry.Trace;

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("ExperimentFramework.DataPlane")
        .AddConsoleExporter()  // or other exporters
    );
```

### Composite Backplane

Route events to multiple destinations:

```csharp
builder.Services.AddCompositeDataBackplane(
    sp => ActivatorUtilities.CreateInstance<LoggingDataBackplane>(sp),
    sp => ActivatorUtilities.CreateInstance<OpenTelemetryDataBackplane>(sp)
    // Add more backplane factories...
);
```

### Custom Implementation

Implement `IDataBackplane` for custom integrations:

```csharp
public class MyCustomBackplane : IDataBackplane
{
    public async ValueTask PublishAsync(DataPlaneEnvelope envelope, CancellationToken ct)
    {
        // Send to your analytics system
        await _myAnalytics.SendEventAsync(envelope, ct);
    }

    public ValueTask FlushAsync(CancellationToken ct) 
    { 
        return ValueTask.CompletedTask; 
    }

    public ValueTask<BackplaneHealth> HealthAsync(CancellationToken ct)
    {
        return ValueTask.FromResult(BackplaneHealth.Healthy());
    }
}
```

## Configuration Options

```csharp
builder.Services.AddDataBackplane(options =>
{
    // Event types
    options.EnableExposureEvents = true;
    options.EnableAssignmentEvents = true;
    options.EnableOutcomeEvents = true;
    options.EnableAnalysisSignals = true;
    options.EnableErrorEvents = true;

    // Exposure semantics
    options.ExposureSemantics = ExposureSemantics.OnDecision; // or OnFirstUse, Explicit

    // Batching and buffering
    options.BatchSize = 100;
    options.FlushInterval = TimeSpan.FromSeconds(10);
    options.MaxQueueSize = 10000;

    // Failure handling
    options.FailureMode = BackplaneFailureMode.Drop; // or Block

    // Sampling
    options.SamplingRate = 1.0; // 0.0 to 1.0

    // PII redaction
    options.PiiRedaction.RedactSubjectIds = false;
    options.PiiRedaction.RedactTenantIds = false;
    options.PiiRedaction.RedactFields.Add("email");
});
```

## Subject Identity Provider

Provide subject identity for exposure logging:

```csharp
public class MyIdentityProvider : ISubjectIdentityProvider
{
    private readonly IHttpContextAccessor _accessor;

    public string SubjectType => "user";

    public bool TryGetSubjectId(out string subjectId)
    {
        var userId = _accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        subjectId = userId ?? "";
        return !string.IsNullOrEmpty(subjectId);
    }
}

builder.Services.AddScoped<ISubjectIdentityProvider, MyIdentityProvider>();
```

## Assignment Policies

Define how subjects are assigned to variants:

- `BestEffort`: No consistency guarantees
- `SessionSticky`: Sticky within a session
- `SubjectSticky`: Sticky for a subject across sessions
- `GloballySticky`: Globally sticky (requires shared state)

## Example: Full Integration

```csharp
// 1. Register backplane and options
builder.Services.AddDataBackplane(options =>
{
    options.EnableExposureEvents = true;
    options.SamplingRate = 1.0;
});

// Choose your backplane implementation
builder.Services.AddOpenTelemetryDataBackplane();  // or AddLoggingDataBackplane() or AddInMemoryDataBackplane()

// Optional: Register subject identity provider
builder.Services.AddSingleton<ISubjectIdentityProvider, MyIdentityProvider>();

// 2. Configure experiments with exposure logging
var experiments = ExperimentFrameworkBuilder.Create()
    .WithExposureLogging()
    .Trial<IMyService>(t => t
        .UsingFeatureFlag("MyFeature")
        .AddControl<DefaultImpl>()
        .AddCondition<ExperimentalImpl>("true"))
    .UseDispatchProxy();

builder.Services.AddExperimentFramework(experiments);

// 3. Optional: Configure OpenTelemetry SDK to collect activities
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("ExperimentFramework.DataPlane")
        .AddConsoleExporter());  // or other exporters

// 4. Use normally - exposures are logged automatically
var service = app.Services.GetRequiredService<IMyService>();
await service.DoWorkAsync();

// 5. Check backplane health
var backplane = app.Services.GetRequiredService<IDataBackplane>();
var health = await backplane.HealthAsync();
Console.WriteLine($"Backplane healthy: {health.IsHealthy}");
```

## Running the Sample

```bash
cd samples/ExperimentFramework.DataPlaneSample
dotnet run
```

The sample demonstrates:
- Configuring an in-memory data backplane
- Enabling exposure logging
- Running an experiment
- Inspecting captured exposure events
- Checking backplane health

## Architecture

```
User Code
    ↓
IMyService (Proxy)
    ↓
ExposureLoggingDecorator
    ↓ (publishes exposure event)
IDataBackplane
    ↓
[Implementation: InMemory, Logging, Custom]
    ↓
Your Analytics/Observability System
```

## Best Practices

1. **Use sampling in production**: Reduce data volume with `SamplingRate < 1.0`
2. **Choose appropriate failure mode**: Use `Drop` for latency-sensitive paths
3. **Implement health checks**: Monitor backplane availability
4. **Use composite backplane**: Route to multiple destinations for redundancy
5. **Redact PII**: Configure PII redaction for sensitive data
6. **Buffer and batch**: Use appropriate `BatchSize` and `FlushInterval` for efficiency

## Future Enhancements

The following features are planned:

- Sample Ratio Mismatch (SRM) detection
- Sequential testing metadata support
- Advanced assignment tracking with state
- Integration with OpenTelemetry events
- Kafka/EventHub backplane implementations
- Parquet export for data lakes
