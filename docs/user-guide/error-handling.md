# Error Handling

ExperimentFramework provides built-in error handling strategies to manage failures in experimental implementations. This allows you to safely test new code while maintaining system reliability.

## Error Policies

Error policies determine what happens when a trial throws an exception. The framework supports three policies:

| Policy | Behavior | Use Case |
|--------|----------|----------|
| Throw | Propagate the exception immediately | Development, when you want to see failures |
| RedirectAndReplayDefault | Fall back to default trial on error | Production, safe rollback to stable code |
| RedirectAndReplayAny | Try all trials until one succeeds | High availability scenarios |

## Throw Policy (Default)

The throw policy propagates exceptions immediately without attempting fallback.

### When to Use

- Development and testing environments
- When you need to see and diagnose failures quickly
- When trial failures should stop request processing

### Configuration

Throw is the default policy if no policy is specified:

```csharp
.Define<IPaymentProcessor>(c => c
    .UsingFeatureFlag("UseNewPaymentProvider")
    .AddDefaultTrial<StripePayment>("false")
    .AddTrial<NewPaymentProvider>("true"))
    // No error policy specified - uses Throw by default
```

Or explicitly:

```csharp
.OnErrorThrow()
```

### Behavior

When a trial throws an exception:

```csharp
public class NewPaymentProvider : IPaymentProcessor
{
    public async Task<PaymentResult> ChargeAsync(decimal amount)
    {
        throw new PaymentException("Service unavailable");
    }
}
```

The exception propagates to the caller:

```csharp
try
{
    await paymentProcessor.ChargeAsync(100m);
}
catch (PaymentException ex)
{
    // Exception is thrown directly
    // No fallback attempted
}
```

## RedirectAndReplayDefault Policy

The redirect-and-replay-default policy catches exceptions from the selected trial and falls back to the default trial.

### When to Use

- Production environments
- When you want to test new implementations with automatic rollback
- When the default implementation is known to be stable
- When partial availability is better than complete failure

### Configuration

```csharp
.Define<IPaymentProcessor>(c => c
    .UsingFeatureFlag("UseNewPaymentProvider")
    .AddDefaultTrial<StripePayment>("false")
    .AddTrial<NewPaymentProvider>("true")
    .OnErrorRedirectAndReplayDefault())
```

### Behavior

When the selected trial throws:

```
1. Try NewPaymentProvider
   └─ Throws PaymentException

2. Catch exception

3. Try StripePayment (default trial)
   └─ Succeeds

4. Return result
```

Example:

```csharp
// Feature flag is enabled, so NewPaymentProvider is selected
var result = await paymentProcessor.ChargeAsync(100m);

// If NewPaymentProvider throws, framework automatically:
// 1. Catches the exception
// 2. Switches to StripePayment (default)
// 3. Retries the operation
// 4. Returns the result from StripePayment

// Caller receives successful result and doesn't see the exception
```

### Logging Failed Attempts

Use the error logging decorator to track when fallback occurs:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .AddLogger(l => l.AddErrorLogging())
    .Define<IPaymentProcessor>(c => c
        .UsingFeatureFlag("UseNewPaymentProvider")
        .AddDefaultTrial<StripePayment>("false")
        .AddTrial<NewPaymentProvider>("true")
        .OnErrorRedirectAndReplayDefault());
```

Logged output when fallback occurs:

```
error: ExperimentFramework.ErrorLogging[0]
      Experiment error: IPaymentProcessor.ChargeAsync trial=true
      System.PaymentException: Service unavailable
         at NewPaymentProvider.ChargeAsync(Decimal amount)
```

### Avoiding Retry Storms

Be cautious when the default trial can also fail:

```csharp
public class StripePayment : IPaymentProcessor
{
    public async Task<PaymentResult> ChargeAsync(decimal amount)
    {
        // This can also throw
        throw new PaymentException("Stripe is down");
    }
}
```

In this case, both trials fail and the exception propagates:

```
1. Try NewPaymentProvider -> Throws
2. Try StripePayment (default) -> Throws
3. Propagate exception to caller
```

## RedirectAndReplayAny Policy

The redirect-and-replay-any policy tries all registered trials in sequence until one succeeds.

### When to Use

- High availability scenarios
- When you have multiple fallback options
- When any successful response is acceptable
- Circuit breaker patterns

### Configuration

```csharp
.Define<ICache>(c => c
    .UsingConfigurationKey("Cache:Provider")
    .AddDefaultTrial<InMemoryCache>("")
    .AddTrial<RedisCache>("redis")
    .AddTrial<MemcachedCache>("memcached")
    .OnErrorRedirectAndReplayAny())
```

### Behavior

When a trial throws, the framework tries the next available trial:

```
1. Try RedisCache (selected by configuration)
   └─ Throws ConnectionException

2. Try MemcachedCache
   └─ Throws ConnectionException

3. Try InMemoryCache (default)
   └─ Succeeds

4. Return result
```

### Trial Order

Trials are attempted in this order:

1. Selected trial (based on selection mode)
2. Other non-default trials (order unspecified)
3. Default trial (always last)

### Example Scenario

Caching with multiple fallback options:

```csharp
public interface ICache
{
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value);
}

public class RedisCache : ICache
{
    public async Task<T> GetAsync<T>(string key)
    {
        // Redis is down
        throw new ConnectionException("Redis unavailable");
    }
}

public class MemcachedCache : ICache
{
    public async Task<T> GetAsync<T>(string key)
    {
        // Memcached is also down
        throw new ConnectionException("Memcached unavailable");
    }
}

public class InMemoryCache : ICache
{
    private readonly ConcurrentDictionary<string, object> _cache = new();

    public Task<T> GetAsync<T>(string key)
    {
        // Always succeeds (no external dependencies)
        if (_cache.TryGetValue(key, out var value))
        {
            return Task.FromResult((T)value);
        }
        return Task.FromResult(default(T));
    }
}
```

Usage:

```csharp
// Configuration specifies Redis
// Redis fails, Memcached fails, InMemory succeeds
var value = await cache.GetAsync<string>("user:123");

// Caller receives the result from InMemoryCache
// No exception is thrown
```

### When All Trials Fail

If all trials throw exceptions, the last exception is propagated:

```csharp
public class InMemoryCache : ICache
{
    public Task<T> GetAsync<T>(string key)
    {
        // Even the fallback fails
        throw new OutOfMemoryException();
    }
}
```

Result:

```
1. Try RedisCache -> Throws ConnectionException
2. Try MemcachedCache -> Throws ConnectionException
3. Try InMemoryCache -> Throws OutOfMemoryException
4. Propagate OutOfMemoryException to caller
```

## Error Logging Decorator

The error logging decorator logs exceptions before they propagate or trigger fallback.

### Configuration

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .AddLogger(l => l.AddErrorLogging())
    .Define<IPaymentProcessor>(c => c
        .UsingFeatureFlag("UseNewPaymentProvider")
        .AddDefaultTrial<StripePayment>("false")
        .AddTrial<NewPaymentProvider>("true")
        .OnErrorRedirectAndReplayDefault());
```

### Logged Information

When an exception occurs:

```
error: ExperimentFramework.ErrorLogging[0]
      Experiment error: IPaymentProcessor.ChargeAsync trial=true
      System.InvalidOperationException: Payment gateway timeout
         at NewPaymentProvider.ChargeAsync(Decimal amount)
         at ExperimentFramework.ExperimentProxy.InvokeAsync(...)
```

The log includes:

- Service interface name
- Method name
- Trial key that failed
- Full exception with stack trace

## Choosing an Error Policy

Use this decision tree:

```
Is this production?
├─ No (Development/Testing):
│   └─ Use Throw (see failures immediately)
└─ Yes (Production):
    └─ Do you have a stable default implementation?
        ├─ Yes:
        │   └─ Use RedirectAndReplayDefault
        └─ No:
            └─ Do you have multiple fallback options?
                ├─ Yes: Use RedirectAndReplayAny
                └─ No: Use Throw (and handle in application code)
```

## Best Practices

### 1. Always Have a Stable Default

The default trial should be your most reliable implementation:

```csharp
// Good: Stable implementation as default
.AddDefaultTrial<ProvenPaymentProvider>("default")
.AddTrial<NewExperimentalProvider>("experimental")

// Bad: Experimental implementation as default
.AddDefaultTrial<ExperimentalProvider>("default")
.AddTrial<ProvenProvider>("proven")
```

### 2. Use Error Logging

Always enable error logging in production to track fallback occurrences:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .AddLogger(l => l
        .AddBenchmarks()
        .AddErrorLogging())  // Track when failures occur
    .Define<IPaymentProcessor>(c => c
        .UsingFeatureFlag("UseNewPaymentProvider")
        .AddDefaultTrial<StripePayment>("false")
        .AddTrial<NewPaymentProvider>("true")
        .OnErrorRedirectAndReplayDefault());
```

### 3. Monitor Fallback Rates

Track how often fallback occurs to identify problematic trials:

```csharp
public class MetricsDecorator : IExperimentDecorator
{
    private readonly IMetrics _metrics;

    public async ValueTask<object?> InvokeAsync(
        InvocationContext context,
        Func<ValueTask<object?>> next)
    {
        try
        {
            return await next();
        }
        catch (Exception)
        {
            _metrics.Increment($"experiment.fallback.{context.ServiceType.Name}");
            throw;
        }
    }
}
```

### 4. Avoid Side Effects in Failing Trials

Ensure trials don't perform irreversible operations before failing:

```csharp
// Bad: Side effect before failure
public async Task ProcessPaymentAsync(Payment payment)
{
    await _database.SavePaymentAttemptAsync(payment);  // Side effect
    throw new InvalidOperationException("Payment failed");
}

// Good: Validate before side effects
public async Task ProcessPaymentAsync(Payment payment)
{
    ValidatePayment(payment);  // Throws if invalid
    await _database.SavePaymentAttemptAsync(payment);  // Only if valid
    await ProcessPaymentInternalAsync(payment);
}
```

### 5. Consider Idempotency

When using RedirectAndReplayAny, ensure operations are idempotent:

```csharp
public async Task SendEmailAsync(Email email)
{
    // Use idempotency key to prevent duplicate sends
    var idempotencyKey = $"email:{email.Id}";

    if (await _cache.GetAsync<bool>(idempotencyKey))
    {
        return; // Already sent
    }

    await _emailProvider.SendAsync(email);
    await _cache.SetAsync(idempotencyKey, true, TimeSpan.FromHours(24));
}
```

## Combining with Telemetry

Error policies work seamlessly with telemetry to provide observability:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .AddLogger(l => l
        .AddBenchmarks()
        .AddErrorLogging())
    .Define<IPaymentProcessor>(c => c
        .UsingFeatureFlag("UseNewPaymentProvider")
        .AddDefaultTrial<StripePayment>("false")
        .AddTrial<NewPaymentProvider>("true")
        .OnErrorRedirectAndReplayDefault());

services.AddExperimentFramework(experiments);
services.AddOpenTelemetryExperimentTracking();
```

This provides:

- Error logs when trials fail
- Timing metrics for successful and failed attempts
- Distributed traces showing fallback paths
- Telemetry tags indicating which trial was attempted and which succeeded

## Next Steps

- [Telemetry](telemetry.md) - Add observability to track experiment behavior
- [Advanced Topics](advanced.md) - Implement custom error handling logic
- [Samples](samples.md) - See complete examples of error handling patterns
