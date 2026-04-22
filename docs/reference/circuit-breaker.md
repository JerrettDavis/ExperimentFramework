# Circuit Breaker

Circuit breaker automatically disables failing conditions after reaching a failure threshold, preventing cascading failures and giving failing services time to recover.

## Overview

The circuit breaker pattern protects your system by:

1. **Monitoring failures**: Tracks failure rate over a sliding time window
2. **Opening the circuit**: Stops calling failing condition when threshold exceeded
3. **Half-open state**: Periodically tests if service recovered
4. **Closing the circuit**: Resumes normal operation when service healthy

ExperimentFramework integrates with [Polly](https://www.pollydocs.org/) for circuit breaker implementation.

## Installation

```bash
dotnet add package ExperimentFramework.Resilience
```

This package includes Polly and circuit breaker integration.

## Basic Configuration

```csharp
using ExperimentFramework.Resilience;

var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IPaymentGateway>(t => t
        .UsingFeatureFlag("UseNewPaymentGateway")
        .AddControl<StableGateway>("false")
        .AddCondition<NewGateway>("true")
        .OnErrorRedirectAndReplayControl())
    .WithCircuitBreaker(options =>
    {
        options.FailureRatioThreshold = 0.5;      // Open after 50% failure rate
        options.MinimumThroughput = 10;            // Need 10 calls to assess
        options.SamplingDuration = TimeSpan.FromSeconds(30);
        options.BreakDuration = TimeSpan.FromSeconds(60);
        options.OnCircuitOpen = CircuitBreakerAction.FallbackToDefault;
    })
    .UseDispatchProxy();

builder.Services.AddExperimentFramework(experiments);
```

## Configuration Options

### FailureRatioThreshold

Percentage of failures (0.0-1.0) that triggers circuit opening.

```csharp
options.FailureRatioThreshold = 0.3; // Open at 30% failure rate
```

**Guidelines:**
- **0.1-0.2**: Conservative, production-critical services
- **0.3-0.5**: Balanced, most scenarios
- **0.6-0.8**: Permissive, development/testing

### MinimumThroughput

Minimum number of requests before evaluating failure ratio.

```csharp
options.MinimumThroughput = 20; // Need 20 requests before assessment
```

**Prevents** premature circuit opening from a few failures during low traffic.

**Guidelines:**
- **5-10**: Low traffic services
- **10-20**: Medium traffic
- **50+**: High traffic services

### SamplingDuration

Time window for tracking failures.

```csharp
options.SamplingDuration = TimeSpan.FromMinutes(1);
```

**Guidelines:**
- **10-30 seconds**: Fast-changing conditions
- **1-2 minutes**: Most scenarios
- **5+ minutes**: Slowly varying loads

### BreakDuration

How long the circuit stays open before transitioning to half-open.

```csharp
options.BreakDuration = TimeSpan.FromMinutes(2);
```

**During this time**, all requests are immediately failed (or fallback to default).

**Guidelines:**
- **30-60 seconds**: Fast recovery expected
- **2-5 minutes**: Standard services
- **10+ minutes**: Services with slow startup

### OnCircuitOpen Actions

What happens when circuit opens:

```csharp
// Throw exception
options.OnCircuitOpen = CircuitBreakerAction.ThrowException;

// Fallback to control condition
options.OnCircuitOpen = CircuitBreakerAction.FallbackToDefault;

// Fallback to specific condition
options.OnCircuitOpen = CircuitBreakerAction.FallbackToSpecificTrial;
options.FallbackTrialKey = "noop";
```

## Circuit States

### Closed (Normal Operation)

All requests pass through normally. Failures are tracked.

```
Request → Trial → Response
```

### Open (Circuit Tripped)

All requests fail immediately or fallback. Service not called.

```
Request → ❌ Circuit Open → Fallback
```

### Half-Open (Testing Recovery)

After `BreakDuration`, allows test requests to check if service recovered.

```
Test Request → Trial → Success? → Close : Open
```

## Real-World Example: Payment Gateway

```csharp
public interface IPaymentGateway
{
    Task<PaymentResult> ProcessPaymentAsync(Payment payment);
}

public class StablePaymentGateway : IPaymentGateway
{
    private readonly ILogger<StablePaymentGateway> _logger;

    public StablePaymentGateway(ILogger<StablePaymentGateway> logger)
    {
        _logger = logger;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(Payment payment)
    {
        _logger.LogInformation("Processing with stable gateway");
        // Proven, reliable implementation
        return await ProcessLegacyPayment(payment);
    }
}

public class NewPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _http;
    private readonly ILogger<NewPaymentGateway> _logger;

    public NewPaymentGateway(HttpClient http, ILogger<NewPaymentGateway> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(Payment payment)
    {
        _logger.LogInformation("Processing with new gateway");
        // New implementation - may have reliability issues
        var response = await _http.PostAsJsonAsync("/process", payment);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaymentResult>();
    }
}

// Configuration
builder.Services.AddHttpClient<NewPaymentGateway>();
builder.Services.AddScoped<StablePaymentGateway>();
builder.Services.AddScoped<NewPaymentGateway>();
builder.Services.AddScoped<IPaymentGateway, StablePaymentGateway>();

var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IPaymentGateway>(t => t
        .UsingFeatureFlag("UseNewPaymentGateway")
        .AddControl<StablePaymentGateway>("false")
        .AddCondition<NewPaymentGateway>("true")
        .OnErrorRedirectAndReplayControl())
    .WithCircuitBreaker(options =>
    {
        // Conservative settings for payment processing
        options.FailureRatioThreshold = 0.2;       // Open at 20% failure
        options.MinimumThroughput = 5;              // Need only 5 calls
        options.SamplingDuration = TimeSpan.FromMinutes(1);
        options.BreakDuration = TimeSpan.FromMinutes(5);
        options.OnCircuitOpen = CircuitBreakerAction.FallbackToDefault;
    })
    .UseDispatchProxy();

builder.Services.AddExperimentFramework(experiments);
```

**Scenario:**
1. New gateway starts failing (network issue, API down, etc.)
2. After 5 requests with 20% failure rate, circuit opens
3. All payment requests route to stable gateway for 5 minutes
4. After 5 minutes, test request checks if new gateway recovered
5. If recovered, circuit closes and normal operation resumes

## Combining with Timeout

Circuit breaker works great with timeout enforcement:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IExternalService>(t => t
        .UsingFeatureFlag("UseExternalApi")
        .AddControl<CachedService>("false")
        .AddCondition<ExternalApiService>("true")
        .OnErrorRedirectAndReplayControl())
    .WithTimeout(TimeSpan.FromSeconds(3), TimeoutAction.FallbackToDefault)
    .WithCircuitBreaker(options =>
    {
        options.FailureRatioThreshold = 0.5;
        options.MinimumThroughput = 10;
        options.SamplingDuration = TimeSpan.FromSeconds(30);
        options.BreakDuration = TimeSpan.FromMinutes(1);
        options.OnCircuitOpen = CircuitBreakerAction.FallbackToDefault;
    })
    .UseDispatchProxy();
```

**Effect:**
- Individual slow requests timeout after 3 seconds
- If 50% of requests fail/timeout within 30 seconds, circuit opens
- All requests route to cached service for 1 minute
- Prevents hammering slow/failing external API

## Multi-Service Example

Different circuit breaker settings per service:

```csharp
// Critical payment service - conservative settings
var paymentExperiments = ExperimentFrameworkBuilder.Create()
    .Trial<IPaymentGateway>(t => t
        .UsingFeatureFlag("UseNewPayment")
        .AddControl<StablePayment>("false")
        .AddCondition<NewPayment>("true")
        .OnErrorRedirectAndReplayControl())
    .WithCircuitBreaker(options =>
    {
        options.FailureRatioThreshold = 0.1;  // Very conservative
        options.MinimumThroughput = 5;
        options.OnCircuitOpen = CircuitBreakerAction.FallbackToDefault;
    })
    .UseDispatchProxy();

// Non-critical recommendations - permissive settings
var recommendationExperiments = ExperimentFrameworkBuilder.Create()
    .Trial<IRecommendationEngine>(t => t
        .UsingFeatureFlag("UseMachineLearning")
        .AddControl<RuleBased>("false")
        .AddCondition<MachineLearning>("true")
        .OnErrorRedirectAndReplayControl())
    .WithCircuitBreaker(options =>
    {
        options.FailureRatioThreshold = 0.6;  // More permissive
        options.MinimumThroughput = 20;
        options.OnCircuitOpen = CircuitBreakerAction.FallbackToDefault;
    })
    .UseDispatchProxy();

builder.Services.AddExperimentFramework(paymentExperiments);
builder.Services.AddExperimentFramework(recommendationExperiments);
```

## Best Practices

### 1. Start Conservative

Begin with strict thresholds and relax based on observed behavior:

```csharp
options.FailureRatioThreshold = 0.2;  // Start at 20%, increase if too sensitive
options.MinimumThroughput = 10;        // Higher threshold for production
```

### 2. Match Break Duration to Service Recovery Time

Consider how long service needs to recover:

```csharp
// Database failover: 30-60 seconds
options.BreakDuration = TimeSpan.FromSeconds(60);

// External API with rate limiting: 5-10 minutes
options.BreakDuration = TimeSpan.FromMinutes(10);

// Microservice restart: 2-3 minutes
options.BreakDuration = TimeSpan.FromMinutes(3);
```

### 3. Monitor Circuit State

Log circuit state changes:

```csharp
.WithCircuitBreaker(options =>
{
    options.FailureRatioThreshold = 0.5;
    options.MinimumThroughput = 10;
    options.SamplingDuration = TimeSpan.FromSeconds(30);
    options.BreakDuration = TimeSpan.FromMinutes(1);
    options.OnCircuitOpen = CircuitBreakerAction.FallbackToDefault;
})
.AddLogger(l => l.AddErrorLogging()) // Logs circuit breaker events
```

### 4. Test Circuit Breaker Behavior

Write integration tests:

```csharp
[Fact]
public async Task CircuitBreaker_OpensAfterFailures()
{
    // Arrange
    var experiments = ExperimentFrameworkBuilder.Create()
        .Trial<IService>(t => t
            .UsingFeatureFlag("UseNewService")
            .AddControl<StableService>("false")
            .AddCondition<FailingService>("true")
            .OnErrorRedirectAndReplayControl())
        .WithCircuitBreaker(options =>
        {
            options.FailureRatioThreshold = 0.5;
            options.MinimumThroughput = 5;
            options.OnCircuitOpen = CircuitBreakerAction.FallbackToDefault;
        })
        .UseDispatchProxy();

    // Act - cause failures
    for (int i = 0; i < 10; i++)
    {
        try
        {
            await service.ExecuteAsync();
        }
        catch { }
        await Task.Delay(50);
    }

    // Assert - circuit opened, using stable service
    var result = await service.ExecuteAsync();
    Assert.Equal("Stable", result.Source);
}
```

### 5. Use with Kill Switch

Combine with kill switch for manual control:

```csharp
var killSwitch = new InMemoryKillSwitchProvider();

var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IService>(t => t
        .UsingFeatureFlag("UseNewService")
        .AddControl<StableService>("false")
        .AddCondition<NewService>("true")
        .OnErrorRedirectAndReplayControl())
    .WithCircuitBreaker(options =>
    {
        options.FailureRatioThreshold = 0.5;
        options.MinimumThroughput = 10;
        options.OnCircuitOpen = CircuitBreakerAction.FallbackToDefault;
    })
    .WithKillSwitch(killSwitch)
    .UseDispatchProxy();

// Emergency disable if circuit breaker isn't fast enough
killSwitch.DisableTrial(typeof(IService), "true");
```

## Monitoring

Track circuit breaker state with metrics:

```csharp
dotnet add package ExperimentFramework.Metrics.Exporters
```

```csharp
var metrics = new PrometheusExperimentMetrics();

var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IService>(t => t
        .UsingFeatureFlag("UseNewService")
        .AddControl<StableService>("false")
        .AddCondition<NewService>("true")
        .OnErrorRedirectAndReplayControl())
    .WithCircuitBreaker(options =>
    {
        options.FailureRatioThreshold = 0.5;
        options.MinimumThroughput = 10;
        options.OnCircuitOpen = CircuitBreakerAction.FallbackToDefault;
    })
    .WithMetrics(metrics)
    .UseDispatchProxy();

app.MapGet("/metrics", () => metrics.GeneratePrometheusOutput());
```

**Grafana queries:**

```promql
# Circuit breaker open rate
sum(rate(experiment_errors_total{error="CircuitBreakerOpenException"}[5m])) by (service)

# Failure rate by condition
sum(rate(experiment_errors_total[5m])) by (service, trial_key)
/
sum(rate(experiment_invocations_total[5m])) by (service, trial_key)
```

## Troubleshooting

### Circuit Not Opening

**Symptom**: Circuit never opens despite failures.

**Solutions:**
1. Check `MinimumThroughput` - may not have enough requests
2. Verify `FailureRatioThreshold` isn't too high
3. Ensure exceptions are being thrown (not swallowed)
4. Check `SamplingDuration` - window may be too short

### Circuit Opening Too Frequently

**Symptom**: Circuit opens and closes repeatedly (flapping).

**Solutions:**
1. Increase `FailureRatioThreshold` (too strict)
2. Increase `MinimumThroughput` (premature from small sample)
3. Increase `BreakDuration` (not enough time to recover)
4. Check if condition has intermittent issues needing fixing

### Fallback Not Working

**Symptom**: Gets `CircuitBreakerOpenException` despite fallback configuration.

**Solutions:**
1. Verify `OnCircuitOpen = CircuitBreakerAction.FallbackToDefault`
2. Ensure `OnErrorRedirectAndReplayControl()` is configured
3. Check that control condition is registered

## See Also

- [Timeout Enforcement](timeout-enforcement.md) - Prevent slow conditions
- [Error Handling](error-handling.md) - Fallback strategies
- [Kill Switch](kill-switch.md) - Manual emergency shutdown
- [Metrics](metrics.md) - Monitor circuit breaker state
