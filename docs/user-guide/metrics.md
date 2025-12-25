# Metrics

Track experiment performance with Prometheus or OpenTelemetry metrics to monitor experiment health, detect issues, and make data-driven decisions about rollouts.

## Overview

The metrics system collects:

- **Invocation counts**: How many times each condition is executed
- **Duration**: How long each condition takes to execute
- **Outcomes**: Success vs failure rates
- **Fallbacks**: How often fallback logic triggers

## Installation

```bash
dotnet add package ExperimentFramework.Metrics.Exporters
```

This package includes both Prometheus and OpenTelemetry exporters.

## Prometheus Exporter

### Basic Setup

```csharp
using ExperimentFramework.Metrics.Exporters;

var prometheusMetrics = new PrometheusExperimentMetrics();

var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IDatabase>(t => t
        .UsingFeatureFlag("UseCloudDb")
        .AddControl<LocalDb>("false")
        .AddCondition<CloudDb>("true")
        .OnErrorRedirectAndReplayControl())
    .WithMetrics(prometheusMetrics)
    .UseDispatchProxy();

builder.Services.AddExperimentFramework(experiments);

var app = builder.Build();

// Expose metrics endpoint
app.MapGet("/metrics", () => prometheusMetrics.GeneratePrometheusOutput());

app.Run();
```

### Metrics Output

```
# TYPE experiment_invocations_total counter
experiment_invocations_total{service="IDatabase",trial_key="cloud",method="GetDataAsync"} 1523

# TYPE experiment_success_total counter
experiment_success_total{service="IDatabase",trial_key="cloud",method="GetDataAsync"} 1478

# TYPE experiment_errors_total counter
experiment_errors_total{service="IDatabase",trial_key="cloud",method="GetDataAsync"} 45

# TYPE experiment_duration_seconds histogram
experiment_duration_seconds_sum{service="IDatabase",trial_key="cloud",method="GetDataAsync"} 45.234
experiment_duration_seconds_count{service="IDatabase",trial_key="cloud",method="GetDataAsync"} 1523
```

### Collected Metrics

| Metric | Type | Description | Tags |
|--------|------|-------------|------|
| `experiment_invocations_total` | Counter | Total number of invocations | `service`, `trial_key`, `method` |
| `experiment_success_total` | Counter | Total number of successful invocations | `service`, `trial_key`, `method` |
| `experiment_errors_total` | Counter | Total number of failed invocations | `service`, `trial_key`, `method` |
| `experiment_duration_seconds` | Histogram | Duration of invocations | `service`, `trial_key`, `method` |

**Tags:**
- `service`: Service interface name (e.g., "IDatabase")
- `trial_key`: Condition key (e.g., "cloud", "local")
- `method`: Method name (e.g., "GetDataAsync")

## OpenTelemetry Exporter

### Basic Setup

```csharp
using ExperimentFramework.Metrics.Exporters;
using OpenTelemetry.Metrics;

var otelMetrics = new OpenTelemetryExperimentMetrics("ExperimentFramework", "1.0.0");

var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IDatabase>(t => t
        .UsingFeatureFlag("UseCloudDb")
        .AddControl<LocalDb>("false")
        .AddCondition<CloudDb>("true")
        .OnErrorRedirectAndReplayControl())
    .WithMetrics(otelMetrics)
    .UseDispatchProxy();

builder.Services.AddExperimentFramework(experiments);

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter("ExperimentFramework") // Match meter name
        .AddPrometheusExporter());

var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint(); // /metrics

app.Run();
```

### Full OpenTelemetry Integration

```csharp
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;

var otelMetrics = new OpenTelemetryExperimentMetrics("ExperimentFramework", "1.0.0");

var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IDatabase>(t => t
        .UsingFeatureFlag("UseCloudDb")
        .AddControl<LocalDb>("false")
        .AddCondition<CloudDb>("true")
        .OnErrorRedirectAndReplayControl())
    .WithMetrics(otelMetrics)
    .UseDispatchProxy();

builder.Services.AddExperimentFramework(experiments);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("MyApiService")
        .AddAttributes(new Dictionary<string, object>
        {
            ["environment"] = builder.Environment.EnvironmentName,
            ["version"] = "1.0.0"
        }))
    .WithMetrics(metrics => metrics
        .AddMeter("ExperimentFramework")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://otel-collector:4317");
        }));
```

## Grafana Dashboards

### Success Rate

```promql
# Overall success rate per condition
sum(rate(experiment_success_total[5m])) by (service, trial_key)
/
sum(rate(experiment_invocations_total[5m])) by (service, trial_key)
```

### Average Latency

```promql
# Average latency per condition
sum(rate(experiment_duration_seconds_sum[5m])) by (service, trial_key)
/
sum(rate(experiment_duration_seconds_count[5m])) by (service, trial_key)
```

### P95 Latency

```promql
# P95 latency (requires histogram buckets)
histogram_quantile(0.95,
  sum(rate(experiment_duration_seconds_bucket[5m])) by (service, trial_key, le)
)
```

### Throughput

```promql
# Requests per second per condition
sum(rate(experiment_invocations_total[5m])) by (service, trial_key)
```

### Error Rate

```promql
# Error rate per condition
sum(rate(experiment_errors_total[5m])) by (service, trial_key)
/
sum(rate(experiment_invocations_total[5m])) by (service, trial_key)
```

### Comparison Dashboard

```promql
# Side-by-side latency comparison
sum(rate(experiment_duration_seconds_sum{trial_key="cloud"}[5m]))
/
sum(rate(experiment_duration_seconds_count{trial_key="cloud"}[5m]))

vs

sum(rate(experiment_duration_seconds_sum{trial_key="local"}[5m]))
/
sum(rate(experiment_duration_seconds_count{trial_key="local"}[5m]))
```

## Real-World Example

Complete setup with alerting:

```csharp
using ExperimentFramework.Metrics.Exporters;

// Create metrics exporter
var prometheusMetrics = new PrometheusExperimentMetrics();

// Configure experiments
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IDatabase>(t => t
        .UsingFeatureFlag("UseCloudDb")
        .AddControl<LocalDb>("false")
        .AddCondition<CloudDb>("true")
        .OnErrorRedirectAndReplayControl())
    .Trial<IPaymentGateway>(t => t
        .UsingFeatureFlag("UseNewPaymentGateway")
        .AddControl<StableGateway>("false")
        .AddCondition<NewGateway>("true")
        .OnErrorRedirectAndReplayControl())
    .WithMetrics(prometheusMetrics)
    .WithTimeout(TimeSpan.FromSeconds(5), TimeoutAction.FallbackToDefault)
    .WithCircuitBreaker(options =>
    {
        options.FailureRatioThreshold = 0.5;
        options.MinimumThroughput = 10;
        options.OnCircuitOpen = CircuitBreakerAction.FallbackToDefault;
    })
    .UseDispatchProxy();

builder.Services.AddExperimentFramework(experiments);

var app = builder.Build();

// Metrics endpoint
app.MapGet("/metrics", () =>
{
    var output = prometheusMetrics.GeneratePrometheusOutput();
    return Results.Text(output, "text/plain; version=0.0.4");
});

// Health check based on metrics
app.MapGet("/health", () =>
{
    // Could check if cloud condition error rate is acceptable
    return Results.Ok(new { status = "healthy" });
});

app.Run();
```

## Prometheus Alerts

### High Error Rate

```yaml
# prometheus.rules.yml
groups:
  - name: experiment_framework
    rules:
      - alert: ExperimentHighErrorRate
        expr: |
          sum(rate(experiment_errors_total[5m])) by (service, trial_key)
          /
          sum(rate(experiment_invocations_total[5m])) by (service, trial_key)
          > 0.05
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Experiment {{ $labels.service }} trial {{ $labels.trial_key }} has high error rate"
          description: "Error rate is {{ $value | humanizePercentage }}"
```

### High Latency

```yaml
- alert: ExperimentHighLatency
  expr: |
    sum(rate(experiment_duration_seconds_sum[5m])) by (service, trial_key)
    /
    sum(rate(experiment_duration_seconds_count[5m])) by (service, trial_key)
    > 5
  for: 5m
  labels:
    severity: warning
  annotations:
    summary: "Experiment {{ $labels.service }} trial {{ $labels.trial_key }} has high latency"
    description: "Average latency is {{ $value }}s"
```

### Low Traffic

```yaml
- alert: ExperimentLowTraffic
  expr: |
    sum(rate(experiment_invocations_total[10m])) by (service) < 1
  for: 10m
  labels:
    severity: info
  annotations:
    summary: "Experiment {{ $labels.service }} has low traffic"
    description: "May not have enough data for statistical significance"
```

## Best Practices

### 1. Add Metrics Early

Add metrics from the start of rollout:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IService>(t => t
        .UsingFeatureFlag("UseNewService")
        .AddControl<DefaultService>("false")
        .AddCondition<NewService>("true")
        .OnErrorRedirectAndReplayControl())
    .WithMetrics(prometheusMetrics)  // Add immediately
    .UseDispatchProxy();
```

### 2. Monitor Before Increasing Traffic

Watch metrics at low traffic levels (5-10%) before increasing:

```json
// Start at 5%
{
  "FeatureManagement": {
    "UseNewService": {
      "EnabledFor": [
        {
          "Name": "Microsoft.Percentage",
          "Parameters": { "Value": 5 }
        }
      ]
    }
  }
}
```

Monitor for 24-48 hours. If metrics look good, increase to 25%, then 50%, then 100%.

### 3. Use Appropriate Aggregation Windows

```promql
# Too short - noisy
sum(rate(experiment_invocations_total[30s])) by (service, trial_key)

# Good - balanced
sum(rate(experiment_invocations_total[5m])) by (service, trial_key)

# Long-term trends
sum(rate(experiment_invocations_total[1h])) by (service, trial_key)
```

### 4. Set Up Alerts

Create alerts for:
- Error rate > 5%
- Latency > 2x baseline
- Circuit breaker opening
- No traffic (experiment not running)

### 5. Track Business Metrics

Combine experiment metrics with business metrics:

```promql
# Conversion rate by condition
sum(rate(business_conversions_total[5m])) by (service, trial_key)
/
sum(rate(business_pageviews_total[5m])) by (service, trial_key)
```

## Troubleshooting

### No Metrics Appearing

**Symptom**: `/metrics` endpoint returns empty or missing experiment metrics.

**Solutions:**
1. Verify `WithMetrics()` is called before `UseDispatchProxy()`
2. Ensure experiments are actually being invoked
3. Check that same metrics instance is used in endpoint
4. Verify experiment services are being called (not bypassed)

### Metrics Delayed

**Symptom**: Metrics lag behind actual usage.

**Solutions:**
1. Check Prometheus scrape interval (default 15s)
2. Verify metrics endpoint is accessible
3. Use shorter aggregation windows for real-time data

### Incorrect Counts

**Symptom**: Invocation counts don't match expected traffic.

**Solutions:**
1. Remember metrics are per-instance (not aggregated across pods)
2. Use Prometheus `sum()` to aggregate across instances
3. Check if proxy is actually being used (not direct injection)

## See Also

- [Telemetry](telemetry.md) - OpenTelemetry distributed tracing
- [Timeout Enforcement](timeout-enforcement.md) - Track timeout rates
- [Circuit Breaker](circuit-breaker.md) - Monitor circuit state
