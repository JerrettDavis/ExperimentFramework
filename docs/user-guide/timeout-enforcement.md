# Timeout Enforcement

Timeout enforcement prevents slow conditions from degrading system performance by limiting how long a condition can execute before either throwing an exception or falling back to a control implementation.

## Overview

When a condition takes longer than the configured timeout, the framework can:

1. **Throw Exception**: Fail fast with a `TimeoutException`
2. **Fallback to Control**: Automatically redirect to the control condition

This is particularly useful when:
- Testing new external APIs or services that may be slow
- Gradual rollout of implementations with unknown performance characteristics
- Preventing cascading failures from slow dependencies

## Basic Configuration

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IDatabase>(t => t
        .UsingFeatureFlag("UseCloudDb")
        .AddControl<LocalDb>("false")
        .AddCondition<CloudDb>("true")
        .OnErrorRedirectAndReplayControl())
    .WithTimeout(TimeSpan.FromSeconds(5), TimeoutAction.FallbackToDefault)
    .UseDispatchProxy();

builder.Services.AddExperimentFramework(experiments);
```

## Timeout Actions

### ThrowException

Throws a `TimeoutException` when the condition exceeds the timeout. Use this when you want explicit control over timeout handling.

```csharp
.WithTimeout(TimeSpan.FromSeconds(3), TimeoutAction.ThrowException)
```

**When to use:**
- Development and testing environments
- When timeout should stop request processing
- When you want to handle timeouts explicitly in calling code

**Example:**

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IPaymentGateway>(t => t
        .UsingFeatureFlag("UseNewGateway")
        .AddControl<StableGateway>("false")
        .AddCondition<NewGateway>("true"))
    .WithTimeout(TimeSpan.FromSeconds(10), TimeoutAction.ThrowException)
    .UseDispatchProxy();

// Usage with explicit handling
try
{
    var result = await paymentGateway.ProcessPaymentAsync(payment);
}
catch (TimeoutException)
{
    _logger.LogWarning("Payment processing timed out");
    return Results.Problem("Payment service temporarily unavailable");
}
```

### FallbackToDefault

Automatically redirects to the control condition when timeout occurs. Use this for graceful degradation.

```csharp
.WithTimeout(TimeSpan.FromSeconds(3), TimeoutAction.FallbackToDefault)
```

**When to use:**
- Production environments
- When you have a reliable fallback implementation
- When degraded functionality is acceptable

**Example:**

```csharp
public interface IWeatherService
{
    Task<Weather> GetWeatherAsync(string city);
}

public class ExternalApiWeatherService : IWeatherService
{
    private readonly HttpClient _http;

    public ExternalApiWeatherService(HttpClient http)
    {
        _http = http;
    }

    public async Task<Weather> GetWeatherAsync(string city)
    {
        // May be slow or timeout
        var response = await _http.GetAsync($"https://api.weather.com/{city}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Weather>();
    }
}

public class CachedWeatherService : IWeatherService
{
    private readonly IMemoryCache _cache;

    public CachedWeatherService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<Weather> GetWeatherAsync(string city)
    {
        // Fast fallback from cache
        return Task.FromResult(_cache.Get<Weather>(city) ?? Weather.Unknown);
    }
}

// Configuration
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IWeatherService>(t => t
        .UsingFeatureFlag("UseExternalWeatherApi")
        .AddControl<CachedWeatherService>("false")
        .AddCondition<ExternalApiWeatherService>("true")
        .OnErrorRedirectAndReplayControl())
    .WithTimeout(TimeSpan.FromSeconds(2), TimeoutAction.FallbackToDefault)
    .UseDispatchProxy();

// Usage - transparent fallback
var weather = await weatherService.GetWeatherAsync("Seattle");
// If API takes > 2 seconds, automatically gets cached result
```

## Real-World Example: API Integration

```csharp
public interface IRecommendationEngine
{
    Task<List<Product>> GetRecommendationsAsync(User user);
}

public class MachineLearningEngine : IRecommendationEngine
{
    private readonly HttpClient _mlClient;

    public MachineLearningEngine(HttpClient mlClient)
    {
        _mlClient = mlClient;
    }

    public async Task<List<Product>> GetRecommendationsAsync(User user)
    {
        // ML service may have variable latency
        var response = await _mlClient.PostAsJsonAsync("/predict", user);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Product>>();
    }
}

public class RuleBasedEngine : IRecommendationEngine
{
    private readonly IProductRepository _products;

    public RuleBasedEngine(IProductRepository products)
    {
        _products = products;
    }

    public async Task<List<Product>> GetRecommendationsAsync(User user)
    {
        // Fast, reliable rule-based recommendations
        return await _products.GetPopularByCategory(user.PreferredCategory);
    }
}

// Configuration
builder.Services.AddHttpClient<MachineLearningEngine>(client =>
{
    client.BaseAddress = new Uri("https://ml-service.example.com");
    client.Timeout = TimeSpan.FromSeconds(10); // HTTP client timeout
});

builder.Services.AddScoped<RuleBasedEngine>();
builder.Services.AddScoped<IRecommendationEngine, RuleBasedEngine>();

var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IRecommendationEngine>(t => t
        .UsingFeatureFlag("UseMachineLearning")
        .AddControl<RuleBasedEngine>("false")
        .AddCondition<MachineLearningEngine>("true")
        .OnErrorRedirectAndReplayControl())
    .WithTimeout(TimeSpan.FromSeconds(3), TimeoutAction.FallbackToDefault)
    .UseDispatchProxy();

builder.Services.AddExperimentFramework(experiments);
```

When ML service takes > 3 seconds, users get rule-based recommendations instead.

## Combining with Circuit Breaker

For even better resilience, combine timeout with circuit breaker:

```csharp
dotnet add package ExperimentFramework.Resilience
```

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IWeatherService>(t => t
        .UsingFeatureFlag("UseExternalApi")
        .AddControl<CachedService>("false")
        .AddCondition<ExternalApiService>("true")
        .OnErrorRedirectAndReplayControl())
    .WithTimeout(TimeSpan.FromSeconds(2), TimeoutAction.FallbackToDefault)
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

This configuration:
1. Times out individual requests after 2 seconds
2. Opens circuit if 50% of requests fail/timeout within 30 seconds
3. Stops trying external API for 1 minute when circuit opens
4. Automatically uses cached service during both scenarios

## Best Practices

### 1. Set Realistic Timeouts

Base timeout values on P95/P99 latency of successful requests:

```csharp
// If P99 latency is 2 seconds, set timeout to 3-4 seconds
.WithTimeout(TimeSpan.FromSeconds(3), TimeoutAction.FallbackToDefault)
```

**Too short**: Excessive false positives, constant fallbacks
**Too long**: Slow user experience during actual failures

### 2. Use Different Timeouts Per Environment

```csharp
var timeout = builder.Environment.IsProduction()
    ? TimeSpan.FromSeconds(3)
    : TimeSpan.FromSeconds(30); // Longer timeout for dev/debugging

.WithTimeout(timeout, TimeoutAction.FallbackToDefault)
```

### 3. Log Timeout Events

Monitor timeout frequency to adjust settings:

```csharp
.Trial<IService>(t => t
    .UsingFeatureFlag("UseNewService")
    .AddControl<DefaultService>("false")
    .AddCondition<NewService>("true")
    .OnErrorRedirectAndReplayControl())
.WithTimeout(TimeSpan.FromSeconds(5), TimeoutAction.FallbackToDefault)
.AddLogger(l => l.AddErrorLogging()) // Logs timeout as error
```

### 4. Ensure Idempotency

Operations may be executed twice when timeout triggers fallback:

```csharp
// ❌ Bad - not idempotent
public async Task PlaceOrderAsync(Order order)
{
    await _db.InsertAsync(order);  // Executed on both timeout trial and fallback!
    await _email.SendAsync(order.ConfirmationEmail);
}

// ✅ Good - idempotent
public async Task PlaceOrderAsync(Order order)
{
    await _db.UpsertAsync(order);  // Safe to call multiple times
    await _email.SendAsync(order.ConfirmationEmail, idempotencyKey: order.Id);
}
```

### 5. Configure HTTP Client Timeouts Separately

ExperimentFramework timeout wraps the entire method call. Configure underlying HTTP client timeouts independently:

```csharp
builder.Services.AddHttpClient<ExternalService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10); // HTTP timeout
});

// Experiment timeout should be less than HTTP timeout
.WithTimeout(TimeSpan.FromSeconds(8), TimeoutAction.FallbackToDefault)
```

## Monitoring

Track timeout rates with metrics:

```csharp
dotnet add package ExperimentFramework.Metrics.Exporters
```

```csharp
var metrics = new PrometheusExperimentMetrics();

var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IService>(t => t
        .UsingFeatureFlag("UseNewService")
        .AddControl<DefaultService>("false")
        .AddCondition<NewService>("true")
        .OnErrorRedirectAndReplayControl())
    .WithTimeout(TimeSpan.FromSeconds(5), TimeoutAction.FallbackToDefault)
    .WithMetrics(metrics)
    .UseDispatchProxy();

app.MapGet("/metrics", () => metrics.GeneratePrometheusOutput());
```

**Grafana query for timeout rate:**

```promql
# Timeout rate per condition
sum(rate(experiment_errors_total{error="TimeoutException"}[5m])) by (service, trial_key)
/
sum(rate(experiment_invocations_total[5m])) by (service, trial_key)
```

## Troubleshooting

### Timeouts Not Working

**Symptom**: Condition runs longer than configured timeout.

**Solutions:**
1. Verify `WithTimeout()` is called before `UseDispatchProxy()`
2. Check that condition method is actually async (not sync blocking)
3. Ensure timeout duration is correct

### Excessive Timeouts

**Symptom**: All requests timing out, constant fallback to control.

**Solutions:**
1. Increase timeout duration based on actual latency
2. Check if experimental condition has performance issues
3. Use kill switch to temporarily disable problematic condition
4. Add circuit breaker to prevent repeated timeout attempts

### Fallback Not Occurring

**Symptom**: `TimeoutException` thrown despite `FallbackToDefault` action.

**Solutions:**
1. Verify `OnErrorRedirectAndReplayControl()` is configured
2. Check that control condition is properly registered
3. Ensure control condition doesn't also timeout

## See Also

- [Circuit Breaker](circuit-breaker.md) - Automatically disable failing trials
- [Error Handling](error-handling.md) - Error policies and fallback strategies
- [Metrics](metrics.md) - Track timeout rates and latency
