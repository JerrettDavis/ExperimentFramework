# Core Concepts

This guide explains the fundamental concepts of ExperimentFramework and how they work together to enable runtime experimentation.

## Conceptual Hierarchy

ExperimentFramework uses a clear hierarchy to organize experiments:

```
Experiment (named container, can span multiple interfaces)
├── Trial (configuration for a single service interface)
│   ├── Control: baseline implementation (stable)
│   ├── Condition/Variant: alternative implementations
│   ├── SelectionRule: how trial activates (feature flags, time, predicates)
│   └── BehaviorRule: how trial behaves (error handling, timeouts)
```

### Experiments

An **Experiment** is a named container that groups related trials. Experiments can span multiple service interfaces when testing related changes together:

```csharp
ExperimentFrameworkBuilder.Create()
    .Experiment("q1-2025-cloud-migration", exp => exp
        .Trial<IDatabase>(t => t
            .UsingFeatureFlag("UseCloudDb")
            .AddControl<LocalDatabase>()
            .AddCondition<CloudDatabase>("cloud"))
        .Trial<ICache>(t => t
            .UsingConfigurationKey("Cache:Provider")
            .AddControl<InMemoryCache>()
            .AddCondition<RedisCache>("redis"))
        .ActiveFrom(DateTimeOffset.Parse("2025-01-01"))
        .ActiveUntil(DateTimeOffset.Parse("2025-03-31")));
```

### Trials

A **Trial** is the configuration for a single service interface. Each trial specifies:
- A **Control** (baseline) implementation
- One or more **Conditions** (experimental implementations)
- Selection and behavior rules

## Fluent DSL Terminology

ExperimentFramework provides multiple equivalent method names to create a natural, readable configuration DSL. Different teams use different terminology for experimentation, so the framework accommodates various conventions:

| Concept | Method Options | When to Use |
|---------|---------------|-------------|
| Baseline implementation | `AddControl<T>()`, `AddDefaultTrial<T>()` | Control is common in A/B testing; DefaultTrial fits traditional experiment language |
| Alternative implementation | `AddCondition<T>()`, `AddVariant<T>()`, `AddTrial<T>()` | Condition fits scientific experiments; Variant fits A/B testing; Trial is general-purpose |
| Experiment definition | `Trial<T>()`, `Define<T>()` | Trial emphasizes the experimental nature; Define is more generic |

These are **functionally equivalent**—choose whichever terminology reads most naturally for your team and scenario:

```csharp
// Scientific experiment style
.Trial<IDatabase>(t => t
    .AddControl<LocalDatabase>()
    .AddCondition<CloudDatabase>("cloud"))

// A/B testing style
.Trial<IPayment>(t => t
    .AddControl<StripePayment>()
    .AddVariant<PayPalPayment>("paypal")
    .AddVariant<SquarePayment>("square"))

// Traditional style
.Define<ICache>(t => t
    .AddDefaultTrial<MemoryCache>("default")
    .AddTrial<RedisCache>("redis"))
```

## Controls and Conditions

### Control (Baseline)

The **Control** is your stable, well-tested implementation. It's used when:
- The experiment is inactive (outside time bounds)
- Selection criteria evaluates to an unknown value
- Error fallback is triggered
- No configuration is provided

```csharp
.AddControl<LocalDatabase>()           // Uses key "control" by default
.AddControl<LocalDatabase>("stable")   // Custom key
```

### Conditions (Variants)

**Conditions** are alternative implementations being tested against the control. As described in the [Fluent DSL Terminology](#fluent-dsl-terminology) section, you can use `AddCondition<T>()`, `AddVariant<T>()`, or `AddTrial<T>()` interchangeably:

```csharp
// All equivalent ways to register alternatives
.AddCondition<CloudDatabase>("cloud")
.AddVariant<CloudDatabase>("cloud")
.AddTrial<CloudDatabase>("cloud")

// Multiple conditions in a single trial
.AddControl<StripePayment>()
.AddCondition<PayPalPayment>("paypal")
.AddCondition<CryptoPayment>("crypto")
.AddVariant<ApplePayPayment>("applepay")
```

### Implementation Registration

All implementations must be registered with the dependency injection container:

```csharp
services.AddScoped<LocalDatabase>();
services.AddScoped<CloudDatabase>();
services.AddScoped<RedisCache>();
```

The framework resolves implementations by their concrete type.

### Condition Keys

Each condition is identified by a unique string key. The key is what the selection logic returns to determine which implementation to use.

For boolean feature flags, keys are typically "true" and "false":

```csharp
.AddControl<LocalDatabase>()        // Uses key "control"
.AddCondition<CloudDatabase>("true") // Flag returns "true"
```

For configuration values or variants, keys can be any string:

```csharp
.AddControl<StripePayment>()
.AddCondition<PayPalPayment>("paypal")
.AddCondition<CryptoPayment>("crypto")
```

## Time-Based Activation

Experiments can be activated based on time bounds and predicates. When an experiment is inactive, the control implementation is used.

### Time Bounds

Schedule experiments to run during specific time periods:

```csharp
.Trial<IDatabase>(t => t
    .AddControl<LocalDatabase>()
    .AddCondition<CloudDatabase>("cloud")
    .ActiveFrom(DateTimeOffset.Parse("2025-01-01"))
    .ActiveUntil(DateTimeOffset.Parse("2025-03-31")));

// Or use ActiveDuring for both bounds at once
.ActiveDuring(
    start: DateTimeOffset.Parse("2025-01-01"),
    end: DateTimeOffset.Parse("2025-03-31"));
```

### Custom Predicates

Use predicates for dynamic activation based on runtime conditions:

```csharp
.Trial<IDatabase>(t => t
    .AddControl<LocalDatabase>()
    .AddCondition<CloudDatabase>("cloud")
    .ActiveWhen(sp =>
    {
        var env = sp.GetService<IHostEnvironment>();
        return env?.IsProduction() == true;
    }));
```

### Combining with Feature Flags

Time bounds and predicates work alongside selection modes. The selection mode (feature flag, configuration, etc.) only takes effect when the experiment is active:

```csharp
.Trial<IDatabase>(t => t
    .UsingFeatureFlag("UseCloudDb")
    .AddControl<LocalDatabase>()
    .AddCondition<CloudDatabase>("cloud")
    .ActiveFrom(DateTimeOffset.Parse("2025-01-01"))  // Only active from Jan 1
    .ActiveWhen(sp => sp.GetService<IEnv>()?.IsProduction == true));  // And in production
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

// Implementations can have different lifetimes
services.AddScoped<LocalDatabase>();      // Scoped
services.AddSingleton<CloudDatabase>();   // Singleton

// The proxy will be Scoped (matching the original)
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IDatabase>(t => t
        .UsingFeatureFlag("UseCloud")
        .AddControl<LocalDatabase>()
        .AddCondition<CloudDatabase>("true"));

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
    .Trial<IDatabase>(t => t
        .UsingFeatureFlag("UseCloud")
        .AddControl<LocalDatabase>()
        .AddCondition<CloudDatabase>("true"));
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
    .Trial<IDatabase>(t => t
        .UsingFeatureFlag("UseCloud")
        .AddControl<LocalDatabase>()
        .AddCondition<CloudDatabase>("true"));
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
    .Trial<IDatabase>(t => t
        .UsingFeatureFlag("UseCloud")
        .AddControl<LocalDatabase>()
        .AddCondition<CloudDatabase>("true"));

// 5. Register the experiment framework
services.AddExperimentFramework(experiments);
```

### What AddExperimentFramework Does

When you call `AddExperimentFramework()`, the framework:

1. Removes the existing registration for `IDatabase`
2. Keeps the concrete type registrations (`LocalDatabase`, `CloudDatabase`)
3. Adds a new registration for `IDatabase` that returns a proxy
4. Preserves the original service lifetime

### Implementation Resolution

When a condition is selected, its implementation is resolved from the service provider:

```csharp
// Inside the proxy
var implementation = serviceProvider.GetRequiredService<CloudDatabase>();
```

This means implementations receive their dependencies from DI normally:

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
2. Time-based activation is evaluated:
   - If outside time bounds or predicate returns false, use control
3. Selection mode determines the condition key:
   - Feature flag: Check if flag is enabled (built-in)
   - Configuration: Read configuration value (built-in)
   - Variant: Query variant feature manager (requires `ExperimentFramework.FeatureManagement` package)
   - Sticky routing: Hash user identity (requires `ExperimentFramework.StickyRouting` package)
   - OpenFeature: Evaluate via OpenFeature SDK (requires `ExperimentFramework.OpenFeature` package)
4. Key is matched to registered conditions
5. If no match, control is used
6. Implementation is resolved from service provider
7. Method is invoked on the implementation

## Next Steps

- [Selection Modes](selection-modes.md) - Learn about the selection strategies
- [Error Handling](error-handling.md) - Understand error policies and fallback behavior
- [Telemetry](telemetry.md) - Integrate observability into your experiments
- [Extensibility](extensibility.md) - Create custom selection mode providers
