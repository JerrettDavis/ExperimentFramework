# OpenFeature Integration

ExperimentFramework supports [OpenFeature](https://openfeature.dev/), an open standard for feature flag management. This allows integration with any OpenFeature-compatible provider such as LaunchDarkly, Flagsmith, CloudBees, or custom providers.

> **Package Required**: OpenFeature support is provided by the `ExperimentFramework.OpenFeature` package.

## Installation

```bash
dotnet add package ExperimentFramework.OpenFeature
dotnet add package OpenFeature
```

## Configuration

Register the provider and use `UsingOpenFeature()` to configure experiments:

```csharp
// Register the OpenFeature selection mode provider
services.AddExperimentOpenFeature();

// Define experiments
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IPaymentProcessor>(t => t
        .UsingOpenFeature("payment-processor-experiment")
        .AddControl<StripeProcessor>("stripe")
        .AddCondition<PayPalProcessor>("paypal")
        .AddCondition<SquareProcessor>("square"))
    .UseDispatchProxy();

services.AddExperimentFramework(experiments);
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

The framework automatically detects the flag type based on condition keys:

**Boolean flags** (when conditions are "true" and "false"):

```csharp
.Trial<IFeature>(t => t
    .UsingOpenFeature("new-feature")
    .AddControl<LegacyFeature>("false")
    .AddCondition<NewFeature>("true"))
```

Uses `GetBooleanValueAsync()` from OpenFeature.

**String flags** (multi-variant):

```csharp
.Trial<IAlgorithm>(t => t
    .UsingOpenFeature("algorithm-variant")
    .AddControl<ControlAlgorithm>("control")
    .AddVariant<VariantA>("variant-a")
    .AddVariant<VariantB>("variant-b"))
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

When OpenFeature is not configured or flag evaluation fails, the framework falls back to the control condition. This provides resilience during:

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

## Package Architecture

OpenFeature support is provided via a separate package (`ExperimentFramework.OpenFeature`) that:

- Has a direct dependency on the OpenFeature SDK
- Provides the `UsingOpenFeature()` extension method
- Registers the `OpenFeatureProviderFactory` for runtime selection
- Graceful fallback to control condition when evaluation fails

This modular approach means you only include OpenFeature dependencies if you actually use them.

## Example: Complete Setup

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Register OpenFeature selection mode provider
builder.Services.AddExperimentOpenFeature();

// Configure OpenFeature provider
await Api.Instance.SetProviderAsync(new YourProvider());

// Configure experiments
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IRecommendationEngine>(t => t
        .UsingOpenFeature("recommendation-algorithm")
        .AddControl<CollaborativeFiltering>("collaborative")
        .AddCondition<ContentBased>("content-based")
        .AddCondition<HybridApproach>("hybrid")
        .OnErrorFallbackToControl())
    .UseDispatchProxy();

builder.Services.AddExperimentFramework(experiments);

var app = builder.Build();
```

## Combining with Other Selection Modes

You can use different selection modes for different services in the same application:

```csharp
ExperimentFrameworkBuilder.Create()
    // OpenFeature for external flag management
    .Trial<IPaymentProcessor>(t => t
        .UsingOpenFeature("payment-experiment")
        .AddControl<StripeProcessor>("stripe")
        .AddCondition<PayPalProcessor>("paypal"))

    // Microsoft Feature Management for internal flags
    .Trial<ISearchService>(t => t
        .UsingFeatureFlag("SearchV2")
        .AddControl<LegacySearch>("false")
        .AddCondition<NewSearch>("true"))

    // Configuration for static routing
    .Trial<ILogger>(t => t
        .UsingConfigurationKey("Logging:Provider")
        .AddControl<ConsoleLogger>("console")
        .AddCondition<FileLogger>("file"))
```
