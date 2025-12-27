# Plugin System

The ExperimentFramework.Plugins package enables dynamic loading of experimental implementations from external DLL assemblies at runtime. This allows you to deploy new experiments without rebuilding your main application.

## Overview

The plugin system provides:

- **Dynamic Assembly Loading**: Load plugin DLLs at runtime using .NET's `AssemblyLoadContext`
- **Configurable Isolation**: Full, Shared, or None isolation modes
- **Manifest System**: Declare plugin metadata and services via JSON, embedded resources, or attributes
- **Hot Reload**: Automatically reload plugins when files change
- **YAML DSL Integration**: Reference plugin types using `plugin:PluginId/alias` syntax

## Installation

```bash
dotnet add package ExperimentFramework.Plugins
```

## Quick Start

### 1. Register Plugin Services

```csharp
using ExperimentFramework.Plugins;

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

### 2. Load Plugins Manually

```csharp
var pluginManager = serviceProvider.GetRequiredService<IPluginManager>();

// Load a specific plugin
var context = await pluginManager.LoadAsync("./plugins/MyPlugin.dll");

Console.WriteLine($"Loaded: {context.Manifest.Name} v{context.Manifest.Version}");

// Resolve types from the plugin
var type = context.GetTypeByAlias("my-impl");
var instance = context.CreateInstance(type, serviceProvider);
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
      debounceMs: 500

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

## Plugin Manifest

Every plugin needs a manifest that declares its metadata and services. The manifest can be provided in four ways:

### Option 1: Source Generator (Recommended)

The easiest approach is to use the source generator, which automatically discovers all public classes implementing non-system interfaces and generates the manifest at compile time.

```bash
dotnet add package ExperimentFramework.Plugins.Generators
```

In your `.csproj`:

```xml
<ItemGroup>
  <!-- Reference the Plugins package for manifest attributes -->
  <PackageReference Include="ExperimentFramework.Plugins" PrivateAssets="compile" />

  <!-- Reference the source generator -->
  <PackageReference Include="ExperimentFramework.Plugins.Generators" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

That's it! The generator will:
- **Discover** all public concrete classes implementing non-system interfaces
- **Generate aliases** from class names (e.g., `StripeV2Processor` → `stripe-v2`)
- **Use assembly metadata** for plugin ID, name, and version

#### Customizing the Generated Manifest

To customize the manifest metadata, add the `GeneratePluginManifest` attribute:

```csharp
using ExperimentFramework.Plugins.Manifest;

[assembly: GeneratePluginManifest(
    Id = "Acme.PaymentExperiments",
    Name = "Acme Payment Experiments Plugin",
    Description = "Experimental payment processors")]
```

#### Customizing Implementation Aliases

Override the auto-generated alias for specific classes:

```csharp
using ExperimentFramework.Plugins.Manifest;

[PluginImplementation(Alias = "stripe-v2")]
public class StripeProcessorVersion2 : IPaymentProcessor { }
```

#### Excluding Classes from Discovery

Exclude specific classes from the manifest:

```csharp
[PluginImplementation(Exclude = true)]
public class InternalHelper : IPaymentProcessor { }
```

### Option 2: Embedded JSON Resource

Create a `plugin.manifest.json` file and embed it as a resource:

```json
{
  "manifestVersion": "1.0",
  "plugin": {
    "id": "Acme.PaymentExperiments",
    "name": "Acme Payment Experiments Plugin",
    "version": "1.0.0",
    "description": "Experimental payment processors"
  },
  "isolation": {
    "mode": "shared",
    "sharedAssemblies": [
      "ExperimentFramework",
      "Microsoft.Extensions.DependencyInjection.Abstractions"
    ]
  },
  "services": [
    {
      "interface": "IPaymentProcessor",
      "implementations": [
        { "type": "Acme.Payments.StripeV2Processor", "alias": "stripe-v2" },
        { "type": "Acme.Payments.AdyenProcessor", "alias": "adyen" }
      ]
    }
  ],
  "lifecycle": {
    "supportsHotReload": true
  }
}
```

In your `.csproj`:

```xml
<ItemGroup>
  <EmbeddedResource Include="plugin.manifest.json">
    <LogicalName>plugin.manifest.json</LogicalName>
  </EmbeddedResource>
</ItemGroup>
```

### Option 2: Adjacent JSON File

Place a `{AssemblyName}.plugin.json` file next to the DLL:

```
plugins/
  MyPlugin.dll
  MyPlugin.plugin.json
```

### Option 3: Assembly Attributes

Use attributes in your plugin code:

```csharp
using ExperimentFramework.Plugins.Manifest;

[assembly: PluginManifest("Acme.PaymentExperiments",
    Name = "Acme Payment Experiments",
    Version = "1.0.0",
    Description = "Experimental payment processors",
    SupportsHotReload = true)]

[assembly: PluginIsolation(
    Mode = PluginIsolationMode.Shared,
    SharedAssemblies = new[] { "ExperimentFramework" })]
```

## Isolation Modes

The plugin system supports three isolation modes:

| Mode | Behavior | Use Case |
|------|----------|----------|
| `Full` | Separate AssemblyLoadContext, no shared types | Untrusted plugins, version conflicts |
| `Shared` | Shares specified assemblies with host | Most common - allows DI integration |
| `None` | Loads into default context | Fully trusted plugins, maximum compatibility |

### Shared Mode (Default)

In `Shared` mode, the plugin shares certain assemblies with the host application. This allows:

- Dependency injection integration
- Shared interfaces between host and plugin
- Smaller plugin sizes (no need to bundle shared dependencies)

Default shared assemblies:
- ExperimentFramework
- ExperimentFramework.Configuration
- ExperimentFramework.Plugins
- Microsoft.Extensions.DependencyInjection.Abstractions
- Microsoft.Extensions.Logging.Abstractions

### Full Isolation

Use `Full` mode when:
- You need to load different versions of the same library
- Plugins are untrusted and need sandboxing
- You want complete separation

```json
{
  "isolation": {
    "mode": "full"
  }
}
```

### No Isolation

Use `None` mode for trusted plugins that need complete access to host types:

```json
{
  "isolation": {
    "mode": "none"
  }
}
```

**Warning**: Plugins loaded with `None` mode cannot be unloaded.

## Type Resolution

Reference plugin types using the `plugin:` prefix:

```
plugin:PluginId/alias       # By alias from manifest
plugin:PluginId/Full.Type   # By full type name
```

### In YAML Configuration

```yaml
conditions:
  - key: stripe-v2
    implementationType: plugin:Acme.Payments/stripe-v2
```

### In Code

```csharp
// Via PluginManager
var type = pluginManager.ResolveType("plugin:Acme.Payments/stripe-v2");

// Via PluginContext
var context = pluginManager.GetPlugin("Acme.Payments");
var type = context.GetTypeByAlias("stripe-v2");

// Create instance
var instance = context.CreateInstance(type, serviceProvider);
```

## Hot Reload

Enable hot reload to automatically reload plugins when files change:

```csharp
builder.Services.AddExperimentPluginsWithHotReload(opts =>
{
    opts.DiscoveryPaths.Add("./plugins");
    opts.HotReloadDebounceMs = 500;
});
```

Or via YAML:

```yaml
experimentFramework:
  plugins:
    hotReload:
      enabled: true
      debounceMs: 500
```

### Events

Subscribe to reload events:

```csharp
var pluginManager = serviceProvider.GetRequiredService<IPluginManager>();

pluginManager.PluginLoaded += (sender, args) =>
{
    Console.WriteLine($"Loaded: {args.Context.Manifest.Id}");
};

pluginManager.PluginUnloaded += (sender, args) =>
{
    Console.WriteLine($"Unloaded: {args.Context.Manifest.Id}");
};

pluginManager.PluginLoadFailed += (sender, args) =>
{
    Console.WriteLine($"Failed: {args.PluginPath} - {args.Exception.Message}");
};
```

### Manifest Lifecycle

Plugins can declare whether they support hot reload:

```json
{
  "lifecycle": {
    "supportsHotReload": true,
    "requiresRestartOnUnload": false
  }
}
```

## Discovery

Configure automatic plugin discovery:

```csharp
builder.Services.AddExperimentPlugins(opts =>
{
    // Direct file paths
    opts.DiscoveryPaths.Add("./plugins/MyPlugin.dll");

    // Directory paths (scans for *.dll)
    opts.DiscoveryPaths.Add("./plugins");

    // Glob patterns
    opts.DiscoveryPaths.Add("./plugins/**/*.plugin.dll");

    // Auto-load on startup
    opts.AutoLoadOnStartup = true;
});
```

## Plugin Manager API

### IPluginManager

```csharp
public interface IPluginManager : IAsyncDisposable
{
    // Events
    event EventHandler<PluginEventArgs>? PluginLoaded;
    event EventHandler<PluginEventArgs>? PluginUnloaded;
    event EventHandler<PluginLoadFailedEventArgs>? PluginLoadFailed;

    // Query
    IReadOnlyList<IPluginContext> GetLoadedPlugins();
    IPluginContext? GetPlugin(string pluginId);
    bool IsLoaded(string pluginId);

    // Lifecycle
    Task<IPluginContext> LoadAsync(string path, PluginLoadOptions? options = null);
    Task UnloadAsync(string pluginId);
    Task<IPluginContext> ReloadAsync(string pluginId);
    Task<IReadOnlyList<IPluginContext>> DiscoverAndLoadAsync();

    // Type resolution
    Type? ResolveType(string typeReference);
}
```

### IPluginContext

```csharp
public interface IPluginContext : IAsyncDisposable
{
    string ContextId { get; }
    IPluginManifest Manifest { get; }
    bool IsLoaded { get; }
    string PluginPath { get; }
    Assembly? MainAssembly { get; }
    IReadOnlyList<Assembly> LoadedAssemblies { get; }

    Type? GetType(string typeName);
    Type? GetTypeByAlias(string alias);
    IEnumerable<Type> GetImplementations(Type interfaceType);
    IEnumerable<Type> GetImplementations<TInterface>();

    object CreateInstance(Type type, IServiceProvider serviceProvider);
    object? CreateInstanceByAlias(string alias, IServiceProvider serviceProvider);
}
```

## Creating a Plugin

### 1. Create Plugin Project

```bash
dotnet new classlib -n MyPlugin
cd MyPlugin
dotnet add package ExperimentFramework.Plugins
dotnet add package ExperimentFramework.Plugins.Generators
```

### 2. Configure Project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <ItemGroup>
    <!-- Reference the Plugins package (compile-only, shared with host) -->
    <PackageReference Include="ExperimentFramework.Plugins">
      <Private>false</Private>
      <ExcludeAssets>runtime</ExcludeAssets>
    </PackageReference>

    <!-- Source generator for auto-generating manifest -->
    <PackageReference Include="ExperimentFramework.Plugins.Generators"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

### 3. (Optional) Add Custom Metadata

```csharp
using ExperimentFramework.Plugins.Manifest;

// Optional: customize plugin metadata
[assembly: GeneratePluginManifest(
    Id = "MyCompany.MyPlugin",
    Name = "My Plugin",
    Description = "My experimental implementations")]
```

### 4. Implement Service

```csharp
namespace MyCompany.MyPlugin;

// Automatically discovered and registered with alias "my-service-impl"
public class MyServiceImpl : IMyService
{
    public Task<string> DoWorkAsync()
    {
        return Task.FromResult("Hello from plugin!");
    }
}
```

The source generator will automatically:
- Discover `MyServiceImpl` implementing `IMyService`
- Generate alias `my-service-impl` from the class name
- Create the manifest with services registration

### 5. Build and Deploy

```bash
dotnet build
cp bin/Debug/net10.0/MyPlugin.dll ../host-app/plugins/
```

### Using Manual Manifest (Alternative)

If you prefer manual control, create a `plugin.manifest.json`:

```json
{
  "manifestVersion": "1.0",
  "plugin": {
    "id": "MyCompany.MyPlugin",
    "name": "My Plugin",
    "version": "1.0.0"
  },
  "services": [
    {
      "interface": "IMyService",
      "implementations": [
        { "type": "MyCompany.MyPlugin.MyServiceImpl", "alias": "my-impl" }
      ]
    }
  ]
}
```

And embed it in your project:

```xml
<ItemGroup>
  <EmbeddedResource Include="plugin.manifest.json">
    <LogicalName>plugin.manifest.json</LogicalName>
  </EmbeddedResource>
</ItemGroup>
```

## Enterprise Build Pipeline

The plugin system enables sophisticated deployment scenarios:

```
Build Pipeline:
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  Main App Repo  │     │ Experiment Repo  │     │ Plugin Artifact │
│  (stable code)  │     │ (new impl DLLs)  │     │    Repository   │
└────────┬────────┘     └────────┬─────────┘     └────────┬────────┘
         │                       │                        │
         │                       ▼                        │
         │              ┌────────────────┐                │
         │              │  CI/CD builds  │                │
         │              │  plugin DLLs   │                │
         │              └───────┬────────┘                │
         │                      │                         │
         │                      ▼                         │
         │              ┌────────────────┐                │
         │              │ Push to artifact│───────────────┤
         │              │   repository   │                │
         │              └────────────────┘                │
         ▼                                                ▼
┌────────────────────────────────────────────────────────────────┐
│                     Production App                              │
│  ┌──────────────┐    ┌─────────────────┐   ┌────────────────┐  │
│  │  YAML Config │───►│  Plugin Loader  │◄──│ Plugin DLLs    │  │
│  │  (experiments)│   │  (hot reload)   │   │ (from artifact)│  │
│  └──────────────┘    └─────────────────┘   └────────────────┘  │
└────────────────────────────────────────────────────────────────┘
```

**Benefits:**
- Experiment implementations developed in separate repos
- Main application never needs rebuilding
- A/B test new implementations in production
- Roll back by removing plugin DLL
- Version experiments independently

## Configuration Reference

### PluginConfigurationOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DiscoveryPaths` | `List<string>` | `[]` | Paths to search for plugins |
| `DefaultIsolationMode` | `PluginIsolationMode` | `Shared` | Default isolation mode |
| `DefaultSharedAssemblies` | `List<string>` | `[]` | Additional assemblies to share |
| `EnableHotReload` | `bool` | `false` | Enable file watching |
| `HotReloadDebounceMs` | `int` | `500` | Debounce interval for hot reload |
| `AutoLoadOnStartup` | `bool` | `true` | Auto-discover and load plugins |
| `ForceIsolation` | `bool` | `false` | Force full isolation for all plugins |
| `EnableUnloading` | `bool` | `true` | Enable collectible contexts |

### YAML Configuration

```yaml
experimentFramework:
  plugins:
    discovery:
      paths:
        - "./plugins"
        - "./plugins/**/*.plugin.dll"
    defaults:
      isolationMode: shared
      sharedAssemblies:
        - ExperimentFramework
        - Microsoft.Extensions.DependencyInjection.Abstractions
    hotReload:
      enabled: true
      debounceMs: 500
```

## Security

The plugin system includes several security features to protect your application from malicious or untrusted plugins.

### Path Restrictions

By default, plugins can be loaded from any path. For production environments, restrict plugin loading to specific directories:

```csharp
builder.Services.AddExperimentPlugins(opts =>
{
    // Only allow plugins from these directories
    opts.AllowedPluginDirectories.Add("./plugins");
    opts.AllowedPluginDirectories.Add("/opt/myapp/plugins");

    // Disable UNC paths (network shares) - default is false
    opts.AllowUncPaths = false;
});
```

Via YAML:

```yaml
experimentFramework:
  plugins:
    security:
      allowedDirectories:
        - "./plugins"
        - "/opt/myapp/plugins"
      allowUncPaths: false
```

### Assembly Signature Verification

For high-security environments, require plugins to be signed with Authenticode:

```csharp
builder.Services.AddExperimentPlugins(opts =>
{
    // Require all plugins to be signed
    opts.RequireSignedAssemblies = true;

    // Optionally, restrict to specific trusted publishers
    opts.TrustedPublisherThumbprints.Add("ABCDEF1234567890...");
});
```

### Manifest Size Limits

Protect against denial-of-service attacks via malformed manifests:

```csharp
builder.Services.AddExperimentPlugins(opts =>
{
    // Limit manifest file size (default: 1MB)
    opts.MaxManifestSizeBytes = 512 * 1024; // 512KB

    // Limit JSON nesting depth (default: 32)
    opts.MaxManifestJsonDepth = 16;
});
```

### Security Best Practices

1. **Use path restrictions**: Always configure `AllowedPluginDirectories` in production
2. **Disable UNC paths**: Keep `AllowUncPaths = false` unless required
3. **Use signed assemblies**: For enterprise deployments, use `RequireSignedAssemblies = true`
4. **Validate plugin sources**: Only deploy plugins from trusted CI/CD pipelines
5. **Use Shared isolation**: Avoid `None` isolation mode for untrusted plugins
6. **Enable audit logging**: Set `EnableAuditLogging = true` to track plugin operations
7. **Monitor file watchers**: Be aware that hot reload watches the filesystem

### Isolation Mode Security

| Mode | Security Level | Use Case |
|------|---------------|----------|
| `Full` | Highest | Untrusted plugins, maximum isolation |
| `Shared` | Medium | Default - allows DI integration, shares specified assemblies |
| `None` | Lowest | Only for fully trusted code, cannot be unloaded |

**Warning**: Plugins loaded with `None` isolation mode have full access to the host application's memory and cannot be unloaded. Only use this mode for plugins that are part of your trusted codebase.

## Production Deployment

This section covers essential considerations for deploying the plugin system in production environments.

### Health Monitoring

The plugin system provides a health check API to monitor plugin status:

```csharp
var pluginManager = serviceProvider.GetRequiredService<IPluginManager>();
var health = await pluginManager.GetHealthAsync();

// Check overall system health
if (health.State == PluginSystemHealthState.Unhealthy)
{
    logger.LogError("Plugin system is unhealthy: {Message}", health.Message);
}

// Check individual plugin status
foreach (var plugin in health.Plugins)
{
    if (plugin.State != PluginHealthState.Healthy)
    {
        logger.LogWarning(
            "Plugin {PluginId} is {State}: {Message}",
            plugin.PluginId, plugin.State, plugin.Message);
    }
}

// Check for failed load attempts
foreach (var failure in health.FailedLoads)
{
    logger.LogError(
        "Failed to load plugin from {Path}: {Error}",
        failure.PluginPath, failure.ErrorMessage);
}
```

### ASP.NET Core Health Check Integration

Integrate plugin health with ASP.NET Core health checks:

```csharp
public class PluginHealthCheck : IHealthCheck
{
    private readonly IPluginManager _pluginManager;

    public PluginHealthCheck(IPluginManager pluginManager)
    {
        _pluginManager = pluginManager;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var health = await _pluginManager.GetHealthAsync(cancellationToken);

        return health.State switch
        {
            PluginSystemHealthState.Healthy => HealthCheckResult.Healthy(health.Message),
            PluginSystemHealthState.Degraded => HealthCheckResult.Degraded(health.Message),
            _ => HealthCheckResult.Unhealthy(health.Message)
        };
    }
}

// Registration
services.AddHealthChecks()
    .AddCheck<PluginHealthCheck>("plugins");
```

### Production Configuration Checklist

Before deploying to production, review this checklist:

**Security Configuration:**
- [ ] Set `AllowedPluginDirectories` to restrict plugin loading locations
- [ ] Enable `RequireSignedAssemblies = true` for enterprise deployments
- [ ] Configure `TrustedPublisherThumbprints` with your organization's certificate thumbprints
- [ ] Disable `AllowUncPaths` unless required (default: disabled)
- [ ] Use `Shared` or `Full` isolation mode (avoid `None` for untrusted plugins)

**Validation Configuration:**
- [ ] Set appropriate `MaxManifestSizeBytes` (default: 1MB)
- [ ] Set appropriate `MaxManifestJsonDepth` (default: 32)
- [ ] Enable `EnableAuditLogging = true` for compliance scenarios

**Hot Reload Configuration:**
- [ ] Disable `EnableHotReload` in production unless required
- [ ] If enabled, set appropriate `HotReloadDebounceMs` (default: 500ms)
- [ ] Ensure file system watchers don't impact performance

**Example Production Configuration:**

```csharp
services.AddExperimentPlugins(opts =>
{
    // Restrict plugin loading to specific directories
    opts.AllowedPluginDirectories.Add("/app/plugins");

    // Require signed assemblies
    opts.RequireSignedAssemblies = true;
    opts.TrustedPublisherThumbprints.Add("YOUR_CERTIFICATE_THUMBPRINT");

    // Disable hot reload in production
    opts.EnableHotReload = false;

    // Use shared isolation for DI integration
    opts.DefaultIsolationMode = PluginIsolationMode.Shared;

    // Enable audit logging
    opts.EnableAuditLogging = true;

    // Set manifest limits
    opts.MaxManifestSizeBytes = 1024 * 1024; // 1MB
    opts.MaxManifestJsonDepth = 32;
});
```

### Performance Tuning

**Assembly Loading:**
- Use `Shared` isolation mode to reduce memory footprint when plugins share common dependencies
- Pre-load plugins at startup rather than on-demand to avoid runtime latency
- Use `EnableUnloading = false` if plugins are never reloaded (better performance)

**Memory Management:**
- Monitor `AssemblyCount` and `TypeCount` from health checks
- When unloading plugins, call `GC.Collect()` to reclaim memory from collectible contexts
- Be aware that collectible context unloading is non-deterministic

**Hot Reload (if enabled):**
- Set `HotReloadDebounceMs` appropriately to avoid excessive reloads
- Monitor file watcher resource usage on systems with many plugins

### Operational Guidance

**Deployment:**
1. Deploy plugins to designated directories only
2. Use your CI/CD pipeline to sign and validate plugins
3. Version plugins independently from the main application
4. Keep plugin manifests version-controlled

**Monitoring:**
- Monitor plugin health endpoint regularly
- Alert on `PluginSystemHealthState.Unhealthy`
- Track `FailedLoads` to catch deployment issues
- Log plugin load/unload events for debugging

**Incident Response:**
1. Use `GetHealthAsync()` to diagnose issues
2. Check logs for security or manifest validation errors
3. Verify plugin paths and signatures
4. Try reloading problematic plugins: `await pluginManager.ReloadAsync(pluginId);`

## Troubleshooting

### Plugin Not Loading

1. **Check file exists**: Verify the DLL path is correct
2. **Check manifest**: Ensure manifest is valid JSON with required fields
3. **Check isolation**: Try `None` mode to debug loading issues
4. **Check dependencies**: Ensure all dependencies are available
5. **Check security**: Verify plugin path is in `AllowedPluginDirectories`
6. **Check signature**: If `RequireSignedAssemblies` is enabled, ensure the plugin is signed

### Type Not Found

1. **Check alias**: Verify the alias matches the manifest
2. **Check namespace**: Use full type name if alias doesn't work
3. **Check assembly loading**: Verify the assembly was loaded successfully
4. **Check loader exceptions**: Look for `ReflectionTypeLoadException` details in logs

### Hot Reload Not Working

1. **Check lifecycle**: Ensure `supportsHotReload: true` in manifest
2. **Check file watcher**: Verify directory exists and is accessible
3. **Check debounce**: Wait for debounce interval to elapse
4. **Check file locks**: Ensure build process releases file locks before reload

### Memory Leaks

1. **Dispose contexts**: Always dispose plugin contexts when done
2. **Use collectible mode**: Set `EnableUnloading: true`
3. **Trigger GC**: Call `GC.Collect()` after unloading
4. **Note**: Collectible context unloading is non-deterministic; some references may prevent unload

### Security Errors

1. **SecurityException: Plugin path not in allowed directories**: Add the path to `AllowedPluginDirectories`
2. **SecurityException: UNC paths not allowed**: Set `AllowUncPaths = true` or use a local path
3. **SecurityException: Assembly not signed**: Sign the plugin with Authenticode
4. **SecurityException: Untrusted publisher**: Add the certificate thumbprint to `TrustedPublisherThumbprints`

## Next Steps

- [Configuration Guide](configuration.md) - Full YAML/JSON configuration reference
- [Extensibility](extensibility.md) - Create custom selection mode providers
- [Getting Started](getting-started.md) - Basic framework setup
