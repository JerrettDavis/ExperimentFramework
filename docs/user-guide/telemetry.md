# Telemetry

ExperimentFramework provides comprehensive telemetry capabilities to track experiment execution, measure performance, and integrate with observability platforms.

## Overview

The framework supports three levels of telemetry:

1. **Built-in Logging** - Benchmark and error logging decorators
2. **OpenTelemetry Integration** - Distributed tracing with Activity API
3. **Custom Telemetry** - Implement your own telemetry providers

## Built-in Logging Decorators

The framework includes decorators for common telemetry scenarios that integrate with `ILogger`.

### Benchmark Decorator

The benchmark decorator measures execution time for each experiment invocation.

#### Configuration

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .AddLogger(l => l.AddBenchmarks())
    .Trial<IDatabase>(t => t
        .UsingFeatureFlag("UseCloudDb")
        .AddControl<LocalDatabase>("false")
        .AddVariant<CloudDatabase>("true"));

services.AddExperimentFramework(experiments);
```

#### Logged Output

```
info: ExperimentFramework.Benchmarks[0]
      Experiment call: IDatabase.QueryAsync trial=true elapsedMs=42.3
```

The log includes:

- Interface name: `IDatabase`
- Method name: `QueryAsync`
- Trial key: `true`
- Elapsed time in milliseconds: `42.3`

#### Use Cases

- Performance comparison between trials
- Identifying slow implementations
- Tracking performance regressions
- SLA monitoring

### Error Logging Decorator

The error logging decorator captures exceptions before they propagate or trigger fallback.

#### Configuration

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .AddLogger(l => l.AddErrorLogging())
    .Trial<IPaymentProcessor>(t => t
        .UsingFeatureFlag("UseNewPaymentProvider")
        .AddControl<StripePayment>("false")
        .AddVariant<NewPaymentProvider>("true")
        .OnErrorRedirectAndReplayDefault());

services.AddExperimentFramework(experiments);
```

#### Logged Output

```
error: ExperimentFramework.ErrorLogging[0]
      Experiment error: IPaymentProcessor.ChargeAsync trial=true
      System.InvalidOperationException: Connection timeout
         at NewPaymentProvider.ChargeAsync(Decimal amount)
         at ExperimentFramework.Decorators.ErrorLoggingDecorator.InvokeAsync(...)
```

The log includes:

- Interface and method name
- Trial key that failed
- Full exception with stack trace

#### Use Cases

- Monitoring condition failure rates
- Debugging experimental implementations
- Alerting on error thresholds
- Understanding fallback behavior

### Combining Decorators

You can enable both decorators simultaneously:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .AddLogger(l => l
        .AddBenchmarks()
        .AddErrorLogging())
    .Trial<IDatabase>(t => t
        .UsingFeatureFlag("UseCloudDb")
        .AddControl<LocalDatabase>("false")
        .AddVariant<CloudDatabase>("true")
        .OnErrorRedirectAndReplayDefault());
```

Decorators execute in registration order, so benchmarks will include the time spent in error logging.

## OpenTelemetry Integration

The framework integrates with OpenTelemetry to provide distributed tracing for experiments.

### Prerequisites

OpenTelemetry support uses the `System.Diagnostics.Activity` API, which is part of .NET. No additional packages are required for basic functionality.

To export traces to an observability platform, install the OpenTelemetry SDK:

```bash
dotnet add package OpenTelemetry.Exporter.Console
dotnet add package OpenTelemetry.Exporter.Jaeger
dotnet add package OpenTelemetry.Exporter.Zipkin
```

### Configuration

Enable OpenTelemetry tracking:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IDatabase>(t => t
        .UsingFeatureFlag("UseCloudDb")
        .AddControl<LocalDatabase>("false")
        .AddVariant<CloudDatabase>("true"));

services.AddExperimentFramework(experiments);
services.AddOpenTelemetryExperimentTracking();
```

Configure the OpenTelemetry SDK to listen to the `ExperimentFramework` activity source:

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService("MyApplication"))
        .AddSource("ExperimentFramework")  // Listen to experiment activities
        .AddConsoleExporter()
        .AddJaegerExporter());
```

### Activity Tags

Each experiment invocation creates an activity with these tags:

| Tag | Description | Example |
|-----|-------------|---------|
| `experiment.service` | Interface type name | `IDatabase` |
| `experiment.method` | Method being invoked | `QueryAsync` |
| `experiment.selector` | Selector name (feature flag/config key) | `UseCloudDb` |
| `experiment.trial.selected` | Initially selected condition key | `true` |
| `experiment.trial.candidates` | All available condition keys | `false,true` |
| `experiment.outcome` | Success or failure | `success` |
| `experiment.fallback` | Fallback condition key (if applicable) | `false` |
| `experiment.variant` | Variant name (for variant mode) | `variant-a` |
| `experiment.variant.source` | How variant was determined | `variantManager` |

### Activity Names

Activities are named using this pattern:

```
Experiment IDatabase.QueryAsync
```

This makes it easy to filter and group experiment traces in observability platforms.

### Example Trace

```
Span: Experiment IDatabase.QueryAsync
  experiment.service: IDatabase
  experiment.method: QueryAsync
  experiment.selector: UseCloudDb
  experiment.trial.selected: true
  experiment.trial.candidates: false,true
  experiment.outcome: success
  Duration: 42.3ms
```

### Viewing in Jaeger

When exported to Jaeger, traces appear hierarchically:

```
HTTP Request
└─ DataService.GetCustomers
   └─ Experiment IDatabase.QueryAsync
      └─ CloudDatabase.QueryAsync
         └─ SQL Query
```

### Zero Overhead When Disabled

If no `ActivityListener` is attached to the `ExperimentFramework` source, activity creation is a no-op with negligible overhead (typically < 50ns).

## Custom Telemetry

Implement the `IExperimentTelemetry` interface to create custom telemetry providers.

### IExperimentTelemetry Interface

```csharp
public interface IExperimentTelemetry
{
    IExperimentTelemetryScope StartInvocation(
        Type serviceType,
        string methodName,
        string selectorName,
        string trialKey,
        IReadOnlyList<string> candidateKeys);
}

public interface IExperimentTelemetryScope : IDisposable
{
    void RecordSuccess();
    void RecordFailure(Exception exception);
    void RecordFallback(string fallbackKey);
    void RecordVariant(string variantName, string variantSource);
}
```

### Example: Metrics Telemetry

Create a custom telemetry provider that records metrics:

```csharp
using System.Diagnostics.Metrics;

public class MetricsExperimentTelemetry : IExperimentTelemetry
{
    private static readonly Meter Meter = new("ExperimentFramework", "1.0.0");
    private static readonly Counter<long> InvocationCounter = Meter.CreateCounter<long>("experiment.invocations");
    private static readonly Histogram<double> DurationHistogram = Meter.CreateHistogram<double>("experiment.duration");

    public IExperimentTelemetryScope StartInvocation(
        Type serviceType,
        string methodName,
        string selectorName,
        string trialKey,
        IReadOnlyList<string> candidateKeys)
    {
        return new MetricsScope(serviceType, methodName, trialKey);
    }

    private class MetricsScope : IExperimentTelemetryScope
    {
        private readonly Type _serviceType;
        private readonly string _methodName;
        private readonly string _trialKey;
        private readonly long _startTimestamp;
        private string _outcome = "success";

        public MetricsScope(Type serviceType, string methodName, string trialKey)
        {
            _serviceType = serviceType;
            _methodName = methodName;
            _trialKey = trialKey;
            _startTimestamp = Stopwatch.GetTimestamp();
        }

        public void RecordSuccess()
        {
            _outcome = "success";
        }

        public void RecordFailure(Exception exception)
        {
            _outcome = "failure";
        }

        public void RecordFallback(string fallbackKey)
        {
            InvocationCounter.Add(1, new KeyValuePair<string, object?>("event", "fallback"));
        }

        public void RecordVariant(string variantName, string variantSource)
        {
            // Can record variant metrics if needed
        }

        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp).TotalMilliseconds;

            var tags = new TagList
            {
                { "service", _serviceType.Name },
                { "method", _methodName },
                { "trial", _trialKey },
                { "outcome", _outcome }
            };

            InvocationCounter.Add(1, tags);
            DurationHistogram.Record(elapsed, tags);
        }
    }
}
```

Register the custom telemetry provider:

```csharp
services.AddSingleton<IExperimentTelemetry, MetricsExperimentTelemetry>();
```

### Example: Application Insights

Integrate with Application Insights:

```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

public class ApplicationInsightsTelemetry : IExperimentTelemetry
{
    private readonly TelemetryClient _telemetryClient;

    public ApplicationInsightsTelemetry(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }

    public IExperimentTelemetryScope StartInvocation(
        Type serviceType,
        string methodName,
        string selectorName,
        string trialKey,
        IReadOnlyList<string> candidateKeys)
    {
        return new AppInsightsScope(_telemetryClient, serviceType, methodName, trialKey);
    }

    private class AppInsightsScope : IExperimentTelemetryScope
    {
        private readonly TelemetryClient _client;
        private readonly DependencyTelemetry _telemetry;

        public AppInsightsScope(TelemetryClient client, Type serviceType, string methodName, string trialKey)
        {
            _client = client;
            _telemetry = new DependencyTelemetry
            {
                Type = "Experiment",
                Name = $"{serviceType.Name}.{methodName}",
                Data = trialKey
            };
            _telemetry.Properties["service"] = serviceType.Name;
            _telemetry.Properties["method"] = methodName;
            _telemetry.Properties["trial"] = trialKey;
        }

        public void RecordSuccess()
        {
            _telemetry.Success = true;
        }

        public void RecordFailure(Exception exception)
        {
            _telemetry.Success = false;
            _client.TrackException(exception);
        }

        public void RecordFallback(string fallbackKey)
        {
            _telemetry.Properties["fallback"] = fallbackKey;
        }

        public void RecordVariant(string variantName, string variantSource)
        {
            _telemetry.Properties["variant"] = variantName;
        }

        public void Dispose()
        {
            _client.TrackDependency(_telemetry);
        }
    }
}
```

## Telemetry Best Practices

### 1. Use Appropriate Cardinality

Be mindful of tag cardinality in metrics:

```csharp
// Good: Low cardinality
tags.Add("trial", trialKey);  // Limited number of values

// Bad: High cardinality
tags.Add("user_id", userId);  // Millions of unique values
```

### 2. Sample High-Volume Experiments

For high-traffic experiments, consider sampling:

```csharp
public class SamplingTelemetry : IExperimentTelemetry
{
    private readonly IExperimentTelemetry _inner;
    private readonly double _sampleRate;
    private readonly Random _random = new();

    public SamplingTelemetry(IExperimentTelemetry inner, double sampleRate)
    {
        _inner = inner;
        _sampleRate = sampleRate;
    }

    public IExperimentTelemetryScope StartInvocation(...)
    {
        if (_random.NextDouble() < _sampleRate)
        {
            return _inner.StartInvocation(...);
        }
        return NoopScope.Instance;
    }
}
```

### 3. Combine Telemetry Approaches

Use multiple telemetry mechanisms together:

```csharp
// Logs for debugging
.AddLogger(l => l.AddBenchmarks().AddErrorLogging())

// Traces for distributed tracing
services.AddOpenTelemetryExperimentTracking();

// Custom metrics for dashboards
services.AddSingleton<IExperimentTelemetry, MetricsExperimentTelemetry>();
```

### 4. Monitor Fallback Rates

Track how often fallback occurs:

```sql
-- Query for fallback rate in your metrics system
SELECT
    service,
    COUNT(CASE WHEN event = 'fallback' THEN 1 END) / COUNT(*) AS fallback_rate
FROM experiment_invocations
GROUP BY service
```

Alert when fallback rate exceeds threshold:

```
IF fallback_rate > 0.05 THEN ALERT "Experiment fallback rate too high"
```

### 5. Correlate with Business Metrics

Link experiment telemetry to business outcomes:

```csharp
public async Task ProcessOrderAsync(Order order)
{
    // Experiment executes and logs telemetry
    var payment = await _paymentProcessor.ChargeAsync(order.Total);

    // Log business outcome
    _metrics.Record("order.value", order.Total, new TagList
    {
        { "payment_trial", payment.ProviderName }
    });
}
```

This allows correlation:
- Did the new payment provider increase successful transactions?
- What was the revenue impact of the experimental recommendation engine?

## Debugging with Telemetry

### Finding Slow Trials

Use benchmark logs to identify performance issues:

```bash
# Filter logs for slow invocations
cat application.log | grep "Experiment call" | grep "elapsedMs" | awk '$NF > 1000'
```

### Tracking Failure Patterns

Analyze error logs to find common failure causes:

```bash
# Count failure types
cat application.log | grep "Experiment error" | awk '{print $NF}' | sort | uniq -c
```

### Visualizing Experiment Impact

Use OpenTelemetry traces to understand request flow:

1. Filter traces by experiment service
2. Compare durations between conditions
3. Identify downstream impacts
4. Spot error propagation patterns

## Next Steps

- [Naming Conventions](naming-conventions.md) - Customize selector names for telemetry
- [Advanced Topics](advanced.md) - Build custom decorators and telemetry providers
- [Samples](samples.md) - See complete telemetry implementations
