# Extensibility Guide

ExperimentFramework uses a provider-based architecture that allows you to create custom selection modes. This guide explains how to implement and register your own selection mode providers with minimal boilerplate.

## Quick Start

Creating a custom selection mode requires just two things:

1. A provider class with the `[SelectionMode]` attribute
2. A single line to register it

```csharp
// 1. Create your provider
[SelectionMode("Redis")]
public class RedisSelectionProvider : SelectionModeProviderBase
{
    private readonly IConnectionMultiplexer _redis;

    public RedisSelectionProvider(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public override async ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(context.SelectorName);
        return value.HasValue ? value.ToString() : null;
    }
}

// 2. Register it (one line!)
services.AddSelectionModeProvider<RedisSelectionProvider>();

// 3. Use it
.Trial<ICache>(t => t
    .UsingCustomMode("Redis", "cache:provider")
    .AddControl<MemoryCache>()
    .AddCondition<RedisCache>("redis"))
```

That's it! No factory classes, no manual wiring.

## Architecture Overview

The extensibility system consists of these components:

| Component | Purpose |
|-----------|---------|
| `[SelectionMode]` | Attribute that declares the mode identifier |
| `SelectionModeProviderBase` | Abstract base class that reduces boilerplate |
| `SelectionModeProviderFactory<T>` | Generic factory that eliminates custom factory classes |
| `AddSelectionModeProvider<T>()` | Extension method for simple registration |
| `ISelectionModeProvider` | Core interface (for advanced scenarios) |
| `SelectionModeRegistry` | Thread-safe registry that manages providers |

## Creating a Custom Selection Mode

### Option 1: Using the Base Class (Recommended)

The simplest approach uses `SelectionModeProviderBase`:

```csharp
using ExperimentFramework.Selection;

[SelectionMode("Redis")]
public class RedisSelectionProvider : SelectionModeProviderBase
{
    private readonly IConnectionMultiplexer _redis;

    // Dependencies are injected automatically
    public RedisSelectionProvider(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public override async ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(context.SelectorName);

            if (value.HasValue)
            {
                var key = value.ToString();
                // Verify the key exists in registered trials
                if (context.TrialKeys.Contains(key))
                {
                    return key;
                }
            }
        }
        catch
        {
            // Fall through to default on any error
        }

        // Return null to use the default trial
        return null;
    }

    // Optional: Override to customize default selector naming
    public override string GetDefaultSelectorName(Type serviceType, IExperimentNamingConvention convention)
        => $"experiments:{serviceType.Name.ToLowerInvariant()}";
}
```

**Key benefits:**
- `ModeIdentifier` is automatically derived from the `[SelectionMode]` attribute
- `GetDefaultSelectorName` has a sensible default implementation
- Dependencies are injected via constructor

### Option 2: Implementing the Interface Directly

For advanced scenarios, implement `ISelectionModeProvider` directly:

```csharp
[SelectionMode("Redis")]
public class RedisSelectionProvider : ISelectionModeProvider
{
    private readonly IConnectionMultiplexer _redis;

    public RedisSelectionProvider(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public string ModeIdentifier => "Redis";

    public async ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
    {
        // Selection logic...
    }

    public string GetDefaultSelectorName(Type serviceType, IExperimentNamingConvention convention)
        => convention.FeatureFlagNameFor(serviceType);
}
```

## Registration

### Simple Registration

For most providers, use the generic extension method:

```csharp
services.AddSelectionModeProvider<RedisSelectionProvider>();
```

This:
- Reads the mode identifier from the `[SelectionMode]` attribute
- Creates a `SelectionModeProviderFactory<T>` automatically
- Uses `ActivatorUtilities` to create instances with DI support

### Explicit Mode Identifier

Override the attribute if needed:

```csharp
services.AddSelectionModeProvider<RedisSelectionProvider>("CustomRedis");
```

### Creating Extension Methods (Optional)

For a polished package, create extension methods:

```csharp
namespace ExperimentFramework.Redis;

public static class RedisModes
{
    public const string Redis = "Redis";
}

public static class ExperimentBuilderExtensions
{
    public static ServiceExperimentBuilder<T> UsingRedis<T>(
        this ServiceExperimentBuilder<T> builder,
        string? keyName = null)
        where T : class
        => builder.UsingCustomMode(RedisModes.Redis, keyName);
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddExperimentRedis(this IServiceCollection services)
        => services.AddSelectionModeProvider<RedisSelectionProvider>();
}
```

## SelectionContext

The `SelectionContext` provides all information needed for selection:

```csharp
public sealed class SelectionContext
{
    /// <summary>Scoped service provider for resolving dependencies.</summary>
    public required IServiceProvider ServiceProvider { get; init; }

    /// <summary>Selector name (e.g., feature flag name, config key).</summary>
    public required string SelectorName { get; init; }

    /// <summary>All registered trial keys for this experiment.</summary>
    public required IReadOnlyList<string> TrialKeys { get; init; }

    /// <summary>Default (control) key to use when selection fails.</summary>
    public required string DefaultKey { get; init; }

    /// <summary>Service interface type being experimented on.</summary>
    public required Type ServiceType { get; init; }
}
```

## Best Practices

### Return null for Fallback

Return `null` from `SelectTrialKeyAsync` to indicate the default trial should be used:

```csharp
public override async ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
{
    var selectedKey = await EvaluateAsync(context);

    // Return null to use default trial
    if (string.IsNullOrEmpty(selectedKey))
        return null;

    // Only return keys that are actually registered
    if (!context.TrialKeys.Contains(selectedKey))
        return null;

    return selectedKey;
}
```

### Handle Errors Gracefully

Always catch exceptions and fall back to default:

```csharp
public override async ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
{
    try
    {
        return await DoSelectionAsync(context);
    }
    catch (Exception ex)
    {
        _logger?.LogWarning(ex, "Selection failed for {Selector}", context.SelectorName);
        return null; // Fall back to default
    }
}
```

### Use Constructor Injection

Dependencies are automatically injected when using `AddSelectionModeProvider<T>()`:

```csharp
[SelectionMode("MyMode")]
public class MyProvider : SelectionModeProviderBase
{
    private readonly ILogger<MyProvider> _logger;
    private readonly IHttpContextAccessor _httpContext;

    // Dependencies injected automatically
    public MyProvider(ILogger<MyProvider> logger, IHttpContextAccessor httpContext)
    {
        _logger = logger;
        _httpContext = httpContext;
    }

    public override ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
    {
        // Use injected dependencies...
    }
}
```

## Source Generator Integration

Custom selection modes work automatically with both runtime proxies and source-generated proxies:

- **Runtime proxies**: Provider is invoked directly at runtime
- **Source-generated proxies**: Generator emits code that delegates to the provider via the registry

The generator recognizes `UsingCustomMode()` calls and generates appropriate delegation code.

## Built-in Provider Examples

For reference, see the extension package providers:

| Provider | Package | Description |
|----------|---------|-------------|
| `VariantFeatureFlagProvider` | `ExperimentFramework.FeatureManagement` | Multi-variant selection via IVariantFeatureManager |
| `StickyRoutingProvider` | `ExperimentFramework.StickyRouting` | Deterministic selection via identity hashing |
| `OpenFeatureProvider` | `ExperimentFramework.OpenFeature` | Vendor-neutral evaluation via OpenFeature SDK |

## Testing Custom Providers

```csharp
[Fact]
public async Task Provider_selects_correct_trial()
{
    // Arrange
    var redis = CreateMockRedis(returning: "variant-a");
    var provider = new RedisSelectionProvider(redis);
    var context = new SelectionContext
    {
        ServiceProvider = new ServiceCollection().BuildServiceProvider(),
        SelectorName = "my-experiment",
        TrialKeys = new[] { "control", "variant-a", "variant-b" },
        DefaultKey = "control",
        ServiceType = typeof(IMyService)
    };

    // Act
    var result = await provider.SelectTrialKeyAsync(context);

    // Assert
    Assert.Equal("variant-a", result);
}

[Fact]
public async Task Provider_returns_null_on_error()
{
    // Arrange
    var redis = CreateThrowingMockRedis();
    var provider = new RedisSelectionProvider(redis);
    var context = CreateTestContext();

    // Act
    var result = await provider.SelectTrialKeyAsync(context);

    // Assert
    Assert.Null(result); // Should fall back to default
}
```

## Advanced: Custom Factory

For special scenarios requiring custom creation logic, implement `ISelectionModeProviderFactory`:

```csharp
public class CustomProviderFactory : ISelectionModeProviderFactory
{
    public string ModeIdentifier => "Custom";

    public ISelectionModeProvider Create(IServiceProvider scopedProvider)
    {
        // Custom creation logic with access to scoped services
        var config = scopedProvider.GetRequiredService<IConfiguration>();
        var connectionString = config["Redis:ConnectionString"];
        var redis = ConnectionMultiplexer.Connect(connectionString);
        return new RedisSelectionProvider(redis);
    }
}

// Register manually
services.AddSingleton<ISelectionModeProviderFactory, CustomProviderFactory>();
```

This is rarely needed since `AddSelectionModeProvider<T>()` handles most scenarios automatically.
