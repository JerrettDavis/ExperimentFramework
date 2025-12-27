# YAML/JSON Configuration

ExperimentFramework supports declarative experiment configuration via YAML or JSON files. This allows you to define experiments without code changes and enables configuration-driven deployments.

## Quick Start

### 1. Install the Configuration Package

```bash
dotnet add package ExperimentFramework.Configuration
```

### 2. Create a Configuration File

Create `experiments.yaml` in your project root:

```yaml
experimentFramework:
  settings:
    proxyStrategy: dispatchProxy

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
```

### 3. Register from Configuration

```csharp
builder.Services.AddExperimentFrameworkFromConfiguration(builder.Configuration);
```

That's it! The framework will automatically discover and load your experiment definitions.

---

## Configuration File Discovery

The framework scans for configuration files in the following order:

1. **appsettings.json** - `ExperimentFramework` section
2. **appsettings.{Environment}.json** - Environment-specific overrides
3. **experiments.yaml** or **experiments.yml** - Root directory
4. **ExperimentDefinitions/*.yaml** - All YAML/JSON files in this directory

### Custom Paths

Specify additional paths in appsettings.json:

```json
{
  "ExperimentFramework": {
    "ConfigurationPaths": [
      "config/experiments/main.yaml",
      "config/experiments/feature-flags.yaml"
    ]
  }
}
```

Or programmatically:

```csharp
services.AddExperimentFrameworkFromConfiguration(configuration, opts =>
{
    opts.BasePath = "./config";
    opts.AdditionalPaths.Add("custom/experiments.yaml");
    opts.ScanDefaultPaths = true;
});
```

---

## YAML Schema Reference

### Root Structure

```yaml
experimentFramework:
  settings:           # Global framework settings
  decorators:         # Global decorator pipeline
  trials:             # Standalone trial definitions
  experiments:        # Named experiment groups
```

### Settings

```yaml
experimentFramework:
  settings:
    proxyStrategy: sourceGenerators  # or: dispatchProxy
    namingConvention: default        # Custom naming convention identifier
```

| Property | Values | Description |
|----------|--------|-------------|
| `proxyStrategy` | `sourceGenerators`, `dispatchProxy` | Proxy generation strategy |
| `namingConvention` | `default`, custom identifier | Naming convention for selectors |

### Decorators

Configure the global decorator pipeline:

```yaml
experimentFramework:
  decorators:
    - type: logging
      options:
        benchmarks: true
        errorLogging: true

    - type: timeout
      options:
        timeout: "00:00:30"
        action: fallbackToDefault

    - type: circuitBreaker
      options:
        failureRatioThreshold: 0.5
        minimumThroughput: 10
        samplingDuration: "00:00:30"
        breakDuration: "00:01:00"

    - type: outcomeCollection
      options:
        collectDuration: true
        collectErrors: true
```

| Decorator Type | Description | Required Package |
|----------------|-------------|------------------|
| `logging` | Benchmarks and error logging | Built-in |
| `timeout` | Timeout enforcement | Built-in |
| `circuitBreaker` | Polly circuit breaker | `ExperimentFramework.Resilience` |
| `outcomeCollection` | Automatic outcome recording | `ExperimentFramework.Data` |
| `custom` | Custom decorator (requires `typeName`) | - |

### Trial Definition

```yaml
trials:
  - serviceType: "IMyDatabase"              # Interface to experiment on
    selectionMode:                          # How to select the trial
      type: featureFlag
      flagName: UseCloudDb
    control:                                # Control (baseline) implementation
      key: control
      implementationType: "MyDbContext"
    conditions:                             # Experimental conditions
      - key: "true"
        implementationType: "MyCloudDbContext"
      - key: variant-a
        implementationType: "VariantADbContext"
    errorPolicy:                            # Error handling strategy
      type: fallbackToControl
    activation:                             # Time-based activation
      from: "2025-01-01T00:00:00Z"
      until: "2025-06-30T23:59:59Z"
```

### Selection Modes

#### Feature Flag (Boolean)

```yaml
selectionMode:
  type: featureFlag
  flagName: MyFeatureFlag    # Optional: uses naming convention if omitted
```

Routes based on `IFeatureManager.IsEnabledAsync()`. Trial keys are `"true"` and `"false"`.

#### Configuration Key

```yaml
selectionMode:
  type: configurationKey
  key: "Experiments:ServiceVariant"    # Configuration path
```

Routes based on `IConfiguration` string value.

#### Variant Feature Flag

```yaml
selectionMode:
  type: variantFeatureFlag
  flagName: MyVariantFeature
```

Routes based on `IVariantFeatureManager.GetVariantAsync()`. Requires `ExperimentFramework.FeatureManagement`.

#### Sticky Routing

```yaml
selectionMode:
  type: stickyRouting
  selectorName: checkout-experiment    # Optional identifier
```

Deterministic routing based on user/session identity. Requires `ExperimentFramework.StickyRouting` and an `IExperimentIdentityProvider` implementation.

#### OpenFeature

```yaml
selectionMode:
  type: openFeature
  flagName: payment-processor
```

Routes based on OpenFeature flag evaluation. Requires `ExperimentFramework.OpenFeature`.

#### Custom Mode

```yaml
selectionMode:
  type: custom
  modeIdentifier: Redis
  selectorName: cache:provider
```

Uses a registered custom selection mode provider.

### Error Policies

| Type | Description |
|------|-------------|
| `throw` | Exception propagates immediately (default) |
| `fallbackToControl` | Falls back to control on error |
| `fallbackTo` | Falls back to specific key |
| `tryInOrder` | Tries keys in specified order |
| `tryAny` | Tries all conditions until one succeeds |

```yaml
# Fallback to control
errorPolicy:
  type: fallbackToControl

# Fallback to specific key
errorPolicy:
  type: fallbackTo
  fallbackKey: noop

# Try in order
errorPolicy:
  type: tryInOrder
  fallbackKeys:
    - cache
    - memory
    - static

# Try any
errorPolicy:
  type: tryAny
```

### Activation Windows

```yaml
activation:
  from: "2025-01-01T00:00:00Z"     # Start date (optional)
  until: "2025-06-30T23:59:59Z"   # End date (optional)
```

Trials are only active within the specified time window. Outside this window, the control is used.

### Named Experiments

Group related trials into named experiments with metadata:

```yaml
experiments:
  - name: q1-checkout-optimization
    metadata:
      owner: platform-team
      ticket: PLAT-1234
      description: Testing new checkout flow
    activation:
      from: "2025-01-01T00:00:00Z"
      until: "2025-03-31T23:59:59Z"
    trials:
      - serviceType: ICheckoutService
        selectionMode:
          type: stickyRouting
        control:
          key: control
          implementationType: LegacyCheckout
        conditions:
          - key: new
            implementationType: NewCheckout
    hypothesis:
      name: checkout-conversion
      type: superiority
      nullHypothesis: "No difference in conversion rate"
      alternativeHypothesis: "New checkout improves conversion"
      primaryEndpoint:
        name: purchase_completed
        outcomeType: binary
        higherIsBetter: true
      expectedEffectSize: 0.05
      successCriteria:
        alpha: 0.05
        power: 0.80
```

---

## JSON Configuration

JSON configuration follows the same structure as YAML. Use PascalCase for property names:

```json
{
  "ExperimentFramework": {
    "Settings": {
      "ProxyStrategy": "dispatchProxy"
    },
    "Trials": [
      {
        "ServiceType": "IMyDatabase",
        "SelectionMode": {
          "Type": "featureFlag",
          "FlagName": "UseCloudDb"
        },
        "Control": {
          "Key": "control",
          "ImplementationType": "MyDbContext"
        },
        "Conditions": [
          {
            "Key": "true",
            "ImplementationType": "MyCloudDbContext"
          }
        ],
        "ErrorPolicy": {
          "Type": "fallbackToControl"
        }
      }
    ]
  }
}
```

---

## Type Resolution

Types can be referenced by:

| Format | Example |
|--------|---------|
| Simple name | `MyDbContext` |
| Full name | `MyApp.Data.MyDbContext` |
| Assembly-qualified | `MyApp.Data.MyDbContext, MyApp` |

### Type Aliases

For cleaner configuration, register type aliases:

```csharp
services.AddExperimentFrameworkFromConfiguration(configuration, opts =>
{
    opts.TypeAliases.Add("IMyDb", typeof(IMyDatabase));
    opts.TypeAliases.Add("LocalDb", typeof(MyDbContext));
    opts.TypeAliases.Add("CloudDb", typeof(MyCloudDbContext));
});
```

Then use in YAML:

```yaml
trials:
  - serviceType: IMyDb
    control:
      key: control
      implementationType: LocalDb
    conditions:
      - key: cloud
        implementationType: CloudDb
```

---

## Hybrid Mode

Combine programmatic and file-based configuration:

```csharp
// Programmatic configuration
var builder = ExperimentFrameworkBuilder.Create()
    .AddLogger(l => l.AddBenchmarks())
    .Trial<IMyService>(t => t
        .UsingFeatureFlag("MyFlag")
        .AddControl<DefaultImpl>()
        .AddCondition<ExperimentalImpl>("true"));

// Merge with file configuration
services.AddExperimentFramework(builder, configuration, opts =>
{
    opts.ScanDefaultPaths = true;
});
```

File configuration is merged on top of programmatic configuration.

---

## Hot Reload

Enable automatic configuration reloading when files change:

```csharp
services.AddExperimentFrameworkFromConfiguration(configuration, opts =>
{
    opts.EnableHotReload = true;
    opts.OnConfigurationChanged = newConfig =>
    {
        logger.LogInformation("Experiment configuration reloaded");
    };
});
```

Note: Hot reload creates new proxy instances. Long-running operations may continue using the old configuration until completion.

---

## Validation

The framework validates configuration on startup:

- Required properties (serviceType, control, etc.)
- Valid selection mode types
- Non-duplicate condition keys
- Valid activation date ranges
- Type resolution

Control validation behavior:

```csharp
services.AddExperimentFrameworkFromConfiguration(configuration, opts =>
{
    opts.ThrowOnValidationErrors = true;  // Throw on startup (default)
    // or
    opts.ThrowOnValidationErrors = false; // Log warnings, skip invalid trials
});
```

---

## Complete Example

### experiments.yaml

```yaml
experimentFramework:
  settings:
    proxyStrategy: dispatchProxy

  decorators:
    - type: logging
      options:
        benchmarks: true
        errorLogging: true
    - type: outcomeCollection
      options:
        collectDuration: true
        collectErrors: true

  trials:
    # Simple feature flag trial
    - serviceType: "MyApp.Services.IRecommendationService, MyApp"
      selectionMode:
        type: featureFlag
        flagName: UseMLRecommendations
      control:
        key: control
        implementationType: "MyApp.Services.RuleBasedRecommendations, MyApp"
      conditions:
        - key: "true"
          implementationType: "MyApp.Services.MLRecommendations, MyApp"
      errorPolicy:
        type: fallbackToControl

    # Multi-variant configuration-based trial
    - serviceType: "MyApp.Payments.IPaymentProcessor, MyApp"
      selectionMode:
        type: configurationKey
        key: "Payments:Processor"
      control:
        key: stripe
        implementationType: "MyApp.Payments.StripeProcessor, MyApp"
      conditions:
        - key: paypal
          implementationType: "MyApp.Payments.PayPalProcessor, MyApp"
        - key: square
          implementationType: "MyApp.Payments.SquareProcessor, MyApp"
      errorPolicy:
        type: tryInOrder
        fallbackKeys:
          - stripe

  experiments:
    - name: checkout-optimization-q1
      metadata:
        owner: growth-team
        ticket: GROWTH-789
      activation:
        from: "2025-01-15T00:00:00Z"
        until: "2025-03-31T23:59:59Z"
      trials:
        - serviceType: "MyApp.Checkout.ICheckoutFlow, MyApp"
          selectionMode:
            type: stickyRouting
          control:
            key: legacy
            implementationType: "MyApp.Checkout.LegacyCheckout, MyApp"
          conditions:
            - key: streamlined
              implementationType: "MyApp.Checkout.StreamlinedCheckout, MyApp"
          errorPolicy:
            type: fallbackToControl
      hypothesis:
        name: checkout-conversion
        type: superiority
        nullHypothesis: "No difference in checkout completion rate"
        alternativeHypothesis: "Streamlined checkout improves completion rate"
        primaryEndpoint:
          name: checkout_completed
          outcomeType: binary
          higherIsBetter: true
        expectedEffectSize: 0.03
        successCriteria:
          alpha: 0.05
          power: 0.80
          minimumSampleSize: 5000
```

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register concrete implementations
builder.Services.AddScoped<RuleBasedRecommendations>();
builder.Services.AddScoped<MLRecommendations>();
builder.Services.AddScoped<StripeProcessor>();
builder.Services.AddScoped<PayPalProcessor>();
builder.Services.AddScoped<SquareProcessor>();
builder.Services.AddScoped<LegacyCheckout>();
builder.Services.AddScoped<StreamlinedCheckout>();

// Register interfaces with default implementations
builder.Services.AddScoped<IRecommendationService, RuleBasedRecommendations>();
builder.Services.AddScoped<IPaymentProcessor, StripeProcessor>();
builder.Services.AddScoped<ICheckoutFlow, LegacyCheckout>();

// Register sticky routing identity provider
builder.Services.AddExperimentStickyRouting();
builder.Services.AddScoped<IExperimentIdentityProvider, UserIdentityProvider>();

// Register data collection for experiments
builder.Services.AddExperimentDataCollection();

// Load experiment configuration from YAML
builder.Services.AddExperimentFrameworkFromConfiguration(
    builder.Configuration,
    opts =>
    {
        opts.ScanDefaultPaths = true;
        opts.EnableHotReload = true;
    });

var app = builder.Build();
app.Run();
```

---

## DSL to Fluent API Mapping

| YAML | Fluent API |
|------|------------|
| `selectionMode.type: featureFlag` | `.UsingFeatureFlag()` |
| `selectionMode.type: configurationKey` | `.UsingConfigurationKey()` |
| `selectionMode.type: variantFeatureFlag` | `.UsingVariantFeatureFlag()` |
| `selectionMode.type: openFeature` | `.UsingOpenFeature()` |
| `selectionMode.type: stickyRouting` | `.UsingStickyRouting()` |
| `selectionMode.type: custom` | `.UsingCustomMode()` |
| `errorPolicy.type: throw` | (default) |
| `errorPolicy.type: fallbackToControl` | `.OnErrorFallbackToControl()` |
| `errorPolicy.type: fallbackTo` | `.OnErrorFallbackTo(key)` |
| `errorPolicy.type: tryInOrder` | `.OnErrorTryInOrder(keys)` |
| `errorPolicy.type: tryAny` | `.OnErrorTryAny()` |
| `activation.from/until` | `.ActiveFrom()/.ActiveUntil()` |
| `decorators[].type: logging` | `.AddLogger()` |
| `decorators[].type: circuitBreaker` | `.WithCircuitBreaker()` |
| `decorators[].type: outcomeCollection` | `.WithOutcomeCollection()` |

---

## Best Practices

### 1. Organize by Feature

```
ExperimentDefinitions/
  checkout/
    checkout-flow.yaml
    payment-processing.yaml
  recommendations/
    algorithm-tests.yaml
  shared/
    decorators.yaml
```

### 2. Use Environment-Specific Files

```
experiments.yaml              # Base configuration
experiments.Development.yaml  # Dev overrides
experiments.Production.yaml   # Prod overrides
```

### 3. Leverage Type Aliases

Keep YAML clean by registering commonly-used types as aliases.

### 4. Validate Before Deploy

```csharp
// In a startup check or health check
var loader = new ExperimentConfigurationLoader();
var config = loader.Load(configuration, options);
var validator = new ConfigurationValidator();
var result = validator.Validate(config);

if (!result.IsValid)
{
    foreach (var error in result.FatalErrors)
    {
        logger.LogError("Config error: {Path} - {Message}", error.Path, error.Message);
    }
    throw new InvalidOperationException("Invalid experiment configuration");
}
```

### 5. Use Named Experiments for A/B Tests

Named experiments with hypotheses enable statistical analysis and clear documentation of what you're testing.

---

## Troubleshooting

### Type Not Found

```
TypeResolutionException: Could not resolve type 'IMyService'
```

**Solutions:**
- Use assembly-qualified names: `"MyApp.Services.IMyService, MyApp"`
- Register type aliases
- Ensure the assembly is loaded

### Configuration Not Loading

**Check:**
- File is in the correct location
- File has correct extension (`.yaml`, `.yml`, `.json`)
- YAML syntax is valid (use a YAML linter)
- `experimentFramework:` root key is present

### Hot Reload Not Working

**Check:**
- `EnableHotReload = true` is set
- File is being modified (not recreated)
- Application has read access to the file

### Validation Errors

Enable verbose logging to see validation details:

```csharp
opts.ThrowOnValidationErrors = false; // Log instead of throw
```

Check logs for `[Warning]` entries about skipped trials.

---

## Next Steps

- [Plugin System](plugins.md) - Load experiment implementations from external DLLs at runtime
- [Extensibility](extensibility.md) - Create custom selection mode providers
- [Getting Started](getting-started.md) - Basic framework setup with code examples
