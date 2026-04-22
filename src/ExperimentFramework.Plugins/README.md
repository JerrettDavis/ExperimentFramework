# ExperimentFramework.Plugins

Dynamic assembly loading and plugin system for ExperimentFramework. Enables runtime loading of experimental implementations from external DLLs with configurable isolation modes.

## Installation

```bash
dotnet add package ExperimentFramework.Plugins
```

## Quick Start

### 1. Register the plugin system

```csharp
builder.Services.AddExperimentFrameworkPlugins(options =>
{
    options.PluginDirectory = "./plugins";
    options.IsolationMode = PluginIsolationMode.Isolated;
});
```

### 2. Load plugins at runtime

Plugins are discovered automatically from the configured directory. Each plugin assembly is scanned for experiment implementations that match registered service types.

### 3. Hot reload (optional)

```csharp
builder.Services.AddExperimentFrameworkPlugins(options =>
{
    options.PluginDirectory = "./plugins";
    options.EnableHotReload = true;
});
```

## Features

- **Runtime loading** - Load experiment implementations from external DLLs without recompilation
- **Isolation modes** - Configure assembly load context isolation per plugin
- **Hot reload** - Watch plugin directories and reload assemblies on change
- **Plugin manifests** - Declarative metadata for plugin discovery and validation
- **Security** - Optional signature verification and sandboxing for untrusted plugins
- **DI integration** - Seamless integration with `Microsoft.Extensions.DependencyInjection`

## Isolation Modes

| Mode | Description |
|------|-------------|
| `Shared` | Plugin runs in the default load context (no isolation) |
| `Isolated` | Plugin runs in its own `AssemblyLoadContext` (default) |
| `Collectible` | Isolated context that supports unloading for hot reload |

## Plugin Manifest

Plugins can include a manifest file for metadata and dependency declarations:

```json
{
  "id": "my-plugin",
  "version": "1.0.0",
  "assembly": "MyPlugin.dll",
  "experiments": [
    {
      "serviceType": "IMyService",
      "implementationType": "MyPlugin.MyServiceImpl"
    }
  ]
}
```

## Documentation

See the [full documentation](../../docs/user-guide/plugins.md) for advanced configuration, security options, and plugin authoring guides.
