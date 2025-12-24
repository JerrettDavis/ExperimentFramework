# ExperimentFramework

A .NET library for routing service calls through configurable trials based on feature flags, configuration values, or custom routing logic.

## Features

**Selection Modes**
- Boolean feature flags (`true`/`false` keys)
- Configuration values (string variants)
- Variant feature flags (IVariantFeatureManager integration)
- Sticky routing (deterministic user/session-based routing)
- OpenFeature (open standard for feature flag management)

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
        .Define<IMyDatabase>(c =>
            c.UsingFeatureFlag("UseCloudDb")
             .AddDefaultTrial<MyDbContext>("false")
             .AddTrial<MyCloudDbContext>("true")
             .OnErrorRedirectAndReplayDefault())
        .Define<IMyTaxProvider>(c =>
            c.UsingConfigurationKey("Experiments:TaxProvider")
             .AddDefaultTrial<DefaultTaxProvider>("")
             .AddTrial<OkTaxProvider>("OK")
             .AddTrial<TxTaxProvider>("TX")
             .OnErrorRedirectAndReplayAny());
}

var experiments = ConfigureExperiments();
builder.Services.AddExperimentFramework(experiments);
```

**Option B: Runtime Proxies (Flexible)**

```csharp
public static ExperimentFrameworkBuilder ConfigureExperiments()
{
    return ExperimentFrameworkBuilder.Create()
        .Define<IMyDatabase>(c =>
            c.UsingFeatureFlag("UseCloudDb")
             .AddDefaultTrial<MyDbContext>("false")
             .AddTrial<MyCloudDbContext>("true")
             .OnErrorRedirectAndReplayDefault())
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

### Boolean Feature Flag
Routes based on enabled/disabled state:
```csharp
c.UsingFeatureFlag("MyFeature")
 .AddDefaultTrial<DefaultImpl>("false")
 .AddTrial<ExperimentalImpl>("true")
```

### Configuration Value
Routes based on string configuration value:
```csharp
c.UsingConfigurationKey("Experiments:ServiceName")
 .AddDefaultTrial<ControlImpl>("")
 .AddTrial<VariantA>("A")
 .AddTrial<VariantB>("B")
```

### Variant Feature Flag
Routes based on IVariantFeatureManager (requires Microsoft.FeatureManagement package):
```csharp
c.UsingVariantFeatureFlag("MyVariantFeature")
 .AddDefaultTrial<ControlImpl>("control")
 .AddTrial<VariantA>("variant-a")
 .AddTrial<VariantB>("variant-b")
```

### Sticky Routing (A/B Testing)
Deterministic routing based on user/session identity:
```csharp
// 1. Implement identity provider
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

// 2. Register provider
builder.Services.AddScoped<IExperimentIdentityProvider, UserIdentityProvider>();

// 3. Configure sticky routing
c.UsingStickyRouting()
 .AddDefaultTrial<ControlImpl>("control")
 .AddTrial<VariantA>("a")
 .AddTrial<VariantB>("b")
```

### OpenFeature
Routes based on OpenFeature flag evaluation (works with any OpenFeature-compatible provider):
```csharp
// Install OpenFeature SDK and your preferred provider
// dotnet add package OpenFeature

// Configure provider
await Api.Instance.SetProviderAsync(new YourProvider());

// Configure experiment
c.UsingOpenFeature("payment-processor")
 .AddDefaultTrial<StripeProcessor>("stripe")
 .AddTrial<PayPalProcessor>("paypal")
 .AddTrial<SquareProcessor>("square")
```

See [OpenFeature Integration Guide](docs/user-guide/openfeature.md) for provider setup examples.

## Error Policies

Control fallback behavior when trials fail:

### 1. Throw (Default)
Exception propagates immediately, no retries:
```csharp
// No method call needed - Throw is the default policy
.Define<IMyService>(c => c
    .UsingFeatureFlag("MyFeature")
    .AddDefaultTrial<DefaultImpl>("false")
    .AddTrial<ExperimentalImpl>("true"))
// If ExperimentalImpl throws, exception propagates to caller
```

### 2. RedirectAndReplayDefault
Falls back to default trial on error:
```csharp
.Define<IMyService>(c => c
    .UsingFeatureFlag("MyFeature")
    .AddDefaultTrial<DefaultImpl>("false")
    .AddTrial<ExperimentalImpl>("true")
    .OnErrorRedirectAndReplayDefault())
// Tries: [preferred, default]
```

### 3. RedirectAndReplayAny
Tries all trials until one succeeds (sorted alphabetically):
```csharp
.Define<IMyService>(c => c
    .UsingConfigurationKey("ServiceVariant")
    .AddDefaultTrial<DefaultImpl>("")
    .AddTrial<VariantA>("a")
    .AddTrial<VariantB>("b")
    .OnErrorRedirectAndReplayAny())
// Tries all variants in sorted order until one succeeds
```

### 4. RedirectAndReplay
Redirects to a specific fallback trial (e.g., Noop diagnostics handler):
```csharp
.Define<IMyService>(c => c
    .UsingFeatureFlag("MyFeature")
    .AddDefaultTrial<PrimaryImpl>("true")
    .AddTrial<SecondaryImpl>("false")
    .AddTrial<NoopHandler>("noop")
    .OnErrorRedirectAndReplay("noop"))
// Tries: [preferred, specific_fallback]
// Useful for dedicated diagnostics/safe-mode handlers
```

### 5. RedirectAndReplayOrdered
Tries ordered list of fallback trials:
```csharp
.Define<IMyService>(c => c
    .UsingFeatureFlag("UseCloudDb")
    .AddDefaultTrial<CloudDbImpl>("true")
    .AddTrial<LocalCacheImpl>("cache")
    .AddTrial<InMemoryCacheImpl>("memory")
    .AddTrial<StaticDataImpl>("static")
    .OnErrorRedirectAndReplayOrdered("cache", "memory", "static"))
// Tries: [preferred, cache, memory, static] in exact order
// Fine-grained control over fallback strategy
```

## Timeout Enforcement

Prevent slow trials from degrading system performance:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .Define<IMyDatabase>(c => c
        .UsingFeatureFlag("UseCloudDb")
        .AddDefaultTrial<LocalDb>("false")
        .AddTrial<CloudDb>("true")
        .OnErrorRedirectAndReplayDefault())
    .WithTimeout(TimeSpan.FromSeconds(5), TimeoutAction.FallbackToDefault)
    .UseDispatchProxy();
```

**Actions:**
- `TimeoutAction.ThrowException` - Throw `TimeoutException` when trial exceeds timeout
- `TimeoutAction.FallbackToDefault` - Automatically fallback to default trial on timeout

See [Timeout Enforcement Guide](docs/user-guide/timeout-enforcement.md) for detailed examples.

## Circuit Breaker

Automatically disable failing trials using Polly:

```bash
dotnet add package ExperimentFramework.Resilience
```

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .Define<IMyService>(c => c
        .UsingFeatureFlag("UseNewService")
        .AddDefaultTrial<StableService>("false")
        .AddTrial<NewService>("true")
        .OnErrorRedirectAndReplayDefault())
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
    .Define<IMyService>(c => c.UsingFeatureFlag("MyFeature")...)
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
    .Define<IMyDatabase>(c => c.UsingFeatureFlag("UseCloudDb")...)
    .WithKillSwitch(killSwitch)
    .UseDispatchProxy();

// Emergency disable
killSwitch.DisableExperiment(typeof(IMyDatabase));
killSwitch.DisableTrial(typeof(IMyDatabase), "cloud");
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
    .Define<IMyService>(c => c.UsingFeatureFlag() /* uses convention */)
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
2. The generator analyzes `Define<T>()` calls to extract interface types
3. For each interface, a proxy class is generated implementing direct method calls
4. Generated proxies are discovered and registered automatically

Performance: <100ns overhead per method call (near-zero reflection overhead)

**2. Runtime Proxies (Alternative)**

Uses `System.Reflection.DispatchProxy` for dynamic proxies:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .Define<IMyDatabase>(c => c.UsingFeatureFlag("UseCloudDb")...)
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
2. Concrete types remain registered (for trial resolution)
3. Interfaces are re-registered with source-generated proxy factories
4. All proxies are registered as singletons and create scopes internally per invocation

### Request-Scoped Consistency
Uses `IFeatureManagerSnapshot` (when available) to ensure consistent feature evaluation within a scope/request.

### Decorator Pipeline
Decorators wrap invocations in registration order:
- First registered = outermost wrapper
- Last registered = closest to actual invocation

### Sticky Routing Algorithm
1. Sorts trial keys alphabetically (deterministic ordering)
2. Hashes: `SHA256("{identity}:{selectorName}")`
3. Maps hash to trial via modulo: `hashValue % trialCount`
4. Same identity always routes to same trial

## Architecture

```
User Code
    ↓
IMyDatabase (Proxy)
    ↓
┌─────────────────────────────┐
│  Telemetry Scope (Start)    │
├─────────────────────────────┤
│  Trial Selection             │
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
│  - Fallback to Default       │
│  - Try All Trials            │
├─────────────────────────────┤
│  Trial Invocation            │
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
- Trials **must be registered by concrete type** (ImplementationType) in DI. Factory/instance registrations are not supported.
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
| `Define<TService>(Action<ServiceExperimentBuilder<TService>>)` | Defines service experiment |

### Service Experiment Builder

| Method | Description |
|--------|-------------|
| `UsingFeatureFlag(string?)` | Boolean feature flag selection |
| `UsingConfigurationKey(string?)` | Configuration value selection |
| `UsingVariantFeatureFlag(string?)` | Variant feature manager selection |
| `UsingStickyRouting(string?)` | Sticky routing selection |
| `UsingOpenFeature(string?)` | OpenFeature flag selection |
| `AddDefaultTrial<TImpl>(string)` | Registers default trial |
| `AddTrial<TImpl>(string)` | Registers additional trial |
| `OnErrorRedirectAndReplayDefault()` | Falls back to default on error |
| `OnErrorRedirectAndReplayAny()` | Tries all trials on error |
| `OnErrorRedirectAndReplay(string)` | Redirects to specific fallback trial on error |
| `OnErrorRedirectAndReplayOrdered(params string[])` | Tries ordered list of fallback trials on error |

### Extension Methods

| Method | Description |
|--------|-------------|
| `AddExperimentFramework(ExperimentFrameworkBuilder)` | Registers framework in DI |
| `AddOpenTelemetryExperimentTracking()` | Enables OpenTelemetry tracing |

## License

[MIT](LICENSE)
