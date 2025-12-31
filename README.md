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

**Scientific Experimentation**
- **Data Collection** (`ExperimentFramework.Data`)
  - Automatic outcome recording (binary, continuous, count, duration)
  - Thread-safe in-memory storage with aggregation
  - Decorator-based collection for zero-code integration
- **Statistical Analysis** (`ExperimentFramework.Science`)
  - Hypothesis testing (t-test, chi-square, Mann-Whitney U, ANOVA)
  - Effect size calculation (Cohen's d, odds ratio, relative risk)
  - Power analysis and sample size calculation
  - Multiple comparison corrections (Bonferroni, Holm, Benjamini-Hochberg)
  - Publication-ready reports (Markdown, JSON)

**Extensibility Features**
- **Plugin System** (`ExperimentFramework.Plugins`)
  - Dynamic assembly loading at runtime
  - Configurable isolation modes (Full, Shared, None)
  - Plugin manifests (JSON or assembly attributes)
  - Hot reload support with file watching
  - YAML DSL integration with `plugin:PluginId/alias` syntax

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

## YAML/JSON Configuration (NEW)

Define experiments declaratively without code changes using YAML or JSON files:

### 1. Install Configuration Package

```bash
dotnet add package ExperimentFramework.Configuration
```

### 2. Create experiments.yaml

```yaml
experimentFramework:
  settings:
    proxyStrategy: dispatchProxy

  decorators:
    - type: logging
      options:
        benchmarks: true
        errorLogging: true

  trials:
    - serviceType: IMyDatabase
      selectionMode:
        type: featureFlag
        flagName: UseCloudDb
      control:
        key: control
        implementationType: MyDbContext
      conditions:
        - key: "true"
          implementationType: MyCloudDbContext
      errorPolicy:
        type: fallbackToControl

  experiments:
    - name: checkout-optimization
      metadata:
        owner: platform-team
        ticket: PLAT-1234
      activation:
        from: "2025-01-01T00:00:00Z"
        until: "2025-03-31T23:59:59Z"
      trials:
        - serviceType: ICheckoutService
          selectionMode:
            type: stickyRouting
          control:
            key: legacy
            implementationType: LegacyCheckout
          conditions:
            - key: streamlined
              implementationType: StreamlinedCheckout
      hypothesis:
        name: checkout-conversion
        type: superiority
        nullHypothesis: "No difference in conversion rate"
        alternativeHypothesis: "Streamlined checkout improves conversion"
        primaryEndpoint:
          name: purchase_completed
          outcomeType: binary
          higherIsBetter: true
        expectedEffectSize: 0.05
        successCriteria:
          alpha: 0.05
          power: 0.80
```

### 3. Register from Configuration

```csharp
// Load experiment configuration from YAML files
builder.Services.AddExperimentFrameworkFromConfiguration(builder.Configuration);

// Or with options
builder.Services.AddExperimentFrameworkFromConfiguration(builder.Configuration, opts =>
{
    opts.ScanDefaultPaths = true;
    opts.EnableHotReload = true;
    opts.TypeAliases.Add("IMyDb", typeof(IMyDatabase));
});
```

### Features

- **Auto-discovery**: Scans `experiments.yaml`, `ExperimentDefinitions/**/*.yaml`, and appsettings.json
- **Type aliases**: Use simple names instead of assembly-qualified type names
- **Hot reload**: Configuration changes apply without restart
- **Validation**: Comprehensive validation with helpful error messages
- **Hybrid mode**: Combine programmatic and file-based configuration

### Selection Modes in YAML

| YAML Type | Fluent API Equivalent |
|-----------|----------------------|
| `featureFlag` | `.UsingFeatureFlag()` |
| `configurationKey` | `.UsingConfigurationKey()` |
| `variantFeatureFlag` | `.UsingVariantFeatureFlag()` |
| `stickyRouting` | `.UsingStickyRouting()` |
| `openFeature` | `.UsingOpenFeature()` |
| `custom` | `.UsingCustomMode()` |

See the [Configuration Guide](docs/user-guide/configuration.md) for complete documentation.

## Schema Stamping and Versioning (NEW)

Track and version configuration schemas with deterministic hashing for enterprise governance and safe migrations:

```bash
dotnet add package ExperimentFramework.Configuration
```

### Features

- **Deterministic Hashing**: Fast FNV-1a hashing for schema change detection
- **Automatic Versioning**: Versions increment only when schemas actually change
- **Per-Extension Schemas**: Each extension independently manages its schema
- **Unified Schema Documents**: Single artifact for the entire solution
- **Audit Trail**: Complete version history for compliance

### Example Usage

```csharp
using ExperimentFramework.Configuration.Schema;

// Compute deterministic schema hash
var schema = new SchemaDefinition
{
    Types = [ /* configuration types */ ]
};

var normalized = SchemaHasher.NormalizeSchema(schema);
var hash = SchemaHasher.ComputeHash(normalized);

// Track version changes
var tracker = new SchemaVersionTracker("schema-history.json");
var version = tracker.GetVersionForHash("MyExtension", hash);

// Same hash = same version, different hash = incremented version
tracker.SaveHistory();

// Generate unified schema for entire solution
var unifiedDoc = new UnifiedSchemaDocument
{
    Schemas = extensionSchemas,
    UnifiedHash = SchemaHasher.ComputeUnifiedHash(extensionSchemas.Values.Select(s => s.Metadata.SchemaHash))
};
```

### Use Cases

- **Migration Detection**: Automatically detect when configuration migrations are required
- **CI/CD Validation**: Block deployments with unapproved schema changes
- **Environment Compatibility**: Verify schema compatibility across environments
- **Audit Compliance**: Maintain complete schema evolution history

See the [Schema Stamping Guide](docs/user-guide/schema-stamping.md) for complete documentation.

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

## Scientific Experimentation

ExperimentFramework includes comprehensive scientific experimentation capabilities for running rigorous, reproducible experiments.

### Data Collection

Automatically record experiment outcomes for statistical analysis:

```bash
dotnet add package ExperimentFramework.Data
```

```csharp
// 1. Register data collection services
services.AddExperimentDataCollection();

// 2. Enable automatic outcome collection
var experiments = ExperimentFrameworkBuilder.Create()
    .WithOutcomeCollection(opts =>
    {
        opts.CollectDuration = true;
        opts.CollectErrors = true;
    })
    .Trial<ICheckout>(t => t
        .UsingFeatureFlag("NewCheckout")
        .AddControl<OldCheckout>()
        .AddCondition<NewCheckout>("true")
        .OnErrorFallbackToControl())
    .UseSourceGenerators();

// 3. Record custom outcomes
public class CheckoutService
{
    private readonly IOutcomeRecorder _recorder;

    public async Task<bool> CompleteCheckout(string userId)
    {
        var success = await ProcessPayment();

        // Record binary outcome (conversion)
        await _recorder.RecordBinaryAsync(
            experimentName: "checkout-test",
            trialKey: "new",
            subjectId: userId,
            metricName: "purchase_completed",
            success: success);

        return success;
    }
}
```

### Statistical Analysis

Perform rigorous statistical analysis on experiment data:

```bash
dotnet add package ExperimentFramework.Science
```

```csharp
// 1. Register science services
services.AddExperimentScience();

// 2. Define a hypothesis
var hypothesis = new HypothesisBuilder("checkout-conversion")
    .Superiority()
    .NullHypothesis("New checkout has no effect on conversion")
    .AlternativeHypothesis("New checkout improves conversion rate")
    .PrimaryEndpoint("purchase_completed", OutcomeType.Binary, ep => ep
        .Description("Purchase completion rate")
        .HigherIsBetter())
    .ExpectedEffectSize(0.05) // 5% improvement
    .WithSuccessCriteria(c => c
        .Alpha(0.05)
        .Power(0.80)
        .MinimumSampleSize(1000))
    .Build();

// 3. Analyze experiment
var analyzer = serviceProvider.GetRequiredService<IExperimentAnalyzer>();
var report = await analyzer.AnalyzeAsync("checkout-test", hypothesis);

// 4. Generate report
var reporter = new MarkdownReporter();
var markdown = await reporter.GenerateAsync(report);
Console.WriteLine(markdown);
```

### Statistical Tests Available

| Test | Use Case | Interface |
|------|----------|-----------|
| Welch's t-test | Compare means of two groups | `IStatisticalTest` |
| Paired t-test | Compare before/after measurements | `IPairedStatisticalTest` |
| Chi-square test | Compare proportions (binary outcomes) | `IStatisticalTest` |
| Mann-Whitney U | Non-parametric comparison | `IStatisticalTest` |
| One-way ANOVA | Compare 3+ groups | `IMultiGroupStatisticalTest` |

### Power Analysis

Calculate required sample sizes before running experiments:

```csharp
var powerAnalyzer = PowerAnalyzer.Instance;

// How many samples do I need?
var requiredN = powerAnalyzer.CalculateSampleSize(
    effectSize: 0.05,    // Expected 5% improvement
    power: 0.80,         // 80% power
    alpha: 0.05);        // 5% significance

// What power do I have with current samples?
var achievedPower = powerAnalyzer.CalculatePower(
    sampleSizePerGroup: 500,
    effectSize: 0.05,
    alpha: 0.05);

// What effect can I detect?
var mde = powerAnalyzer.CalculateMinimumDetectableEffect(
    sampleSizePerGroup: 500,
    power: 0.80,
    alpha: 0.05);
```

### Effect Size Calculators

Quantify the magnitude of treatment effects:

```csharp
// For continuous outcomes (Cohen's d)
var cohensD = CohensD.Instance.Calculate(controlData, treatmentData);
// d = 0.5 → Medium effect

// For binary outcomes (relative risk)
var rr = RelativeRisk.Instance.Calculate(
    controlSuccesses: 50, controlTotal: 200,
    treatmentSuccesses: 75, treatmentTotal: 200);
// RR = 1.5 → 50% relative improvement

// For binary outcomes (odds ratio)
var or = OddsRatio.Instance.Calculate(
    controlSuccesses: 50, controlTotal: 200,
    treatmentSuccesses: 75, treatmentTotal: 200);
```

### Multiple Comparison Corrections

When testing multiple hypotheses, apply corrections to control false discovery:

```csharp
var pValues = new double[] { 0.01, 0.02, 0.03, 0.04, 0.05 };

// Bonferroni (most conservative, controls FWER)
var bonferroni = BonferroniCorrection.Instance.AdjustPValues(pValues);

// Holm-Bonferroni (less conservative, controls FWER)
var holm = HolmBonferroniCorrection.Instance.AdjustPValues(pValues);

// Benjamini-Hochberg (controls FDR, more power)
var bh = BenjaminiHochbergCorrection.Instance.AdjustPValues(pValues);
```

### Example Report Output

```markdown
# Experiment Report: checkout-test

## Summary
| Property | Value |
|----------|-------|
| **Status** | ✅ Completed |
| **Conclusion** | 🏆 Treatment wins |
| **Total Samples** | 2,500 |

## Primary Analysis
**Test:** Chi-Square Test for Independence

| Statistic | Value |
|-----------|-------|
| Test Statistic | 12.5432 |
| p-value | < 0.001 |
| Significant | **Yes** |
| Point Estimate | 0.048 |
| 95% CI | [0.021, 0.075] |

## Effect Size
- **Measure:** Relative Risk
- **Value:** 1.24
- **Magnitude:** Small

## Recommendations
- Consider rolling out the treatment to all users.
```

See the [Scientific Analysis Guide](docs/user-guide/statistical-analysis.md) for detailed documentation.

## Plugin System (NEW)

Deploy experimental implementations as separate DLLs without rebuilding your main application.

### 1. Install Plugin Package

```bash
dotnet add package ExperimentFramework.Plugins
```

### 2. Configure Plugin Loading

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Add plugin support
builder.Services.AddExperimentPlugins(opts =>
{
    opts.DiscoveryPaths.Add("./plugins");
    opts.EnableHotReload = true;
    opts.DefaultIsolationMode = PluginIsolationMode.Shared;
});

// Register experiment framework
builder.Services.AddExperimentFrameworkFromConfiguration(builder.Configuration);
```

### 3. Reference Plugin Types in YAML

```yaml
experimentFramework:
  plugins:
    discovery:
      paths:
        - "./plugins"
    hotReload:
      enabled: true

  trials:
    - serviceType: IPaymentProcessor
      selectionMode:
        type: featureFlag
        flagName: PaymentExperiment
      control:
        key: control
        implementationType: DefaultProcessor
      conditions:
        - key: stripe-v2
          implementationType: plugin:Acme.Payments/stripe-v2
        - key: adyen
          implementationType: plugin:Acme.Payments/adyen
```

### 4. Create a Plugin

```csharp
// MyPlugin.csproj with EnableDynamicLoading=true
// plugin.manifest.json embedded as resource:
{
  "manifestVersion": "1.0",
  "plugin": {
    "id": "Acme.PaymentExperiments",
    "name": "Acme Payment Experiments",
    "version": "1.0.0"
  },
  "services": [{
    "interface": "IPaymentProcessor",
    "implementations": [
      { "type": "StripeV2Processor", "alias": "stripe-v2" }
    ]
  }]
}
```

### Isolation Modes

| Mode | Behavior | Use Case |
|------|----------|----------|
| `Full` | Separate AssemblyLoadContext | Untrusted plugins, version conflicts |
| `Shared` | Shares specified assemblies | Most common, allows DI integration |
| `None` | Loads into default context | Fully trusted, maximum compatibility |

See the [Plugin System Guide](docs/user-guide/plugins.md) for complete documentation.

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
| `ExperimentFramework.Plugins` | `services.AddExperimentPlugins()` |

### Extension Methods

| Method | Description |
|--------|-------------|
| `AddExperimentFramework(ExperimentFrameworkBuilder)` | Registers framework in DI |
| `AddOpenTelemetryExperimentTracking()` | Enables OpenTelemetry tracing |
| `AddSelectionModeProvider<TProvider>()` | Registers a custom selection mode provider |

### Data Collection Methods (ExperimentFramework.Data)

| Method | Description |
|--------|-------------|
| `services.AddExperimentDataCollection()` | Registers outcome storage and recording services |
| `services.AddExperimentDataCollection<TStore>()` | Registers with custom storage implementation |
| `services.AddExperimentDataCollectionNoop()` | Registers no-op storage (zero overhead) |
| `builder.WithOutcomeCollection()` | Enables automatic outcome collection via decorators |

### Science Methods (ExperimentFramework.Science)

| Method | Description |
|--------|-------------|
| `services.AddExperimentScience()` | Registers all statistical analysis services |
| `TwoSampleTTest.Instance.Perform()` | Welch's two-sample t-test |
| `PairedTTest.Instance.Perform()` | Paired samples t-test |
| `ChiSquareTest.Instance.Perform()` | Chi-square test for proportions |
| `MannWhitneyUTest.Instance.Perform()` | Mann-Whitney U (non-parametric) |
| `OneWayAnova.Instance.Perform()` | One-way ANOVA for 3+ groups |
| `PowerAnalyzer.Instance.CalculateSampleSize()` | Calculate required sample size |
| `PowerAnalyzer.Instance.CalculatePower()` | Calculate achieved power |
| `CohensD.Instance.Calculate()` | Cohen's d effect size |
| `OddsRatio.Instance.Calculate()` | Odds ratio for binary outcomes |
| `RelativeRisk.Instance.Calculate()` | Relative risk for binary outcomes |
| `BonferroniCorrection.Instance.AdjustPValues()` | Bonferroni p-value correction |
| `HolmBonferroniCorrection.Instance.AdjustPValues()` | Holm step-down correction |
| `BenjaminiHochbergCorrection.Instance.AdjustPValues()` | FDR correction |

### Plugin Methods (ExperimentFramework.Plugins)

| Method | Description |
|--------|-------------|
| `services.AddExperimentPlugins()` | Registers plugin system with default options |
| `services.AddExperimentPluginsWithHotReload()` | Registers with hot reload enabled |
| `pluginManager.LoadAsync(path)` | Load a plugin from a DLL path |
| `pluginManager.UnloadAsync(pluginId)` | Unload a loaded plugin |
| `pluginManager.ReloadAsync(pluginId)` | Reload a plugin (unload + load) |
| `pluginManager.GetLoadedPlugins()` | Get all currently loaded plugins |
| `pluginManager.ResolveType(reference)` | Resolve type from `plugin:Id/alias` reference |
| `pluginContext.GetTypeByAlias(alias)` | Get type by manifest alias |
| `pluginContext.CreateInstance(type, sp)` | Create instance with DI |

## License

[MIT](LICENSE)
