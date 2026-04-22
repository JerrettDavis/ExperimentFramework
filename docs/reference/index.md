# Introduction

ExperimentFramework is a library for .NET that enables runtime experimentation and A/B testing through dependency injection. It allows you to safely test new implementations of services in production by routing a subset of traffic to experimental code paths while maintaining the ability to fall back to stable implementations if errors occur.

## What is ExperimentFramework?

At its core, ExperimentFramework is a dynamic proxy generator that intercepts service method calls and routes them to different implementations (called "trials") based on configurable selection criteria. This happens transparently to your application code, requiring no changes to existing business logic.

The framework integrates with .NET's dependency injection container and Microsoft's Feature Management library to provide a type-safe, configuration-driven approach to experimentation.

## When to Use ExperimentFramework

ExperimentFramework is designed for scenarios where you need to:

- **Test new service implementations** in production before fully committing to them
- **Gradually roll out changes** to a subset of users or requests
- **Compare performance** between different algorithms or data sources
- **Implement A/B testing** for features that affect backend behavior
- **Support multi-variant experiments** with more than two options
- **Provide fallback mechanisms** when experimental code fails
- **Conduct rigorous scientific experiments** with statistical analysis *(NEW)*
- **Generate publication-ready reports** with effect sizes and confidence intervals *(NEW)*

## When Not to Use ExperimentFramework

This framework may not be appropriate for:

- **UI/frontend experiments** - Use client-side A/B testing tools instead
- **Simple configuration switches** - If you just need to toggle behavior, use IOptions<T>
- **Single deployments** - If you're not running multiple implementations simultaneously
- **Performance-critical hot paths** - The proxy introduces minimal but measurable overhead

## How It Works

ExperimentFramework uses several .NET features to enable runtime experimentation:

1. **Roslyn Source Generators**: Generate compile-time proxy classes that intercept method calls and route them to different implementations
2. **Dependency Injection**: Manages the lifecycle of trial implementations and ensures they receive their dependencies
3. **Configuration System**: Reads feature flags and settings that control trial selection
4. **Feature Management**: Integrates with Microsoft.FeatureManagement for advanced feature flag scenarios

### The Experiment Workflow

1. You register multiple implementations of the same interface with your DI container
2. You define an experiment using the fluent builder API, specifying:
   - Which interface to experiment on
   - What selection mode to use (feature flag, configuration, variant, or sticky routing)
   - Which implementations to route to based on the selection value
   - What error handling strategy to apply
3. The framework configures your interface with a proxy
4. When code requests the interface from DI, it receives a proxy instance
5. Each method call on the proxy:
   - Evaluates the selection criteria
   - Resolves the appropriate trial implementation from DI
   - Invokes the method on that trial
   - Applies any decorators (logging, telemetry, error handling)
   - Returns the result to the caller

### Key Concepts

**Trial**: A specific implementation of a service that participates in an experiment. Each trial is identified by a unique key.

**Trial Key**: A string identifier for a trial. For boolean feature flags, these are typically "true" and "false". For other modes, they can be any string value.

**Selection Mode**: The strategy used to choose which trial to execute. The framework supports five modes:
- Boolean Feature Flag (Microsoft Feature Management)
- Configuration Value (IConfiguration)
- Variant Feature Flag (Microsoft Feature Management variants)
- Sticky Routing (deterministic user-based routing)
- OpenFeature (open standard for feature flags)

**Proxy**: A dynamically generated type that implements your service interface and delegates calls to the selected trial.

**Decorator**: A component that wraps trial execution to provide cross-cutting concerns like logging, telemetry, or caching.

**Error Policy**: A strategy for handling exceptions during trial execution, such as falling back to a default trial or trying all trials in sequence.

## Architecture Overview

```
Application Code
    ↓
IMyService (Proxy)
    ↓
┌─────────────────────────┐
│  Selection Logic        │
│  - Evaluate feature flag│
│  - Read configuration   │
│  - Compute user hash    │
└─────────────────────────┘
    ↓
┌─────────────────────────┐
│  Decorator Pipeline     │
│  - Telemetry            │
│  - Benchmarks           │
│  - Error Logging        │
└─────────────────────────┘
    ↓
┌─────────────────────────┐
│  Error Policy           │
│  - Throw on failure     │
│  - Fallback to default  │
│  - Try all trials       │
└─────────────────────────┘
    ↓
┌─────────────────────────┐
│  Trial Execution        │
│  ServiceA.Method()      │
└─────────────────────────┘
    ↓
Return Result
```

## Design Principles

The framework follows these design principles:

**Minimal API Surface**: The public API is focused on the builder pattern for defining experiments. Everything else happens through standard .NET mechanisms.

**Fail-Safe Defaults**: Without any configuration, experiments use their default trial. Selection criteria failures fall back gracefully.

**Zero Overhead When Disabled**: The no-op telemetry implementation compiles away to nothing, ensuring experiments without telemetry have minimal performance impact.

**Convention Over Configuration**: Default naming conventions mean you often don't need to specify feature flag or configuration key names.

**Composable**: Decorators can be combined in any order. Error policies can be chosen independently of selection modes.

**Testable**: Experiments can be easily tested by controlling feature flags and configuration in tests.

## Next Steps

### Getting Started
- [Getting Started](getting-started.md) - Install the framework and create your first experiment
- [Core Concepts](core-concepts.md) - Detailed explanation of trials, proxies, and decorators

### Traffic Routing & Selection
- [Selection Modes](selection-modes.md) - Learn about the different ways to select trials
- [OpenFeature Integration](openfeature.md) - Use OpenFeature providers for flag management

### Observability & Resilience
- [Telemetry](telemetry.md) - OpenTelemetry distributed tracing
- [Metrics](metrics.md) - Prometheus and OpenTelemetry metrics
- [Error Handling](error-handling.md) - Error policies and fallback strategies
- [Circuit Breaker](circuit-breaker.md) - Automatic failure isolation
- [Timeout Enforcement](timeout-enforcement.md) - Prevent runaway operations

### Scientific Experimentation
- [Data Collection](data-collection.md) - Collect experiment outcomes
- [Statistical Analysis](statistical-analysis.md) - Analyze results with statistical tests
- [Hypothesis Testing](hypothesis-testing.md) - Define and test hypotheses
- [Power Analysis](power-analysis.md) - Calculate required sample sizes

### Enterprise Features *(NEW)*
- [Plugin System](plugins.md) - Dynamic assembly loading for experiments
- [YAML/JSON Configuration](configuration.md) - Declarative experiment definitions

### Reference
- [Naming Conventions](naming-conventions.md) - Naming patterns and conventions
- [Extensibility](extensibility.md) - Custom decorators and providers
- [Advanced Topics](advanced.md) - Advanced configuration and patterns
