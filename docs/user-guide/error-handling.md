# Error Handling

ExperimentFramework provides built-in error handling strategies to manage failures in experimental implementations. This allows you to safely test new code while maintaining system reliability.

## Error Policies

Error policies determine what happens when a condition throws an exception. The framework supports five policies:

| Policy | Behavior | Use Case |
|--------|----------|----------|
| Throw | Propagate the exception immediately | Development, when you want to see failures |
| FallbackToControl | Fall back to control on error | Production, safe rollback to stable code |
| TryAny | Try all conditions until one succeeds | High availability scenarios |
| FallbackTo | Redirect to specific fallback condition | Dedicated diagnostics/safe-mode handlers |
| TryInOrder | Try ordered list of fallback conditions | Fine-grained control over fallback strategy |

## Throw Policy (Default)

The throw policy propagates exceptions immediately without attempting fallback.

### When to Use

- Development and testing environments
- When you need to see and diagnose failures quickly
- When condition failures should stop request processing

### Configuration

Throw is the default policy if no policy is specified:

```csharp
.Trial<IPaymentProcessor>(t => t
    .UsingFeatureFlag("UseNewPaymentProvider")
    .AddControl<StripePayment>("false")
    .AddVariant<NewPaymentProvider>("true"))
    // No error policy specified - uses Throw by default
```

Or explicitly:

```csharp
.OnErrorThrow()
```

### Behavior

When a condition throws an exception:

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

## FallbackToControl Policy

The fallback-to-control policy catches exceptions from the selected condition and falls back to the control.

### When to Use

- Production environments
- When you want to test new implementations with automatic rollback
- When the control implementation is known to be stable
- When partial availability is better than complete failure

### Configuration

```csharp
.Trial<IPaymentProcessor>(t => t
    .UsingFeatureFlag("UseNewPaymentProvider")
    .AddControl<StripePayment>("false")
    .AddVariant<NewPaymentProvider>("true")
    .OnErrorFallbackToControl())
```

### Behavior

When the selected condition throws:

```
1. Try NewPaymentProvider
   └─ Throws PaymentException

2. Catch exception

3. Try StripePayment (control)
   └─ Succeeds

4. Return result
```

Example:

```csharp
// Feature flag is enabled, so NewPaymentProvider is selected
var result = await paymentProcessor.ChargeAsync(100m);

// If NewPaymentProvider throws, framework automatically:
// 1. Catches the exception
// 2. Switches to StripePayment (control)
// 3. Retries the operation
// 4. Returns the result from StripePayment

// Caller receives successful result and doesn't see the exception
```

### Logging Failed Attempts

Use the error logging decorator to track when fallback occurs:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .AddLogger(l => l.AddErrorLogging())
    .Trial<IPaymentProcessor>(t => t
        .UsingFeatureFlag("UseNewPaymentProvider")
        .AddControl<StripePayment>("false")
        .AddVariant<NewPaymentProvider>("true")
        .OnErrorFallbackToControl());
```

Logged output when fallback occurs:

```
error: ExperimentFramework.ErrorLogging[0]
      Experiment error: IPaymentProcessor.ChargeAsync trial=true
      System.PaymentException: Service unavailable
         at NewPaymentProvider.ChargeAsync(Decimal amount)
```

### Avoiding Retry Storms

Be cautious when the control can also fail:

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

In this case, both conditions fail and the exception propagates:

```
1. Try NewPaymentProvider -> Throws
2. Try StripePayment (control) -> Throws
3. Propagate exception to caller
```

## TryAny Policy

The try-any policy tries all registered conditions in sequence until one succeeds.

### When to Use

- High availability scenarios
- When you have multiple fallback options
- When any successful response is acceptable
- Circuit breaker patterns

### Configuration

```csharp
.Trial<ICache>(t => t
    .UsingConfigurationKey("Cache:Provider")
    .AddControl<InMemoryCache>("")
    .AddVariant<RedisCache>("redis")
    .AddVariant<MemcachedCache>("memcached")
    .OnErrorTryAny())
```

### Behavior

When a condition throws, the framework tries the next available condition:

```
1. Try RedisCache (selected by configuration)
   └─ Throws ConnectionException

2. Try MemcachedCache
   └─ Throws ConnectionException

3. Try InMemoryCache (control)
   └─ Succeeds

4. Return result
```

### Condition Order

Conditions are attempted in this order:

1. Selected condition (based on selection mode)
2. Other non-control conditions (order unspecified)
3. Control (always last)

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

### When All Conditions Fail

If all conditions throw exceptions, the last exception is propagated:

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

## FallbackTo Policy

The fallback-to policy redirects to a specific fallback condition (e.g., a Noop diagnostics handler) when the selected condition fails.

### When to Use

- When you need a dedicated safe-mode or diagnostics handler
- When you want fine-grained control over which condition handles failures
- When the fallback condition should differ from the control
- Circuit breaker patterns with specific fallback logic

### Configuration

```csharp
.Trial<IDiagnosticsHandler>(t => t
    .UsingFeatureFlag("UsePrimaryDiagnostics")
    .AddControl<PrimaryDiagnosticsHandler>("true")
    .AddVariant<SecondaryDiagnosticsHandler>("false")
    .AddVariant<NoopDiagnosticsHandler>("noop")
    .OnErrorFallbackTo("noop"))
```

### Behavior

When the selected condition throws, the framework redirects to the specified fallback condition:

```
1. Try PrimaryDiagnosticsHandler (selected by feature flag)
   └─ Throws TimeoutException

2. Catch exception

3. Try NoopDiagnosticsHandler (specified fallback)
   └─ Succeeds (no-op returns immediately)

4. Return result
```

### Example Scenario

Diagnostics handler with safe-mode fallback:

```csharp
public class PrimaryDiagnosticsHandler : IDiagnosticsHandler
{
    public async Task CollectDiagnosticsAsync()
    {
        // May timeout connecting to diagnostics service
        throw new TimeoutException("Diagnostics service unavailable");
    }
}

public class NoopDiagnosticsHandler : IDiagnosticsHandler
{
    public Task CollectDiagnosticsAsync()
    {
        // No-op: always succeeds, does nothing
        return Task.CompletedTask;
    }
}
```

Usage:

```csharp
// Primary diagnostics fails, falls back to noop
await diagnosticsHandler.CollectDiagnosticsAsync();

// Application continues without diagnostics
// No exception is thrown
```

### When Fallback Condition Also Fails

If the specified fallback condition also throws, the exception propagates:

```
1. Try PrimaryDiagnosticsHandler -> Throws TimeoutException
2. Try NoopDiagnosticsHandler -> Throws InvalidOperationException
3. Propagate InvalidOperationException to caller
```

## TryInOrder Policy

The try-in-order policy tries an ordered list of fallback conditions in exact sequence until one succeeds.

### When to Use

- When you need fine-grained control over fallback priority
- Multi-tier caching strategies (cloud → local → memory → static)
- When fallback order matters for performance or cost
- When you have specific degradation paths

### Configuration

```csharp
.Trial<IDataService>(t => t
    .UsingFeatureFlag("UseCloudDatabase")
    .AddControl<CloudDatabaseImpl>("true")
    .AddVariant<LocalCacheImpl>("cache")
    .AddVariant<InMemoryCacheImpl>("memory")
    .AddVariant<StaticDataImpl>("static")
    .OnErrorTryInOrder("cache", "memory", "static"))
```

### Behavior

When a condition throws, the framework tries the fallback conditions in exact order:

```
1. Try CloudDatabaseImpl (selected by feature flag)
   └─ Throws ConnectionException

2. Try LocalCacheImpl (first fallback)
   └─ Throws IOException

3. Try InMemoryCacheImpl (second fallback)
   └─ Succeeds

4. Return result
```

The framework stops at the first successful condition and doesn't try remaining fallbacks.

### Example Scenario

Multi-tier data service with degradation strategy:

```csharp
public interface IDataService
{
    Task<CustomerData> GetCustomerDataAsync(int customerId);
}

public class CloudDatabaseImpl : IDataService
{
    public async Task<CustomerData> GetCustomerDataAsync(int customerId)
    {
        // Cloud database might be unavailable
        throw new ConnectionException("Cloud database unreachable");
    }
}

public class LocalCacheImpl : IDataService
{
    public async Task<CustomerData> GetCustomerDataAsync(int customerId)
    {
        // Local cache might be corrupted
        throw new IOException("Cache file corrupted");
    }
}

public class InMemoryCacheImpl : IDataService
{
    private readonly ConcurrentDictionary<int, CustomerData> _cache = new();

    public Task<CustomerData> GetCustomerDataAsync(int customerId)
    {
        // In-memory cache succeeds with cached data
        if (_cache.TryGetValue(customerId, out var data))
            return Task.FromResult(data);

        return Task.FromResult(new CustomerData { Id = customerId, Name = "Unknown" });
    }
}

public class StaticDataImpl : IDataService
{
    public Task<CustomerData> GetCustomerDataAsync(int customerId)
    {
        // Static fallback returns placeholder data
        return Task.FromResult(new CustomerData
        {
            Id = customerId,
            Name = "Loading...",
            IsPlaceholder = true
        });
    }
}
```

Usage:

```csharp
// Configuration: Cloud → LocalCache → InMemory → Static
var customerData = await dataService.GetCustomerDataAsync(123);

// Framework tries:
// 1. CloudDatabase (fails - connection error)
// 2. LocalCache (fails - corrupted)
// 3. InMemoryCache (succeeds - returns cached data)
// 4. StaticData (not tried - InMemory succeeded)

// Caller receives result from InMemoryCache
```

### Condition Order Rules

The framework tries conditions in this exact order:

1. **Selected condition** (based on selection mode) - tried first
2. **Ordered fallback keys** (in the order you specify) - tried in sequence
3. **Fallback keys are skipped if they match the selected condition** - prevents duplicate attempts

Example:

```csharp
.OnErrorTryInOrder("cache", "memory", "static")
```

If feature flag selects `"memory"`, the order becomes:
```
1. Try "memory" (selected)
2. Try "cache" (first fallback, not already tried)
3. Try "static" (second fallback, not already tried)
```

The selected condition is never retried even if it appears in the fallback list.

### When All Conditions Fail

If all conditions in the ordered sequence throw exceptions, the last exception propagates:

```
1. Try CloudDatabaseImpl -> Throws ConnectionException
2. Try LocalCacheImpl -> Throws IOException
3. Try InMemoryCacheImpl -> Throws InvalidOperationException
4. Try StaticDataImpl -> Throws NotImplementedException
5. Propagate NotImplementedException to caller
```

### Performance Considerations

Each fallback attempt incurs the cost of:
- Service resolution from DI container
- Decorator pipeline execution
- Method invocation overhead

For performance-critical paths, consider:
- Keeping the fallback list short (2-3 conditions max)
- Using fast-fail implementations that fail quickly
- Monitoring fallback rates to identify problematic conditions

## Error Logging Decorator

The error logging decorator logs exceptions before they propagate or trigger fallback.

### Configuration

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .AddLogger(l => l.AddErrorLogging())
    .Trial<IPaymentProcessor>(t => t
        .UsingFeatureFlag("UseNewPaymentProvider")
        .AddControl<StripePayment>("false")
        .AddVariant<NewPaymentProvider>("true")
        .OnErrorFallbackToControl());
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
- Condition key that failed
- Full exception with stack trace

## Choosing an Error Policy

Use this decision tree:

```
Is this production?
├─ No (Development/Testing):
│   └─ Use Throw (see failures immediately)
└─ Yes (Production):
    └─ Do you have a stable control implementation?
        ├─ Yes:
        │   └─ Use FallbackToControl
        └─ No:
            └─ Do you have multiple fallback options?
                ├─ Yes: Use TryAny
                └─ No: Use Throw (and handle in application code)
```

## Best Practices

### 1. Always Have a Stable Control

The control should be your most reliable implementation:

```csharp
// Good: Stable implementation as control
.AddControl<ProvenPaymentProvider>("default")
.AddVariant<NewExperimentalProvider>("experimental")

// Bad: Experimental implementation as control
.AddControl<ExperimentalProvider>("default")
.AddVariant<ProvenProvider>("proven")
```

### 2. Use Error Logging

Always enable error logging in production to track fallback occurrences:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .AddLogger(l => l
        .AddBenchmarks()
        .AddErrorLogging())  // Track when failures occur
    .Trial<IPaymentProcessor>(t => t
        .UsingFeatureFlag("UseNewPaymentProvider")
        .AddControl<StripePayment>("false")
        .AddVariant<NewPaymentProvider>("true")
        .OnErrorFallbackToControl());
```

### 3. Monitor Fallback Rates

Track how often fallback occurs to identify problematic conditions:

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

### 4. Avoid Side Effects in Failing Conditions

Ensure conditions don't perform irreversible operations before failing:

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

When using TryAny, ensure operations are idempotent:

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
    .Trial<IPaymentProcessor>(t => t
        .UsingFeatureFlag("UseNewPaymentProvider")
        .AddControl<StripePayment>("false")
        .AddVariant<NewPaymentProvider>("true")
        .OnErrorFallbackToControl());

services.AddExperimentFramework(experiments);
services.AddOpenTelemetryExperimentTracking();
```

This provides:

- Error logs when conditions fail
- Timing metrics for successful and failed attempts
- Distributed traces showing fallback paths
- Telemetry tags indicating which condition was attempted and which succeeded

## Next Steps

- [Telemetry](telemetry.md) - Add observability to track experiment behavior
- [Advanced Topics](advanced.md) - Implement custom error handling logic
- [Samples](samples.md) - See complete examples of error handling patterns
