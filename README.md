# ExperimentFramework

A .NET framework for runtime-switchable A/B testing, feature flags, trial fallback, and comprehensive observability.

**Version 0.1.0** - Production-ready with source-generated zero-overhead proxies

## Key Features

**Multiple Selection Modes**
- Boolean feature flags (`true`/`false` keys)
- Configuration values (string variants)
- Variant feature flags (IVariantFeatureManager integration)
- Sticky routing (deterministic user/session-based A/B testing)

**Enterprise Observability**
- OpenTelemetry distributed tracing support
- Built-in benchmarking and error logging
- Zero overhead when telemetry disabled

**Flexible Configuration**
- Custom naming conventions
- Error policies with fallback strategies
- Decorator pipeline for cross-cutting concerns

**Type-Safe & DI-Friendly**
- Composition-root driven registration
- Full dependency injection integration
- Strongly-typed builder API

## Quick Start

### 1. Register Services

```csharp
// Register concrete implementations
builder.Services.AddScoped<MyDbContext>();
builder.Services.AddScoped<MyCloudDbContext>();

// Register interface with default implementation
builder.Services.AddScoped<IMyDatabase, MyDbContext>();
```

### 2. Configure Experiments

```csharp
[ExperimentCompositionRoot]
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

### 3. Use Services Normally

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

## Error Policies

Control fallback behavior when trials fail:

```csharp
// Throw immediately on error (default)
.OnErrorRedirectAndReplayDefault()

// Fall back to default trial on error
.OnErrorRedirectAndReplayDefault()

// Try all trials until one succeeds
.OnErrorRedirectAndReplayAny()
```

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

### Source Generation
The framework uses Roslyn source generators to create optimized proxy classes at compile time:
1. The `[ExperimentCompositionRoot]` attribute triggers the generator
2. The generator analyzes `Define<T>()` calls to extract interface types
3. For each interface, a proxy class is generated implementing direct method calls
4. Generated proxies are discovered and registered automatically

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

- Trials **must be registered by concrete type** (ImplementationType) in DI. Factory/instance registrations are not supported.
- Source generation requires either `[ExperimentCompositionRoot]` attribute or `.UseSourceGenerators()` fluent API call.
- Generated proxies use direct method calls for zero-reflection overhead.
- Variant feature flag support requires reflection to access internal Microsoft.FeatureManagement APIs and may require updates for future versions.

## API Reference

### Builder Methods

| Method | Description |
|--------|-------------|
| `Create()` | Creates a new framework builder |
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
| `AddDefaultTrial<TImpl>(string)` | Registers default trial |
| `AddTrial<TImpl>(string)` | Registers additional trial |
| `OnErrorRedirectAndReplayDefault()` | Falls back to default on error |
| `OnErrorRedirectAndReplayAny()` | Tries all trials on error |

### Extension Methods

| Method | Description |
|--------|-------------|
| `AddExperimentFramework(ExperimentFrameworkBuilder)` | Registers framework in DI |
| `AddOpenTelemetryExperimentTracking()` | Enables OpenTelemetry tracing |

## License

[MIT](LICENSE)
