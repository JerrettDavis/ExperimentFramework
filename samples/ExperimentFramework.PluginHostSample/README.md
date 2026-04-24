# ExperimentFramework.PluginHostSample

Demonstrates the ExperimentFramework plugin system: loading experiment implementations from an external DLL at runtime, resolving types by alias, and unloading the plugin cleanly.

## What this sample demonstrates

- `AddExperimentPlugins()` — registering the plugin infrastructure with DI
- `IPluginManager.LoadAsync()` — loading a DLL from a directory path at runtime
- Type resolution by alias using the `plugin:PluginId/alias` YAML DSL syntax
- Isolation modes (`PluginIsolationMode.Shared`, `Full`, `None`)
- `IPluginManager.UnloadAsync()` — unloading an assembly load context at runtime

The sample plugin (`ExperimentFramework.SamplePlugin`) exposes three payment processor implementations — `stripe-v2`, `adyen`, and `mollie` — as separate types keyed by alias inside a plugin manifest.

## How to run

**The `SamplePlugin` DLL must be built before running the host.** The host locates the plugin by a relative path from its own output directory; if the DLL is absent it exits immediately with a "Plugin not found" message.

```bash
# Step 1 — build the plugin
dotnet build samples/ExperimentFramework.SamplePlugin

# Step 2 — run the host
dotnet run --project samples/ExperimentFramework.PluginHostSample
```

In Rider, build `ExperimentFramework.SamplePlugin` first (right-click > Build), then set `ExperimentFramework.PluginHostSample` as startup and run.

## Expected output

```
============================================================
  ExperimentFramework Plugin System Demo
============================================================

Step 1: Loading plugin from disk...
Loaded: Acme.PaymentExperiments v1.0.0
Plugin ID: Acme.PaymentExperiments
...

Step 2: Discovering plugin services...
Interface: IPaymentProcessor
  - stripe-v2
  - adyen
  - mollie

Step 3: Using plugin types...
Using Stripe v2 ...:
  Success: True
  Transaction: ...
...

Step 4: Plugin manager features...
Loaded plugins: 1
Is plugin loaded: True
Type reference syntax for YAML configuration:
  - plugin:Acme.PaymentExperiments/stripe-v2
  ...

Step 5: Unloading plugin...
Is plugin loaded: False
Plugin unloaded successfully!

============================================================
  Demo Complete!
============================================================
```

## Where to read more

- [Plugin System reference](../../docs/reference/plugins.md) — full plugin system documentation including manifest format, isolation modes, hot reload, and YAML DSL integration
- [Extensibility Guide](../../docs/reference/extensibility.md) — custom selection mode providers
- [ExperimentFramework.SamplePlugin](../ExperimentFramework.SamplePlugin/) — the plugin DLL used by this sample (payment processor implementations with a `PluginMetadata` manifest)
