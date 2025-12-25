# ExperimentFramework

A .NET library for routing service calls through configurable trials based on feature flags, configuration values, or custom routing logic.

## Fluent DSL Design

ExperimentFramework provides multiple equivalent method names to create a natural, readable configuration DSL. This allows you to describe experiments using terminology that best fits your mental model:

```csharp
// Scientific terminology: Control vs Conditions
.AddControl<BaselineImpl>()
.AddCondition<ExperimentalImpl>("experiment")

// A/B testing terminology: Control vs Variants
.AddControl<ControlImpl>()
.AddVariant<VariantA>("a")
.AddVariant<VariantB>("b")

// Legacy/Default terminology
.AddDefaultTrial<DefaultImpl>("default")
.AddTrial<AlternativeImpl>("alt")
```

All of these are functionally equivalent—use whichever reads most naturally for your scenario. The same applies to `Trial<T>()` and `Define<T>()` at the builder level.

## Features

**Selection Modes**
- Boolean feature flags (`true`/`false` keys) - built-in
- Configuration values (string variants) - built-in
- Custom/extensible modes via provider architecture
- **Optional packages:**
  - `ExperimentFramework.FeatureManagement` - Variant feature flags (IVariantFeatureManager)
  - `ExperimentFramework.StickyRouting` - Deterministic user/session-based routing
  - `ExperimentFramework.OpenFeature` - OpenFeature SDK integration

**Resilience**
- Timeout enforcement with fallback
- Circuit breaker (Polly integration)
- Kill switch for disabling experiments at runtime

**Observability**
- OpenTelemetry tracing
- Metrics collection (Prometheus, OpenTelemetry)
- Built-in benchmarking and error logging

**Configuration**
- Error policies with fallback strategies
- Custom naming conventions
- Decorator pipeline for cross-cutting concerns
- Dependency injection integration

## Quick Start

### 1. Install Packages

```bash
dotnet add package ExperimentFramework
dotnet add package ExperimentFramework.Generators  # For source-generated proxies
# OR use runtime proxies (no generator package needed)
```

### 2. Register Services

```csharp
// Register concrete implementations
builder.Services.AddScoped<MyDbContext>();
builder.Services.AddScoped<MyCloudDbContext>();

// Register interface with default implementation
builder.Services.AddScoped<IMyDatabase, MyDbContext>();
```

### 3. Configure Experiments

**Option A: Source-Generated Proxies (Recommended - Fast)**

```csharp
[ExperimentCompositionRoot]  // Triggers source generation
public static ExperimentFrameworkBuilder ConfigureExperiments()
{
    return ExperimentFrameworkBuilder.Create()
        .AddLogger(l => l.AddBenchmarks().AddErrorLogging())
        .Trial<IMyDatabase>(t => t
            .UsingFeatureFlag("UseCloudDb")
            .AddControl<MyDbContext>()
            .AddCondition<MyCloudDbContext>("true")
            .OnErrorFallbackToControl())
        .Trial<IMyTaxProvider>(t => t
            .UsingConfigurationKey("Experiments:TaxProvider")
            .AddControl<DefaultTaxProvider>()
            .AddVariant<OkTaxProvider>("OK")
            .AddVariant<TxTaxProvider>("TX")
            .OnErrorTryAny());
}

var experiments = ConfigureExperiments();
builder.Services.AddExperimentFramework(experiments);
```

**Option B: Runtime Proxies (Flexible)**

```csharp
public static ExperimentFrameworkBuilder ConfigureExperiments()
{
    return ExperimentFrameworkBuilder.Create()
        .Trial<IMyDatabase>(t => t
            .UsingFeatureFlag("UseCloudDb")
            .AddControl<MyDbContext>()
            .AddCondition<MyCloudDbContext>("true")
            .OnErrorFallbackToControl())
        .UseDispatchProxy();  // Use runtime proxies instead
}

var experiments = ConfigureExperiments();
builder.Services.AddExperimentFramework(experiments);
```

### 4. Use Services Normally

```csharp
public class MyService
{
    private readonly IMyDatabase _db;

    public MyService(IMyDatabase db) => _db = db;

    public async Task DoWork()
    {
        // Framework automatically routes to correct implementation
        var data = await _db.GetDataAsync();
    }
}
```

## Selection Modes

### Built-in Modes

#### Boolean Feature Flag
Routes based on enabled/disabled state:
```csharp
t.UsingFeatureFlag("MyFeature")
 .AddControl<DefaultImpl>()
 .AddCondition<ExperimentalImpl>("true")
```

#### Configuration Value
Routes based on string configuration value:
```csharp
t.UsingConfigurationKey("Experiments:ServiceName")
 .AddControl<ControlImpl>()
 .AddVariant<VariantA>("A")
 .AddVariant<VariantB>("B")
```

### Extension Packages

The framework supports additional selection modes via optional packages. Install only what you need.

#### Variant Feature Flag (ExperimentFramework.FeatureManagement)

Routes based on IVariantFeatureManager (Microsoft.FeatureManagement):

```bash
dotnet add package ExperimentFramework.FeatureManagement
```

```csharp
// Register the provider
services.AddExperimentVariantFeatureFlags();
services.AddFeatureManagement();

// Configure experiment
t.UsingVariantFeatureFlag("MyVariantFeature")
 .AddControl<ControlImpl>()
 .AddCondition<VariantA>("variant-a")
 .AddCondition<VariantB>("variant-b")
```

#### Sticky Routing (ExperimentFramework.StickyRouting)

Deterministic routing based on user/session identity:

```bash
dotnet add package ExperimentFramework.StickyRouting
```

```csharp
// 1. Register the provider
services.AddExperimentStickyRouting();

// 2. Implement and register identity provider
public class UserIdentityProvider : IExperimentIdentityProvider
{
    private readonly IHttpContextAccessor _accessor;
    public UserIdentityProvider(IHttpContextAccessor accessor) => _accessor = accessor;

    public bool TryGetIdentity(out string identity)
    {
        identity = _accessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        return !string.IsNullOrEmpty(identity);
    }
}
services.AddScoped<IExperimentIdentityProvider, UserIdentityProvider>();

// 3. Configure sticky routing
t.UsingStickyRouting()
 .AddControl<ControlImpl>()
 .AddCondition<VariantA>("a")
 .AddCondition<VariantB>("b")
```

#### OpenFeature (ExperimentFramework.OpenFeature)

Routes based on OpenFeature flag evaluation:

```bash
dotnet add package ExperimentFramework.OpenFeature
dotnet add package OpenFeature
```

```csharp
// Register the provider
services.AddExperimentOpenFeature();

// Configure OpenFeature provider
await Api.Instance.SetProviderAsync(new YourProvider());

// Configure experiment
t.UsingOpenFeature("payment-processor")
 .AddControl<StripeProcessor>()
 .AddCondition<PayPalProcessor>("paypal")
 .AddCondition<SquareProcessor>("square")
```

See [OpenFeature Integration Guide](docs/user-guide/openfeature.md) for provider setup examples.

### Custom Selection Modes

Create your own selection modes with minimal boilerplate using the `[SelectionMode]` attribute:

```csharp
// 1. Create your provider (just one class!)
[SelectionMode("Redis")]
public class RedisSelectionProvider : SelectionModeProviderBase
{
    private readonly IConnectionMultiplexer _redis;

    public RedisSelectionProvider(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public override async ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
    {
        var value = await _redis.GetDatabase().StringGetAsync(context.SelectorName);
        return value.HasValue ? value.ToString() : null;
    }
}

// 2. Register it (one line!)
services.AddSelectionModeProvider<RedisSelectionProvider>();

// 3. Use it
t.UsingCustomMode("Redis", "cache:provider")
 .AddControl<MemoryCache>()
 .AddCondition<RedisCache>("redis")
```

No factory classes needed! See [Extensibility Guide](docs/user-guide/extensibility.md) for details.

## Error Policies

Control fallback behavior when conditions fail:

### 1. Throw (Default)
Exception propagates immediately, no retries:
```csharp
// No method call needed - Throw is the default policy
.Trial<IMyService>(t => t
    .UsingFeatureFlag("MyFeature")
    .AddControl<DefaultImpl>()
    .AddCondition<ExperimentalImpl>("true"))
// If ExperimentalImpl throws, exception propagates to caller
```

### 2. FallbackToControl
Falls back to control on error:
```csharp
.Trial<IMyService>(t => t
    .UsingFeatureFlag("MyFeature")
    .AddControl<DefaultImpl>()
    .AddCondition<ExperimentalImpl>("true")
    .OnErrorFallbackToControl())
// Tries: [preferred, control]
```

### 3. TryAny
Tries all conditions until one succeeds (sorted alphabetically):
```csharp
.Trial<IMyService>(t => t
    .UsingConfigurationKey("ServiceVariant")
    .AddControl<DefaultImpl>()
    .AddVariant<VariantA>("a")
    .AddVariant<VariantB>("b")
    .OnErrorTryAny())
// Tries all variants in sorted order until one succeeds
```

### 4. FallbackTo
Redirects to a specific fallback condition (e.g., Noop diagnostics handler):
```csharp
.Trial<IMyService>(t => t
    .UsingFeatureFlag("MyFeature")
    .AddControl<PrimaryImpl>()
    .AddCondition<SecondaryImpl>("secondary")
    .AddCondition<NoopHandler>("noop")
    .OnErrorFallbackTo("noop"))
// Tries: [preferred, specific_fallback]
// Useful for dedicated diagnostics/safe-mode handlers
```

### 5. TryInOrder
Tries ordered list of fallback conditions:
```csharp
.Trial<IMyService>(t => t
    .UsingFeatureFlag("UseCloudDb")
    .AddControl<CloudDbImpl>()
    .AddCondition<LocalCacheImpl>("cache")
    .AddCondition<InMemoryCacheImpl>("memory")
    .AddCondition<StaticDataImpl>("static")
    .OnErrorTryInOrder("cache", "memory", "static"))
// Tries: [preferred, cache, memory, static] in exact order
// Fine-grained control over fallback strategy
```

## Timeout Enforcement

Prevent slow conditions from degrading system performance:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IMyDatabase>(t => t
        .UsingFeatureFlag("UseCloudDb")
        .AddControl<LocalDb>()
        .AddCondition<CloudDb>("true")
        .OnErrorFallbackToControl())
    .WithTimeout(TimeSpan.FromSeconds(5), TimeoutAction.FallbackToDefault)
    .UseDispatchProxy();
```

**Actions:**
- `TimeoutAction.ThrowException` - Throw `TimeoutException` when condition exceeds timeout
- `TimeoutAction.FallbackToDefault` - Automatically fallback to control on timeout

See [Timeout Enforcement Guide](docs/user-guide/timeout-enforcement.md) for detailed examples.

## Circuit Breaker

Automatically disable failing trials using Polly:

```bash
dotnet add package ExperimentFramework.Resilience
```

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IMyService>(t => t
        .UsingFeatureFlag("UseNewService")
        .AddControl<StableService>()
        .AddCondition<NewService>("true")
        .OnErrorFallbackToControl())
    .WithCircuitBreaker(options =>
    {
        options.FailureRatioThreshold = 0.5;      // Open after 50% failure rate
        options.MinimumThroughput = 10;            // Need 10 calls to assess
        options.SamplingDuration = TimeSpan.FromSeconds(30);
        options.BreakDuration = TimeSpan.FromSeconds(60);
        options.OnCircuitOpen = CircuitBreakerAction.FallbackToDefault;
    })
    .UseDispatchProxy();
```

See [Circuit Breaker Guide](docs/user-guide/circuit-breaker.md) for advanced configuration.

## Metrics Collection

Track experiment performance with Prometheus or OpenTelemetry:

```bash
dotnet add package ExperimentFramework.Metrics.Exporters
```

```csharp
var prometheusMetrics = new PrometheusExperimentMetrics();
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IMyService>(t => t.UsingFeatureFlag("MyFeature")...)
    .WithMetrics(prometheusMetrics)
    .UseDispatchProxy();

app.MapGet("/metrics", () => prometheusMetrics.GeneratePrometheusOutput());
```

**Collected Metrics:**
- `experiment_invocations_total` (counter) - Total invocations per experiment/trial
- `experiment_duration_seconds` (histogram) - Duration of each invocation

See [Metrics Guide](docs/user-guide/metrics.md) for OpenTelemetry integration and Grafana dashboards.

## Kill Switch

Emergency shutdown for problematic experiments:

```csharp
var killSwitch = new InMemoryKillSwitchProvider();

var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IMyDatabase>(t => t.UsingFeatureFlag("UseCloudDb")...)
    .WithKillSwitch(killSwitch)
    .UseDispatchProxy();

// Emergency disable
killSwitch.DisableExperiment(typeof(IMyDatabase));
killSwitch.DisableCondition(typeof(IMyDatabase), "cloud");
```

See [Kill Switch Guide](docs/user-guide/kill-switch.md) for distributed scenarios with Redis.

## Custom Naming Conventions

Replace default selector naming:

```csharp
public class MyNamingConvention : IExperimentNamingConvention
{
    public string FeatureFlagNameFor(Type serviceType)
        => $"Features.{serviceType.Name}";

    public string VariantFlagNameFor(Type serviceType)
        => $"Variants.{serviceType.Name}";

    public string ConfigurationKeyFor(Type serviceType)
        => $"Experiments.{serviceType.Name}";
}

var experiments = ExperimentFrameworkBuilder.Create()
    .UseNamingConvention(new MyNamingConvention())
    .Trial<IMyService>(t => t.UsingFeatureFlag() /* uses convention */)
    // ...
```

## OpenTelemetry Integration

Enable distributed tracing for experiments:

```csharp
builder.Services.AddExperimentFramework(experiments);
builder.Services.AddOpenTelemetryExperimentTracking();
```

Emitted activity tags:
- `experiment.service` - Service type name
- `experiment.method` - Method name
- `experiment.selector` - Selector name (feature flag/config key)
- `experiment.trial.selected` - Initially selected trial key
- `experiment.trial.candidates` - All candidate trial keys
- `experiment.outcome` - `success` or `failure`
- `experiment.fallback` - Fallback trial key (if applicable)
- `experiment.variant` - Variant name (for variant mode)

## Configuration Example

### appsettings.json

```json
{
  "FeatureManagement": {
    "UseCloudDb": false,
    "MyVariantFeature": {
      "EnabledFor": [
        {
          "Name": "Microsoft.Targeting",
          "Parameters": {
            "Audience": {
              "Users": ["user1@example.com"],
              "Groups": [
                {
                  "Name": "Beta",
                  "RolloutPercentage": 50
                }
              ]
            }
          }
        }
      ],
      "Variants": [
        {
          "Name": "control",
          "ConfigurationValue": "control"
        },
        {
          "Name": "variant-a",
          "ConfigurationValue": "variant-a"
        },
        {
          "Name": "variant-b",
          "ConfigurationValue": "variant-b"
        }
      ]
    }
  },
  "Experiments": {
    "TaxProvider": ""
  }
}
```

## Running the Sample

From the repo root:

```bash
dotnet run --project samples/ExperimentFramework.SampleConsole
```

While it runs, edit `samples/ExperimentFramework.SampleConsole/appsettings.json`:

```json
{
  "FeatureManagement": { "UseCloudDb": true },
  "Experiments": { "TaxProvider": "OK" }
}
```

Because the JSON file is loaded with `reloadOnChange: true`, changes will be picked up during runtime.

## How It Works

### Proxy Generation

The framework supports two proxy modes:

**1. Source-Generated Proxies (Default, Recommended)**

Uses Roslyn source generators to create optimized proxy classes at compile time:
1. The `[ExperimentCompositionRoot]` attribute or `.UseSourceGenerators()` triggers the generator
2. The generator analyzes `Trial<T>()` calls to extract interface types
3. For each interface, a proxy class is generated implementing direct method calls
4. Generated proxies are discovered and registered automatically

Performance: <100ns overhead per method call (near-zero reflection overhead)

**2. Runtime Proxies (Alternative)**

Uses `System.Reflection.DispatchProxy` for dynamic proxies:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IMyDatabase>(t => t.UsingFeatureFlag("UseCloudDb")...)
    .UseDispatchProxy(); // Use runtime proxies instead of source generation

builder.Services.AddExperimentFramework(experiments);
```

Performance: ~800ns overhead per method call (reflection-based)

Use runtime proxies when:
- Source generators are not available in your build environment
- You need maximum debugging flexibility
- Performance overhead is acceptable for your use case

### DI Rewriting
When you call `AddExperimentFramework()`:
1. Existing interface registrations are removed
2. Concrete types remain registered (for condition resolution)
3. Interfaces are re-registered with source-generated proxy factories
4. All proxies are registered as singletons and create scopes internally per invocation

### Request-Scoped Consistency
Uses `IFeatureManagerSnapshot` (when available) to ensure consistent feature evaluation within a scope/request.

### Decorator Pipeline
Decorators wrap invocations in registration order:
- First registered = outermost wrapper
- Last registered = closest to actual invocation

### Sticky Routing Algorithm
1. Sorts condition keys alphabetically (deterministic ordering)
2. Hashes: `SHA256("{identity}:{selectorName}")`
3. Maps hash to condition via modulo: `hashValue % conditionCount`
4. Same identity always routes to same condition

## Architecture

```
User Code
    ↓
IMyDatabase (Proxy)
    ↓
┌─────────────────────────────┐
│  Telemetry Scope (Start)    │
├─────────────────────────────┤
│  Condition Selection         │
│  - Feature Flag              │
│  - Configuration             │
│  - Variant                   │
│  - Sticky Routing            │
│  - OpenFeature               │
├─────────────────────────────┤
│  Decorator Pipeline          │
│  - Benchmarks                │
│  - Error Logging             │
│  - Custom Decorators         │
├─────────────────────────────┤
│  Error Policy                │
│  - Throw                     │
│  - Fallback to Control       │
│  - Try All Conditions        │
├─────────────────────────────┤
│  Implementation Invocation   │
│  MyDbContext.GetDataAsync()  │
└─────────────────────────────┘
    ↓
Return Result + Telemetry
```

## Advanced Features

### Custom Decorators

Implement cross-cutting concerns:

```csharp
public class CachingDecoratorFactory : IExperimentDecoratorFactory
{
    public IExperimentDecorator Create(IServiceProvider sp)
        => new CachingDecorator(sp.GetRequiredService<IDistributedCache>());
}

public class CachingDecorator : IExperimentDecorator
{
    private readonly IDistributedCache _cache;

    public CachingDecorator(IDistributedCache cache) => _cache = cache;

    public async ValueTask<object?> InvokeAsync(
        InvocationContext ctx,
        Func<ValueTask<object?>> next)
    {
        var key = $"{ctx.ServiceType.Name}:{ctx.MethodName}:{ctx.TrialKey}";

        var cached = await _cache.GetStringAsync(key);
        if (cached != null)
            return JsonSerializer.Deserialize<object>(cached);

        var result = await next();
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(result));
        return result;
    }
}

// Register
var experiments = ExperimentFrameworkBuilder.Create()
    .AddDecoratorFactory(new CachingDecoratorFactory())
    // ...
```

### Multi-Tenant Experiments

Different experiments per tenant:

```csharp
public class TenantIdentityProvider : IExperimentIdentityProvider
{
    private readonly ITenantAccessor _tenantAccessor;

    public bool TryGetIdentity(out string identity)
    {
        identity = $"tenant:{_tenantAccessor.CurrentTenant?.Id ?? "default"}";
        return !string.IsNullOrEmpty(identity);
    }
}
```

## Performance

The framework uses compile-time source generation to create high-performance experiment proxies with direct method invocation.

### Benchmark Results

Run comprehensive performance benchmarks:

```bash
# Windows
.\run-benchmarks.ps1

# macOS/Linux
chmod +x run-benchmarks.sh
./run-benchmarks.sh
```

**Typical overhead** (measured on real hardware):
- **Raw proxy overhead**: ~3-5 μs per method call
- **I/O-bound operations** (5ms delay): < 0.1% overhead
- **CPU-bound operations** (hashing): < 1% overhead

### Key Insights

When methods perform actual work (database calls, API requests, computation), the proxy overhead becomes **negligible**:

```
Without proxy:  5.000 ms
With proxy:     5.003 ms  (0.06% overhead)
```

For high-throughput scenarios with ultra-low-latency requirements, consider:
- Using configuration values (faster than feature flag evaluation)
- Singleton service lifetimes when appropriate
- Batching operations to reduce per-call overhead

See [benchmarks README](benchmarks/ExperimentFramework.Benchmarks/README.md) for detailed analysis.

### Supported Scenarios

All async and generic scenarios validated with comprehensive tests:
- `Task<T>` and `ValueTask<T>` for any `T`
- Generic interfaces: `IRepository<T>`, `ICache<TKey, TValue>`
- Nested generics: `Task<Dictionary<string, List<Product>>>`

## Important Notes

- **Proxy Mode Selection**: You must choose between source-generated or runtime proxies:
  - Source-generated (recommended): Requires `ExperimentFramework.Generators` package + `[ExperimentCompositionRoot]` attribute or `.UseSourceGenerators()` call
  - Runtime (alternative): No extra package needed, just call `.UseDispatchProxy()` on the builder
- Implementations **must be registered by concrete type** (ImplementationType) in DI. Factory/instance registrations are not supported.
- Source-generated proxies use direct method calls for zero-reflection overhead (<100ns per call).
- Runtime proxies use `DispatchProxy` with reflection (~800ns per call).
- Variant feature flag support requires reflection to access internal Microsoft.FeatureManagement APIs and may require updates for future versions.

## API Reference

### Builder Methods

| Method | Description |
|--------|-------------|
| `Create()` | Creates a new framework builder |
| `UseSourceGenerators()` | Use compile-time source-generated proxies (<100ns overhead) |
| `UseDispatchProxy()` | Use runtime DispatchProxy-based proxies (~800ns overhead) |
| `UseNamingConvention(IExperimentNamingConvention)` | Sets custom naming convention |
| `AddLogger(Action<ExperimentLoggingBuilder>)` | Adds logging decorators |
| `AddDecoratorFactory(IExperimentDecoratorFactory)` | Adds custom decorator |
| `Trial<TService>(Action<ServiceExperimentBuilder<TService>>)` | Defines a trial for a service interface |
| `Experiment(string, Action<ExperimentBuilder>)` | Defines a named experiment with multiple trials |

### Service Trial Builder

| Method | Description |
|--------|-------------|
| `UsingFeatureFlag(string?)` | Boolean feature flag selection (built-in) |
| `UsingConfigurationKey(string?)` | Configuration value selection (built-in) |
| `UsingCustomMode(string, string?)` | Custom selection mode (for extension packages) |
| `AddControl<TImpl>()` | Registers the control (baseline) implementation |
| `AddDefaultTrial<TImpl>(string)` | Registers the control implementation (alternative terminology) |
| `AddCondition<TImpl>(string)` | Registers an experimental condition |
| `AddVariant<TImpl>(string)` | Registers an experimental variant (same as AddCondition) |
| `AddTrial<TImpl>(string)` | Registers an experimental trial (same as AddCondition) |
| `OnErrorFallbackToControl()` | Falls back to control on error |
| `OnErrorTryAny()` | Tries all conditions on error |
| `OnErrorFallbackTo(string)` | Redirects to specific fallback condition on error |
| `OnErrorTryInOrder(params string[])` | Tries ordered list of fallback conditions on error |
| `ActiveFrom(DateTimeOffset)` | Activates trial starting at specified time |
| `ActiveUntil(DateTimeOffset)` | Deactivates trial after specified time |
| `ActiveWhen(Func<IServiceProvider, bool>)` | Activates trial when predicate returns true |

### Extension Package Methods

| Package | Method | Description |
|---------|--------|-------------|
| `ExperimentFramework.FeatureManagement` | `UsingVariantFeatureFlag(string?)` | Variant feature manager selection |
| `ExperimentFramework.StickyRouting` | `UsingStickyRouting(string?)` | Identity-based sticky routing |
| `ExperimentFramework.OpenFeature` | `UsingOpenFeature(string?)` | OpenFeature flag selection |

### Extension Package Registration

| Package | Registration Method |
|---------|---------------------|
| `ExperimentFramework.FeatureManagement` | `services.AddExperimentVariantFeatureFlags()` |
| `ExperimentFramework.StickyRouting` | `services.AddExperimentStickyRouting()` |
| `ExperimentFramework.OpenFeature` | `services.AddExperimentOpenFeature()` |

### Extension Methods

| Method | Description |
|--------|-------------|
| `AddExperimentFramework(ExperimentFrameworkBuilder)` | Registers framework in DI |
| `AddOpenTelemetryExperimentTracking()` | Enables OpenTelemetry tracing |
| `AddSelectionModeProvider<TProvider>()` | Registers a custom selection mode provider |

## License

[MIT](LICENSE)
