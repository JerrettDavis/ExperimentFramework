---
_layout: landing
---

# ExperimentFramework

A runtime experiment and A/B testing framework for .NET that enables safe, controlled rollout of new features through dependency injection.

## Overview

ExperimentFramework allows you to run experiments by routing service calls to different implementations based on runtime configuration, feature flags, or user identity. Built on top of .NET's dependency injection container, it provides a type-safe way to test new code paths in production without modifying existing application logic.

## Key Capabilities

- **Multiple Selection Modes**: Route traffic using boolean feature flags, configuration values, variant flags, deterministic user hashing, or OpenFeature providers
- **Error Handling**: Built-in fallback strategies when experimental implementations fail
- **Observability**: Integrated telemetry with OpenTelemetry support for tracking experiment execution
- **Type-Safe**: Strongly-typed fluent API with compile-time validation
- **Zero Overhead**: No-op implementations for production scenarios where telemetry isn't needed

## Installation

Install the ExperimentFramework package via NuGet:

```bash
dotnet add package ExperimentFramework
```

**For source-generated proxies (recommended for best performance):**

```bash
dotnet add package ExperimentFramework.Generators
```

Use `[ExperimentCompositionRoot]` attribute or `.UseSourceGenerators()` in your configuration.

**For runtime proxies (alternative, more flexible):**

No additional package needed. Use `.UseDispatchProxy()` in your configuration.

## Quick Example

Define a trial that switches between database implementations based on a feature flag:

```csharp
// Register your service implementations
services.AddScoped<LocalDatabase>();
services.AddScoped<CloudDatabase>();
services.AddScoped<IDatabase, LocalDatabase>();

// Configure the experiment (source-generated proxies)
[ExperimentCompositionRoot]
static ExperimentFrameworkBuilder ConfigureExperiments()
{
    return ExperimentFrameworkBuilder.Create()
        .Trial<IDatabase>(t => t
            .UsingFeatureFlag("UseCloudDb")
            .AddControl<LocalDatabase>()
            .AddCondition<CloudDatabase>("true")
            .OnErrorFallbackToControl());
}

// OR use runtime proxies
static ExperimentFrameworkBuilder ConfigureWithRuntimeProxies()
{
    return ExperimentFrameworkBuilder.Create()
        .Trial<IDatabase>(t => t
            .UsingFeatureFlag("UseCloudDb")
            .AddControl<LocalDatabase>()
            .AddCondition<CloudDatabase>("true")
            .OnErrorFallbackToControl())
        .UseDispatchProxy();
}

// Register with dependency injection
var experiments = ConfigureExperiments();
services.AddExperimentFramework(experiments);
```

Your application code remains unchanged:

```csharp
public class DataService
{
    private readonly IDatabase _database;

    public DataService(IDatabase database)
    {
        _database = database;
    }

    public async Task<Data> GetDataAsync()
    {
        // Automatically routes to LocalDatabase or CloudDatabase
        // based on the feature flag value
        return await _database.QueryAsync();
    }
}
```

## Configuration

Control experiment behavior through configuration:

```json
{
  "FeatureManagement": {
    "UseCloudDb": true
  }
}
```

## Next Steps

- [Getting Started](user-guide/getting-started.md) - Complete walkthrough with a working example
- [Core Concepts](user-guide/core-concepts.md) - Understanding trials, proxies, and decorators
- [Selection Modes](user-guide/selection-modes.md) - Deep dive into routing strategies

## Requirements

- .NET 10.0 or later
- Microsoft.Extensions.DependencyInjection 10.0 or later
- Microsoft.FeatureManagement 4.4.0 or later (for feature flag support)

## License

This project is licensed under the MIT License.
