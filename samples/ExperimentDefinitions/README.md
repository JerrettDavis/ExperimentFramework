# Example Experiment Definitions

This directory contains example YAML configuration files demonstrating various ExperimentFramework features.

## Examples

| File | Description |
|------|-------------|
| [01-simple-feature-flag.yaml](01-simple-feature-flag.yaml) | Basic A/B test with boolean feature flag |
| [02-multi-variant.yaml](02-multi-variant.yaml) | Multi-variant experiment with configuration keys |
| [03-sticky-routing.yaml](03-sticky-routing.yaml) | Deterministic user-based routing |
| [04-time-bounded-experiment.yaml](04-time-bounded-experiment.yaml) | Time-limited experiments with activation windows |
| [05-named-experiment-with-hypothesis.yaml](05-named-experiment-with-hypothesis.yaml) | Scientific experiments with hypotheses and statistical criteria |
| [06-resilience-patterns.yaml](06-resilience-patterns.yaml) | Circuit breaker, timeout, and fallback patterns |
| [07-openfeature-integration.yaml](07-openfeature-integration.yaml) | OpenFeature standard integration |
| [08-custom-selection-mode.yaml](08-custom-selection-mode.yaml) | Custom selection mode providers |

## Usage

Copy these files to your project and modify as needed:

```bash
# Copy to your project
cp samples/ExperimentDefinitions/*.yaml your-project/ExperimentDefinitions/

# Or reference directly
services.AddExperimentFrameworkFromConfiguration(configuration, opts =>
{
    opts.AdditionalPaths.Add("samples/ExperimentDefinitions/01-simple-feature-flag.yaml");
});
```

## Quick Start

1. Install the configuration package:

```bash
dotnet add package ExperimentFramework.Configuration
```

2. Create your `experiments.yaml` based on these examples

3. Register in `Program.cs`:

```csharp
builder.Services.AddExperimentFrameworkFromConfiguration(builder.Configuration);
```

## Documentation

See the [Configuration Guide](../../docs/user-guide/configuration.md) for complete documentation.
