# ExperimentFramework.Configuration

Declarative YAML/JSON configuration for ExperimentFramework experiments.

## Installation

```bash
dotnet add package ExperimentFramework.Configuration
```

## Quick Start

### 1. Create experiments.yaml

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

### 2. Register from Configuration

```csharp
builder.Services.AddExperimentFrameworkFromConfiguration(builder.Configuration);
```

## Features

- **YAML and JSON support** - Use your preferred format
- **Auto-discovery** - Finds configuration in standard locations
- **Type aliases** - Use simple names instead of assembly-qualified types
- **Hot reload** - Configuration changes apply without restart
- **Validation** - Comprehensive validation with helpful error messages
- **Hybrid mode** - Combine with programmatic configuration

## Configuration Options

```csharp
services.AddExperimentFrameworkFromConfiguration(configuration, opts =>
{
    opts.BasePath = "./config";
    opts.ScanDefaultPaths = true;
    opts.EnableHotReload = true;
    opts.ThrowOnValidationErrors = true;
    opts.TypeAliases.Add("IMyDb", typeof(IMyDatabase));
    opts.AdditionalPaths.Add("custom/experiments.yaml");
});
```

## File Discovery

Default scan paths:
1. `appsettings.json` - `ExperimentFramework` section
2. `appsettings.{Environment}.json` - Environment overrides
3. `experiments.yaml` or `experiments.yml` - Root directory
4. `ExperimentDefinitions/**/*.yaml` - All files in this directory

## Selection Modes

| YAML Type | Description |
|-----------|-------------|
| `featureFlag` | Boolean feature flag |
| `configurationKey` | Configuration value |
| `variantFeatureFlag` | Microsoft.FeatureManagement variants |
| `stickyRouting` | Deterministic user routing |
| `openFeature` | OpenFeature standard |
| `custom` | Custom provider |

## Error Policies

| Type | Description |
|------|-------------|
| `throw` | Propagate exceptions (default) |
| `fallbackToControl` | Use control on error |
| `fallbackTo` | Use specific fallback |
| `tryInOrder` | Try keys in order |
| `tryAny` | Try all until success |

## Documentation

See the [full documentation](../../docs/user-guide/configuration.md) for:
- Complete YAML schema reference
- Type resolution strategies
- Named experiments with hypotheses
- Resilience patterns
- Best practices
