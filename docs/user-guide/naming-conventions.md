# Naming Conventions

ExperimentFramework uses naming conventions to determine feature flag names and configuration keys when you don't explicitly provide them. This guide explains the default behavior and how to customize it.

## Default Naming Conventions

When you define an experiment without specifying a selector name, the framework applies default naming conventions based on the selection mode.

### Feature Flag Names

For boolean and variant feature flags, the default name is the service type name:

```csharp
// Explicit name
.Define<IDatabase>(c => c
    .UsingFeatureFlag("UseCloudDb")
    .AddDefaultTrial<LocalDatabase>("false")
    .AddTrial<CloudDatabase>("true"))

// Convention-based name (uses "IDatabase")
.Define<IDatabase>(c => c
    .UsingFeatureFlag()  // No name provided
    .AddDefaultTrial<LocalDatabase>("false")
    .AddTrial<CloudDatabase>("true"))
```

Configuration in `appsettings.json`:

```json
{
  "FeatureManagement": {
    "IDatabase": false
  }
}
```

### Configuration Keys

For configuration value selection, the default key is `Experiments:{ServiceTypeName}`:

```csharp
// Explicit key
.Define<ICache>(c => c
    .UsingConfigurationKey("Cache:Provider")
    .AddDefaultTrial<InMemoryCache>("inmemory")
    .AddTrial<RedisCache>("redis"))

// Convention-based key (uses "Experiments:ICache")
.Define<ICache>(c => c
    .UsingConfigurationKey()  // No key provided
    .AddDefaultTrial<InMemoryCache>("inmemory")
    .AddTrial<RedisCache>("redis"))
```

Configuration in `appsettings.json`:

```json
{
  "Experiments": {
    "ICache": "redis"
  }
}
```

### Sticky Routing Names

For sticky routing, the default name is the service type name (used for hashing):

```csharp
// Explicit name
.Define<IRecommendationEngine>(c => c
    .UsingStickyRouting("RecommendationExperiment")
    .AddDefaultTrial<ContentBased>("control")
    .AddTrial<CollaborativeFiltering>("variant-a"))

// Convention-based name (uses "IRecommendationEngine")
.Define<IRecommendationEngine>(c => c
    .UsingStickyRouting()  // No name provided
    .AddDefaultTrial<ContentBased>("control")
    .AddTrial<CollaborativeFiltering>("variant-a"))
```

## Custom Naming Conventions

Implement `IExperimentNamingConvention` to define custom naming patterns for your experiments.

### IExperimentNamingConvention Interface

```csharp
public interface IExperimentNamingConvention
{
    string FeatureFlagNameFor(Type serviceType);
    string VariantFlagNameFor(Type serviceType);
    string ConfigurationKeyFor(Type serviceType);
}
```

### Example: Prefix-Based Convention

Create a convention that adds prefixes to all selector names:

```csharp
public class PrefixedNamingConvention : IExperimentNamingConvention
{
    private readonly string _prefix;

    public PrefixedNamingConvention(string prefix)
    {
        _prefix = prefix;
    }

    public string FeatureFlagNameFor(Type serviceType)
    {
        return $"{_prefix}.{serviceType.Name}";
    }

    public string VariantFlagNameFor(Type serviceType)
    {
        return $"{_prefix}.Variants.{serviceType.Name}";
    }

    public string ConfigurationKeyFor(Type serviceType)
    {
        return $"{_prefix}:Config:{serviceType.Name}";
    }
}
```

Register the custom convention:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .UseNamingConvention(new PrefixedNamingConvention("MyApp"))
    .Define<IDatabase>(c => c
        .UsingFeatureFlag()  // Uses "MyApp.IDatabase"
        .AddDefaultTrial<LocalDatabase>("false")
        .AddTrial<CloudDatabase>("true"));
```

Configuration in `appsettings.json`:

```json
{
  "FeatureManagement": {
    "MyApp.IDatabase": false
  }
}
```

### Example: Environment-Aware Convention

Create a convention that includes the environment in selector names:

```csharp
public class EnvironmentNamingConvention : IExperimentNamingConvention
{
    private readonly string _environment;

    public EnvironmentNamingConvention(string environment)
    {
        _environment = environment;
    }

    public string FeatureFlagNameFor(Type serviceType)
    {
        return $"{_environment}_{serviceType.Name}";
    }

    public string VariantFlagNameFor(Type serviceType)
    {
        return $"{_environment}_{serviceType.Name}_Variants";
    }

    public string ConfigurationKeyFor(Type serviceType)
    {
        return $"Experiments:{_environment}:{serviceType.Name}";
    }
}
```

Register with environment from configuration:

```csharp
var environment = builder.Configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";

var experiments = ExperimentFrameworkBuilder.Create()
    .UseNamingConvention(new EnvironmentNamingConvention(environment))
    .Define<IDatabase>(c => c
        .UsingFeatureFlag()  // Uses "Production_IDatabase"
        .AddDefaultTrial<LocalDatabase>("false")
        .AddTrial<CloudDatabase>("true"));
```

### Example: Kebab-Case Convention

Create a convention that converts type names to kebab-case:

```csharp
public class KebabCaseNamingConvention : IExperimentNamingConvention
{
    public string FeatureFlagNameFor(Type serviceType)
    {
        return ToKebabCase(serviceType.Name);
    }

    public string VariantFlagNameFor(Type serviceType)
    {
        return $"{ToKebabCase(serviceType.Name)}-variants";
    }

    public string ConfigurationKeyFor(Type serviceType)
    {
        return $"experiments:{ToKebabCase(serviceType.Name)}";
    }

    private static string ToKebabCase(string input)
    {
        // Remove leading 'I' if it's an interface
        if (input.StartsWith("I") && input.Length > 1 && char.IsUpper(input[1]))
        {
            input = input.Substring(1);
        }

        var result = new StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (i > 0 && char.IsUpper(c))
            {
                result.Append('-');
            }
            result.Append(char.ToLowerInvariant(c));
        }
        return result.ToString();
    }
}
```

Usage:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .UseNamingConvention(new KebabCaseNamingConvention())
    .Define<IPaymentProcessor>(c => c
        .UsingFeatureFlag()  // Uses "payment-processor"
        .AddDefaultTrial<StripePayment>("false")
        .AddTrial<NewPaymentProvider>("true"));
```

Configuration in `appsettings.json`:

```json
{
  "FeatureManagement": {
    "payment-processor": false
  }
}
```

## Convention-Based Configuration

Using naming conventions enables convention-based configuration patterns.

### Organizing Feature Flags by Service

With the default convention, feature flags naturally group by service:

```json
{
  "FeatureManagement": {
    "IDatabase": false,
    "ICache": true,
    "IPaymentProcessor": false,
    "IRecommendationEngine": true
  }
}
```

### Hierarchical Configuration

Use custom conventions to create hierarchical configuration structures:

```csharp
public class HierarchicalNamingConvention : IExperimentNamingConvention
{
    public string FeatureFlagNameFor(Type serviceType)
    {
        return $"Features.{ExtractCategory(serviceType)}.{serviceType.Name}";
    }

    public string VariantFlagNameFor(Type serviceType)
    {
        return $"Variants.{ExtractCategory(serviceType)}.{serviceType.Name}";
    }

    public string ConfigurationKeyFor(Type serviceType)
    {
        return $"Experiments:{ExtractCategory(serviceType)}:{serviceType.Name}";
    }

    private static string ExtractCategory(Type serviceType)
    {
        var name = serviceType.Name;
        if (name.EndsWith("Database") || name.EndsWith("Repository"))
            return "DataAccess";
        if (name.EndsWith("Cache"))
            return "Caching";
        if (name.EndsWith("Payment") || name.EndsWith("Processor"))
            return "Payments";
        return "General";
    }
}
```

Configuration in `appsettings.json`:

```json
{
  "FeatureManagement": {
    "Features": {
      "DataAccess": {
        "IDatabase": false,
        "IUserRepository": true
      },
      "Caching": {
        "ICache": false
      },
      "Payments": {
        "IPaymentProcessor": true
      }
    }
  }
}
```

## Best Practices

### 1. Be Consistent

Use the same naming convention throughout your application:

```csharp
// Good: All experiments use the same convention
var experiments = ExperimentFrameworkBuilder.Create()
    .UseNamingConvention(new KebabCaseNamingConvention())
    .Define<IDatabase>(c => c.UsingFeatureFlag()...)
    .Define<ICache>(c => c.UsingFeatureFlag()...)
    .Define<IPaymentProcessor>(c => c.UsingFeatureFlag()...);

// Bad: Mixing conventions
.Define<IDatabase>(c => c.UsingFeatureFlag("UseCloudDb")...)  // Explicit
.Define<ICache>(c => c.UsingFeatureFlag()...)                 // Convention
```

### 2. Document Your Convention

When using a custom convention, document it clearly:

```csharp
/// <summary>
/// Application naming convention for experiments.
///
/// Feature flags: {Environment}_{ServiceTypeName}
/// Variant flags: {Environment}_{ServiceTypeName}_Variants
/// Config keys: Experiments:{Environment}:{ServiceTypeName}
///
/// Example: Production_IDatabase
/// </summary>
public class AppNamingConvention : IExperimentNamingConvention
{
    // Implementation
}
```

### 3. Validate Convention Output

Ensure your convention produces valid configuration keys:

```csharp
public class ValidatedNamingConvention : IExperimentNamingConvention
{
    private readonly IExperimentNamingConvention _inner;

    public ValidatedNamingConvention(IExperimentNamingConvention inner)
    {
        _inner = inner;
    }

    public string FeatureFlagNameFor(Type serviceType)
    {
        var name = _inner.FeatureFlagNameFor(serviceType);
        ValidateName(name, "feature flag");
        return name;
    }

    public string VariantFlagNameFor(Type serviceType)
    {
        var name = _inner.VariantFlagNameFor(serviceType);
        ValidateName(name, "variant flag");
        return name;
    }

    public string ConfigurationKeyFor(Type serviceType)
    {
        var key = _inner.ConfigurationKeyFor(serviceType);
        ValidateConfigKey(key);
        return key;
    }

    private static void ValidateName(string name, string type)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException($"Naming convention produced empty {type} name.");

        if (name.Length > 100)
            throw new InvalidOperationException($"Naming convention produced {type} name longer than 100 characters: {name}");
    }

    private static void ValidateConfigKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Naming convention produced empty configuration key.");

        if (key.Contains(".."))
            throw new InvalidOperationException($"Configuration key contains invalid '..' sequence: {key}");
    }
}
```

### 4. Consider Multi-Tenant Scenarios

For multi-tenant applications, include tenant context in the convention:

```csharp
public class TenantNamingConvention : IExperimentNamingConvention
{
    private readonly ITenantProvider _tenantProvider;

    public TenantNamingConvention(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    public string FeatureFlagNameFor(Type serviceType)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        return $"{tenantId}_{serviceType.Name}";
    }

    public string VariantFlagNameFor(Type serviceType)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        return $"{tenantId}_{serviceType.Name}_Variants";
    }

    public string ConfigurationKeyFor(Type serviceType)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        return $"Tenants:{tenantId}:Experiments:{serviceType.Name}";
    }
}
```

### 5. Test Your Convention

Write tests to ensure your convention produces expected names:

```csharp
[Fact]
public void Convention_produces_kebab_case_names()
{
    var convention = new KebabCaseNamingConvention();

    var flagName = convention.FeatureFlagNameFor(typeof(IPaymentProcessor));
    Assert.Equal("payment-processor", flagName);

    var configKey = convention.ConfigurationKeyFor(typeof(IUserRepository));
    Assert.Equal("experiments:user-repository", configKey);
}
```

## Migrating from Explicit Names

If you have existing experiments with explicit names, you can migrate gradually:

### Step 1: Implement Convention Matching Current Names

```csharp
public class LegacyNamingConvention : IExperimentNamingConvention
{
    public string FeatureFlagNameFor(Type serviceType)
    {
        // Match existing explicit names
        return serviceType.Name switch
        {
            "IDatabase" => "UseCloudDb",
            "ICache" => "UseRedisCache",
            _ => serviceType.Name
        };
    }

    public string VariantFlagNameFor(Type serviceType)
    {
        return serviceType.Name;
    }

    public string ConfigurationKeyFor(Type serviceType)
    {
        return $"Experiments:{serviceType.Name}";
    }
}
```

### Step 2: Remove Explicit Names

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .UseNamingConvention(new LegacyNamingConvention())
    // Before: .Define<IDatabase>(c => c.UsingFeatureFlag("UseCloudDb")...)
    // After:
    .Define<IDatabase>(c => c.UsingFeatureFlag()...)  // Uses convention
    .Define<ICache>(c => c.UsingFeatureFlag()...);
```

### Step 3: Gradually Transition to New Convention

Once all explicit names are removed, switch to the new convention:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .UseNamingConvention(new KebabCaseNamingConvention())
    .Define<IDatabase>(c => c.UsingFeatureFlag()...)
    .Define<ICache>(c => c.UsingFeatureFlag()...);
```

Update configuration to match new names:

```json
{
  "FeatureManagement": {
    "database": false,
    "cache": true
  }
}
```

## Next Steps

- [Advanced Topics](advanced.md) - Build custom components and advanced patterns
- [Samples](samples.md) - See complete examples using naming conventions
- [Telemetry](telemetry.md) - Selector names appear in telemetry tags
