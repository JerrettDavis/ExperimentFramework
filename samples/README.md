# ExperimentFramework - Sample Applications

This directory contains runnable sample applications demonstrating all features of the ExperimentFramework library.

## Quick Start

```bash
# Run the comprehensive sample (shows all features)
cd samples/ExperimentFramework.ComprehensiveSample
dotnet run

# Run the console sample (basic usage)
cd samples/ExperimentFramework.SampleConsole
dotnet run

# Run the web API sample (sticky routing in web apps)
cd samples/ExperimentFramework.SampleWebApp
dotnet run
```

## Sample Projects

### 1. ExperimentFramework.ComprehensiveSample ⭐ **Recommended Starting Point**

**Location:** `samples/ExperimentFramework.ComprehensiveSample/`

A comprehensive console application demonstrating **all** features of ExperimentFramework in a single runnable sample.

**Demonstrates:**
- ✅ All 5 error policies (Throw, RedirectAndReplayDefault, RedirectAndReplayAny, RedirectAndReplay, RedirectAndReplayOrdered)
- ✅ All 4 selection modes (BooleanFeatureFlag, Configuration, VariantFeatureFlag, StickyRouting)
- ✅ All 5 return types (void, Task, Task<T>, ValueTask, ValueTask<T>)
- ✅ Custom decorators (timing, caching, custom logging)
- ✅ OpenTelemetry distributed tracing integration
- ✅ Variant feature flags (multi-variant A/B/C testing)
- ✅ Source generator usage with `[ExperimentCompositionRoot]` attribute

**Run it:**
```bash
cd samples/ExperimentFramework.ComprehensiveSample
dotnet run
```

**Output:** Clear console output showing each feature in action with detailed explanations.

---

### 2. ExperimentFramework.SampleConsole

**Location:** `samples/ExperimentFramework.SampleConsole/`

A basic console application demonstrating core features with minimal complexity.

**Demonstrates:**
- Boolean feature flag selection (true/false routing)
- Configuration-based selection (multi-variant routing)
- Error policy: RedirectAndReplayDefault
- Error policy: RedirectAndReplayAny
- Built-in decorators (benchmarks, error logging)
- `[ExperimentCompositionRoot]` attribute trigger

**Experiments:**
1. **IMyDatabase** - Routes between local DB and cloud DB based on feature flag
2. **IMyTaxProvider** - Routes between tax providers based on configuration value

**Run it:**
```bash
cd samples/ExperimentFramework.SampleConsole
dotnet run
```

---

### 3. ExperimentFramework.SampleWebApp

**Location:** `samples/ExperimentFramework.SampleWebApp/`

An ASP.NET Core Web API demonstrating sticky routing for consistent user experiences.

**Demonstrates:**
- Sticky routing (hash-based deterministic A/B testing) - requires `ExperimentFramework.StickyRouting` package
- Session-based identity provider
- Boolean feature flag selection
- `.UseSourceGenerators()` fluent API trigger
- Web application integration patterns

**Experiments:**
1. **IRecommendationEngine** - Sticky routing ensures same user always sees same algorithm
2. **ICheckoutFlow** - Feature flag toggles between standard and express checkout

**Run it:**
```bash
cd samples/ExperimentFramework.SampleWebApp
dotnet run
```

**Try it:**
```bash
# Get recommendations (sticky to your session)
curl http://localhost:5000/api/recommendations

# See which algorithm you're assigned
curl http://localhost:5000/api/recommendations/algorithm

# Get checkout flow
curl http://localhost:5000/api/checkout/flow
```

---

## Feature Coverage Matrix

| Feature | Comprehensive | Console | WebApp |
|---------|:-------------:|:-------:|:------:|
| **Selection Modes** | | | |
| Boolean Feature Flag | ✅ | ✅ | ✅ |
| Configuration | ✅ | ✅ | |
| Variant Feature Flag | ✅ | | |
| Sticky Routing | ✅ | | ✅ |
| **Error Policies** | | | |
| Throw | ✅ | | |
| RedirectAndReplayDefault | ✅ | ✅ | ✅ |
| RedirectAndReplayAny | ✅ | ✅ | |
| RedirectAndReplay | ✅ | | |
| RedirectAndReplayOrdered | ✅ | | |
| **Return Types** | | | |
| void | ✅ | | |
| Task | ✅ | ✅ | ✅ |
| Task\<T> | ✅ | ✅ | ✅ |
| ValueTask | ✅ | | |
| ValueTask\<T> | ✅ | | |
| **Advanced Features** | | | |
| Custom Decorators | ✅ | | |
| OpenTelemetry | ✅ | | |
| Built-in Decorators | | ✅ | |
| Session Identity Provider | | | ✅ |
| **Triggers** | | | |
| [ExperimentCompositionRoot] | ✅ | ✅ | |
| .UseSourceGenerators() | | | ✅ |

---

## Understanding Error Policies

Error policies control what happens when a selected trial throws an exception.

### 1. OnErrorThrow (Fail Fast)

```csharp
.Trial<IService>(t => t
    .UsingFeatureFlag("EnableNewVersion")
    .AddControl<OldVersion>()
    .AddCondition<NewVersion>("true")
    .OnErrorThrow()) // ← Throws immediately if condition fails
```

**Behavior:**
- If selected condition throws → Exception propagates immediately
- No fallback attempts
- **Use when:** You want failures to be visible immediately (critical systems)

**See:** `ComprehensiveSample → Demo 1.1`

---

### 2. OnErrorFallbackToControl (Safe Fallback)

```csharp
.Trial<IService>(t => t
    .UsingFeatureFlag("EnableExperiment")
    .AddControl<Stable>()  // ← Fallback
    .AddCondition<Experimental>("true")
    .OnErrorFallbackToControl()) // ← Falls back to control
```

**Behavior:**
- Tries: `[preferred condition, control]`
- If preferred fails → Falls back to control
- If control also fails → Exception propagates
- **Use when:** You have a safe, stable control implementation

**See:** `ComprehensiveSample → Demo 1.2`, `Console → IMyDatabase`

---

### 3. OnErrorTryAny (Resilience)

```csharp
.Trial<IService>(t => t
    .UsingConfig("Experiments:Provider")
    .AddControl<ProviderC>()
    .AddVariant<ProviderA>("a")
    .AddVariant<ProviderB>("b")
    .OnErrorTryAny()) // ← Tries all conditions
```

**Behavior:**
- Tries: `[preferred condition, all other conditions]`
- Continues until one succeeds
- Only throws if ALL conditions fail
- **Use when:** You need maximum resilience (multiple fallbacks)

**See:** `ComprehensiveSample → Demo 1.3`, `Console → IMyTaxProvider`

---

### 4. OnErrorFallbackTo (Specific Fallback)

```csharp
.Trial<IService>(t => t
    .UsingFeatureFlag("EnablePrimaryImplementation")
    .AddControl<PrimaryImpl>()
    .AddCondition<SecondaryImpl>("secondary")
    .AddCondition<NoopHandler>("noop")
    .OnErrorFallbackTo("noop")) // ← Specific fallback condition
```

**Behavior:**
- Tries: `[preferred condition, specific fallback condition]`
- If preferred fails → Redirects to specified condition (e.g., "noop")
- If specific fallback also fails → Exception propagates
- **Use when:** You want to redirect failures to a dedicated diagnostics/Noop handler

**Example use cases:**
- Safe-mode handlers that always succeed
- Diagnostic handlers that log failures and return safe defaults
- Circuit breaker patterns with fallback implementations

**See:** `ComprehensiveSample → Demo 1.4`

---

### 5. OnErrorTryInOrder (Custom Fallback Chain)

```csharp
.Trial<IService>(t => t
    .UsingFeatureFlag("UseCloudDatabase")
    .AddControl<CloudDb>()
    .AddCondition<LocalCache>("cache")
    .AddCondition<InMemoryCache>("memory")
    .AddCondition<StaticData>("static")
    .OnErrorTryInOrder("cache", "memory", "static"))
```

**Behavior:**
- Tries: `[preferred condition, fallback1, fallback2, fallback3, ...]`
- Tries each fallback in the exact order specified
- Only throws if ALL conditions fail
- **Use when:** You need fine-grained control over fallback order

**Example use cases:**
- Tiered data sources: Cloud → Local Cache → In-Memory → Static
- Payment providers with priority order
- Multi-region failover with specific region ordering

**See:** `ComprehensiveSample → Demo 1.5`

---

## Understanding Selection Modes

Selection modes determine how the framework chooses which trial to execute.

### 1. Boolean Feature Flag

Uses `IFeatureManager` to select based on true/false.

```csharp
.UsingFeatureFlag("EnableCloudDatabase")
.AddControl<LocalDb>()
.AddCondition<CloudDb>("true")
```

**Configuration (appsettings.json):**
```json
{
  "FeatureManagement": {
    "EnableCloudDatabase": true
  }
}
```

**See:** `ComprehensiveSample → Demo 1`, `Console → IMyDatabase`, `WebApp → ICheckoutFlow`

---

### 2. Configuration Value

Uses `IConfiguration` to select based on configuration value.

```csharp
.UsingConfigurationKey("Experiments:SearchAlgorithm")
.AddControl<BasicSearch>()
.AddVariant<AdvancedSearch>("advanced")
.AddVariant<AISearch>("ai")
```

**Configuration (appsettings.json):**
```json
{
  "Experiments": {
    "SearchAlgorithm": "advanced"
  }
}
```

**See:** `ComprehensiveSample → Demo 5`, `Console → IMyTaxProvider`

---

### 3. Variant Feature Flag (Multi-Variant)

Uses Microsoft.FeatureManagement variants for A/B/C testing with weighted distribution.

> **Package Required:** `ExperimentFramework.FeatureManagement`

```bash
dotnet add package ExperimentFramework.FeatureManagement
```

```csharp
// Register the provider
services.AddExperimentVariantFeatureFlags();

// Configure experiment
.UsingVariantFeatureFlag("PaymentProviderVariant")
.AddControl<Stripe>()
.AddCondition<PayPal>("paypal")
.AddCondition<Square>("square")
```

**Configuration (appsettings.json):**
```json
{
  "FeatureManagement": {
    "PaymentProviderVariant": {
      "EnabledFor": [...],
      "Variants": [
        { "Name": "stripe", "Weight": 40 },
        { "Name": "paypal", "Weight": 40 },
        { "Name": "square", "Weight": 20 }
      ]
    }
  }
}
```

**See:** `ComprehensiveSample → Demo 4`

---

### 4. Sticky Routing (Deterministic A/B)

Uses hash-based routing to ensure same user always sees same variant.

> **Package Required:** `ExperimentFramework.StickyRouting`

```bash
dotnet add package ExperimentFramework.StickyRouting
```

```csharp
// Register the provider
services.AddExperimentStickyRouting();

// Configure experiment
.UsingStickyRouting()
.AddControl<AlgorithmA>()
.AddCondition<AlgorithmB>("variant-b")
.AddCondition<AlgorithmC>("variant-c")
```

**Also requires:** `IExperimentIdentityProvider` implementation

```csharp
using ExperimentFramework.StickyRouting;

public class SessionIdentityProvider : IExperimentIdentityProvider
{
    public bool TryGetIdentity(out string identity)
    {
        identity = HttpContext.Session.GetString("UserId");
        return !string.IsNullOrEmpty(identity);
    }
}

// Register identity provider
services.AddScoped<IExperimentIdentityProvider, SessionIdentityProvider>();
```

**See:** `ComprehensiveSample`, `WebApp → IRecommendationEngine`

---

## Custom Decorators

Decorators add cross-cutting concerns to experiment invocations.

### Built-in Decorators

```csharp
.AddLogger(l => l
    .AddBenchmarks()      // Performance timing
    .AddErrorLogging())   // Exception logging
```

### Custom Decorators

```csharp
public class TimingDecorator : IExperimentDecorator
{
    public int Order => 1;

    public async ValueTask<object?> InvokeAsync(
        InvocationContext context,
        Func<ValueTask<object?>> next)
    {
        var sw = Stopwatch.StartNew();
        var result = await next();
        sw.Stop();
        Console.WriteLine($"Took {sw.ElapsedMilliseconds}ms");
        return result;
    }
}
```

**Usage:**
```csharp
.Trial<IService>(t => t
    ...
    .AddDecorator<TimingDecoratorFactory>())
```

**See:** `ComprehensiveSample → Demo 2` for timing, caching, and logging decorators

---

## OpenTelemetry Integration

Automatic distributed tracing integration with OpenTelemetry.

**Setup:**
```csharp
// 1. Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("ExperimentFramework")
        .AddConsoleExporter());

// 2. Register telemetry implementation
builder.Services.AddSingleton<IExperimentTelemetry, OpenTelemetryExperimentTelemetry>();
```

**What gets tracked:**
- Service type and method name
- Selected trial key
- Success/failure status
- Execution duration
- Exception details (if any)

**See:** `ComprehensiveSample → Demo 3`

---

## Source Generator Triggers

Two ways to trigger source generation:

### 1. [ExperimentCompositionRoot] Attribute

```csharp
[ExperimentCompositionRoot]
public static ExperimentFrameworkBuilder ConfigureExperiments()
{
    return ExperimentFrameworkBuilder.Create()
        .Trial<IService>(...);
}
```

**Used in:** `ComprehensiveSample`, `Console`

---

### 2. .UseSourceGenerators() Fluent API

```csharp
public static ExperimentFrameworkBuilder ConfigureExperiments()
{
    return ExperimentFrameworkBuilder.Create()
        .Trial<IService>(...)
        .UseSourceGenerators(); // ← Trigger
}
```

**Used in:** `WebApp`

---

## Running the Samples

### Prerequisites

- .NET 10.0 SDK or later
- Visual Studio 2022, VS Code, or Rider (optional)

### Build All Samples

```bash
dotnet build
```

### Run Individual Samples

```bash
# Comprehensive sample (recommended)
dotnet run --project samples/ExperimentFramework.ComprehensiveSample

# Console sample
dotnet run --project samples/ExperimentFramework.SampleConsole

# Web API sample
dotnet run --project samples/ExperimentFramework.SampleWebApp
```

### Run Tests

```bash
dotnet test
```

---

## Next Steps

1. **Start with:** `ExperimentFramework.ComprehensiveSample` to see all features
2. **Learn from:** `ExperimentFramework.SampleConsole` for basic usage patterns
3. **Build web apps:** Use `ExperimentFramework.SampleWebApp` as a template for ASP.NET Core
4. **Customize:** Create your own experiments based on these examples

---

## Additional Resources

- **Main README:** `../README.md` - Project overview and getting started
- **API Documentation:** See XML documentation in source code
- **Benchmarks:** `../benchmarks/` - Performance comparisons
- **Tests:** `../tests/` - Comprehensive test suite showing all scenarios

---

## Need Help?

- 📖 Check the main README.md
- 🐛 Report issues on GitHub
- 💡 Request features via GitHub Issues
- 📧 Contact the maintainers

---

**Happy Experimenting! 🧪**
