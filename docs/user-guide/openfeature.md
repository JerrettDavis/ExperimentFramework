# OpenFeature Integration

ExperimentFramework supports [OpenFeature](https://openfeature.dev/), an open standard for feature flag management. This allows integration with any OpenFeature-compatible provider such as LaunchDarkly, Flagsmith, CloudBees, or custom providers.

## Configuration

Use `UsingOpenFeature()` to configure an experiment to use OpenFeature for trial selection:

```csharp
services.AddExperimentFramework(
    ExperimentFrameworkBuilder.Create()
        .Define<IPaymentProcessor>(c => c
            .UsingOpenFeature("payment-processor-experiment")
            .AddDefaultTrial<StripeProcessor>("stripe")
            .AddTrial<PayPalProcessor>("paypal")
            .AddTrial<SquareProcessor>("square"))
        .UseDispatchProxy());
```

## Flag Key Naming

When no flag key is specified, the framework generates a kebab-case name from the service type:

| Service Type | Generated Flag Key |
|--------------|-------------------|
| `IPaymentProcessor` | `payment-processor` |
| `IUserService` | `user-service` |
| `IDataRepository` | `data-repository` |

You can override this with an explicit flag key:

```csharp
.UsingOpenFeature("my-custom-flag-key")
```

## Boolean vs String Flags

The framework automatically detects the flag type based on trial keys:

**Boolean flags** (when trials are "true" and "false"):

```csharp
.Define<IFeature>(c => c
    .UsingOpenFeature("new-feature")
    .AddDefaultTrial<LegacyFeature>("false")
    .AddTrial<NewFeature>("true"))
```

Uses `GetBooleanValueAsync()` from OpenFeature.

**String flags** (multi-variant):

```csharp
.Define<IAlgorithm>(c => c
    .UsingOpenFeature("algorithm-variant")
    .AddDefaultTrial<ControlAlgorithm>("control")
    .AddTrial<VariantA>("variant-a")
    .AddTrial<VariantB>("variant-b"))
```

Uses `GetStringValueAsync()` from OpenFeature.

## Provider Setup

Configure your OpenFeature provider before using ExperimentFramework:

```csharp
// Example with InMemoryProvider for testing
await Api.Instance.SetProviderAsync(new InMemoryProvider(new Dictionary<string, Flag>
{
    { "payment-processor-experiment", new Flag<string>("paypal") },
    { "new-feature", new Flag<bool>(true) }
}));
```

For production, use your preferred provider:

```csharp
// LaunchDarkly example
await Api.Instance.SetProviderAsync(
    new LaunchDarklyProvider(Configuration.Builder("sdk-key").Build()));

// Flagsmith example
await Api.Instance.SetProviderAsync(
    new FlagsmithProvider(new FlagsmithConfiguration { ApiUrl = "..." }));
```

## Fallback Behavior

When OpenFeature is not configured or flag evaluation fails, the framework falls back to the default trial. This provides resilience during:

- Provider initialization
- Network failures
- Missing flag definitions
- Invalid flag values

## Evaluation Context

OpenFeature evaluation context can be set globally or per-client. The framework uses the default client context:

```csharp
// Set global context
Api.Instance.SetContext(new EvaluationContextBuilder()
    .Set("userId", "user-123")
    .Set("region", "us-east-1")
    .Build());
```

## Soft Dependency

OpenFeature is a soft dependency - the framework uses reflection to access OpenFeature APIs. This means:

- No compile-time dependency on the OpenFeature package
- Graceful fallback when OpenFeature is not installed
- Works with any OpenFeature SDK version

To use OpenFeature, add the package to your project:

```bash
dotnet add package OpenFeature
```

## Example: Complete Setup

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Configure OpenFeature provider
await Api.Instance.SetProviderAsync(new YourProvider());

// Configure experiments
builder.Services.AddExperimentFramework(
    ExperimentFrameworkBuilder.Create()
        .Define<IRecommendationEngine>(c => c
            .UsingOpenFeature("recommendation-algorithm")
            .AddDefaultTrial<CollaborativeFiltering>("collaborative")
            .AddTrial<ContentBased>("content-based")
            .AddTrial<HybridApproach>("hybrid")
            .OnErrorRedirectAndReplayDefault())
        .UseDispatchProxy());

var app = builder.Build();
```

## Combining with Other Selection Modes

You can use different selection modes for different services in the same application:

```csharp
ExperimentFrameworkBuilder.Create()
    // OpenFeature for external flag management
    .Define<IPaymentProcessor>(c => c
        .UsingOpenFeature("payment-experiment")
        .AddDefaultTrial<StripeProcessor>("stripe")
        .AddTrial<PayPalProcessor>("paypal"))

    // Microsoft Feature Management for internal flags
    .Define<ISearchService>(c => c
        .UsingFeatureFlag("SearchV2")
        .AddDefaultTrial<LegacySearch>("false")
        .AddTrial<NewSearch>("true"))

    // Configuration for static routing
    .Define<ILogger>(c => c
        .UsingConfigurationKey("Logging:Provider")
        .AddDefaultTrial<ConsoleLogger>("console")
        .AddTrial<FileLogger>("file"))
```
