# Core Concepts

This guide explains the fundamental concepts of ExperimentFramework and how they work together to enable runtime experimentation.

## Trials

A trial is a specific implementation of a service interface that participates in an experiment. Each trial represents a different approach to solving the same problem.

### Trial Registration

Trials must be registered with the dependency injection container as concrete types:

```csharp
services.AddScoped<LocalDatabase>();
services.AddScoped<CloudDatabase>();
services.AddScoped<RedisCache>();
```

The framework resolves trials by their concrete type, so they must be registered separately from the interface registration.

### Trial Keys

Each trial is identified by a unique string key. The trial key is what the selection logic returns to determine which implementation to use.

For boolean feature flags, trial keys are typically "true" and "false":

```csharp
.AddDefaultTrial<LocalDatabase>("false")
.AddTrial<CloudDatabase>("true")
```

For configuration values or variants, trial keys can be any string:

```csharp
.AddDefaultTrial<StripePayment>("stripe")
.AddTrial<PayPalPayment>("paypal")
.AddTrial<CryptoPayment>("crypto")
```

### Default Trial

Every experiment must specify a default trial. This is the implementation used when:

- The selection criteria evaluates to an unknown value
- The selection mechanism fails
- No configuration is provided

The default trial should be your stable, well-tested implementation:

```csharp
.AddDefaultTrial<StripePayment>("stripe")  // Stable implementation
.AddTrial<NewPaymentProvider>("newprovider")  // Experimental implementation
```

## Proxies

When you register an experiment, the framework replaces your interface registration with a dynamically generated proxy. This proxy intercepts all method calls and routes them to the appropriate trial.

### How Proxies Work

Proxies are generated at compile time using Roslyn source generators:

1. The source generator analyzes your experiment configuration
2. For each interface, a strongly-typed proxy class is generated
3. When you request an interface from DI, you receive a generated proxy instance
4. Method calls on the proxy use direct invocations (no reflection)
5. The proxy evaluates selection criteria and resolves the appropriate trial
6. The method is invoked directly on the trial instance
7. Results are returned to the caller with zero boxing overhead

### Proxy Transparency

From the perspective of your application code, the proxy is indistinguishable from a real implementation:

```csharp
public class OrderService
{
    private readonly IPaymentProcessor _payment;

    public OrderService(IPaymentProcessor payment)
    {
        // _payment is actually a proxy, but your code doesn't know or care
        _payment = payment;
    }

    public async Task ProcessOrderAsync(Order order)
    {
        // This call goes through the proxy
        var result = await _payment.ChargeAsync(order.Total);

        // The proxy selected a trial, invoked it, and returned the result
        return result;
    }
}
```

### Proxy Limitations

Because proxies are generated dynamically, there are some constraints:

- Only interface-based services can be proxied (not classes)
- Trial implementations must be registered by concrete type
- The interface methods must be virtual (interfaces guarantee this)
- Generic return types like `Task<T>` and `ValueTask<T>` are supported

## Service Lifetimes

Trial implementations can have any service lifetime (Transient, Scoped, Singleton), but the proxy always matches the lifetime of the original interface registration.

```csharp
// Original registration was Scoped
services.AddScoped<IDatabase, LocalDatabase>();

// Trials can have different lifetimes
services.AddScoped<LocalDatabase>();      // Scoped
services.AddSingleton<CloudDatabase>();   // Singleton

// The proxy will be Scoped (matching the original)
var experiments = ExperimentFrameworkBuilder.Create()
    .Define<IDatabase>(c => c
        .UsingFeatureFlag("UseCloud")
        .AddDefaultTrial<LocalDatabase>("false")
        .AddTrial<CloudDatabase>("true"));

services.AddExperimentFramework(experiments);
```

The proxy is created once per scope (for scoped services), but trials are resolved according to their registered lifetime.

## Decorator Pipeline

Decorators wrap the execution of trials to provide cross-cutting concerns without modifying trial implementations.

### How Decorators Work

Decorators execute in the order they are registered, forming a pipeline:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .AddLogger(l => l.AddBenchmarks())     // First decorator
    .AddLogger(l => l.AddErrorLogging())   // Second decorator
    .Define<IDatabase>(c => c
        .UsingFeatureFlag("UseCloud")
        .AddDefaultTrial<LocalDatabase>("false")
        .AddTrial<CloudDatabase>("true"));
```

Execution flow:

```
Method Call
    ↓
Benchmark Decorator (start timer)
    ↓
Error Logging Decorator (try/catch wrapper)
    ↓
Trial Execution
    ↓
Error Logging Decorator (log if exception)
    ↓
Benchmark Decorator (stop timer, log elapsed time)
    ↓
Return Result
```

### Built-in Decorators

The framework provides two built-in decorator factories:

**Benchmark Decorator**: Measures and logs execution time

```csharp
.AddLogger(l => l.AddBenchmarks())
```

Logs output:
```
info: ExperimentFramework.Benchmarks[0]
      Experiment call: IDatabase.QueryAsync trial=true elapsedMs=42.3
```

**Error Logging Decorator**: Logs exceptions before they propagate

```csharp
.AddLogger(l => l.AddErrorLogging())
```

Logs output when an error occurs:
```
error: ExperimentFramework.ErrorLogging[0]
      Experiment error: IDatabase.QueryAsync trial=true
      System.InvalidOperationException: Connection failed
```

### Custom Decorators

You can create custom decorators by implementing `IExperimentDecorator` and `IExperimentDecoratorFactory`:

```csharp
public class CachingDecorator : IExperimentDecorator
{
    private readonly IMemoryCache _cache;

    public CachingDecorator(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async ValueTask<object?> InvokeAsync(
        InvocationContext context,
        Func<ValueTask<object?>> next)
    {
        var cacheKey = $"{context.ServiceType.Name}:{context.MethodName}";

        if (_cache.TryGetValue(cacheKey, out object? cached))
        {
            return cached;
        }

        var result = await next();
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }
}

public class CachingDecoratorFactory : IExperimentDecoratorFactory
{
    public IExperimentDecorator Create(IServiceProvider serviceProvider)
    {
        var cache = serviceProvider.GetRequiredService<IMemoryCache>();
        return new CachingDecorator(cache);
    }
}
```

Register custom decorators:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .AddDecoratorFactory(new CachingDecoratorFactory())
    .Define<IDatabase>(c => c
        .UsingFeatureFlag("UseCloud")
        .AddDefaultTrial<LocalDatabase>("false")
        .AddTrial<CloudDatabase>("true"));
```

## Dependency Injection Integration

The framework integrates deeply with .NET's dependency injection system.

### Registration Order

The order of registration is important:

```csharp
// 1. Register trial implementations first
services.AddScoped<LocalDatabase>();
services.AddScoped<CloudDatabase>();

// 2. Register the interface with default implementation
services.AddScoped<IDatabase, LocalDatabase>();

// 3. Add feature management (if using feature flags)
services.AddFeatureManagement();

// 4. Define experiments
var experiments = ExperimentFrameworkBuilder.Create()
    .Define<IDatabase>(c => c
        .UsingFeatureFlag("UseCloud")
        .AddDefaultTrial<LocalDatabase>("false")
        .AddTrial<CloudDatabase>("true"));

// 5. Register the experiment framework
services.AddExperimentFramework(experiments);
```

### What AddExperimentFramework Does

When you call `AddExperimentFramework()`, the framework:

1. Removes the existing registration for `IDatabase`
2. Keeps the concrete type registrations (`LocalDatabase`, `CloudDatabase`)
3. Adds a new registration for `IDatabase` that returns a proxy
4. Preserves the original service lifetime

### Trial Resolution

When a trial is selected, it's resolved from the service provider:

```csharp
// Inside the proxy
var trial = serviceProvider.GetRequiredService<CloudDatabase>();
```

This means trials receive their dependencies from DI normally:

```csharp
public class CloudDatabase : IDatabase
{
    private readonly ILogger<CloudDatabase> _logger;
    private readonly IConfiguration _config;

    // Dependencies are injected by the DI container
    public CloudDatabase(ILogger<CloudDatabase> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }
}
```

## Request-Scoped Consistency

For scoped services, the framework ensures consistent trial selection within a scope.

### Feature Manager Snapshot

When using feature flags with `IFeatureManagerSnapshot`, the feature evaluation is cached per scope:

```csharp
using (var scope = serviceProvider.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDatabase>();

    // First call evaluates the feature flag
    await db.QueryAsync();  // Uses CloudDatabase (flag is true)

    // Subsequent calls use the cached evaluation
    await db.QueryAsync();  // Uses CloudDatabase (same as above)

    // Even if the configuration changes, this scope continues using CloudDatabase
}
```

This ensures that all operations within a single request (represented by a scope) see consistent behavior.

### Why This Matters

Consistency within a scope prevents confusing scenarios:

```csharp
// Without snapshot consistency, this could happen:
var data = await db.GetDataAsync();        // Uses CloudDatabase
await db.SaveDataAsync(data);              // Uses LocalDatabase (flag changed!)
                                           // Data loss - saved to wrong database!

// With snapshot consistency:
var data = await db.GetDataAsync();        // Uses CloudDatabase
await db.SaveDataAsync(data);              // Uses CloudDatabase (same trial)
                                           // Correct - saved to same database
```

## Selection Logic

The proxy evaluates selection criteria on every method call to determine which trial to execute.

### Evaluation Timing

Selection happens immediately before each method invocation:

```csharp
public async Task ProcessAsync()
{
    // Selection evaluated here
    await db.QueryAsync();

    // Selection evaluated again here
    await db.QueryAsync();
}
```

For scoped services using `IFeatureManagerSnapshot`, the evaluation is cached within the scope.

### Selection Flow

1. Proxy intercepts method call
2. Selection mode determines the trial key:
   - Feature flag: Check if flag is enabled
   - Configuration: Read configuration value
   - Variant: Query variant feature manager
   - Sticky routing: Hash user identity
3. Trial key is matched to registered trials
4. If no match, default trial is used
5. Trial is resolved from service provider
6. Method is invoked on trial instance

## Next Steps

- [Selection Modes](selection-modes.md) - Learn about the four selection strategies
- [Error Handling](error-handling.md) - Understand error policies and fallback behavior
- [Telemetry](telemetry.md) - Integrate observability into your experiments
