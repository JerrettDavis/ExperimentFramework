# Selection Modes

Selection modes determine how the framework chooses which condition to execute for each method call. The framework supports multiple selection modes through a modular architecture.

## Overview

| Mode | Package | Use Case | Selection Criteria |
|------|---------|----------|-------------------|
| Boolean Feature Flag | Core | Simple on/off experiments | IFeatureManager enabled state |
| Configuration Value | Core | Multi-variant selection | IConfiguration key value |
| Variant Feature Flag | `ExperimentFramework.FeatureManagement` | Targeted rollouts | IVariantFeatureManager variant name |
| Sticky Routing | `ExperimentFramework.StickyRouting` | A/B testing by user | Hash of user identity |
| OpenFeature | `ExperimentFramework.OpenFeature` | External flag management | OpenFeature provider evaluation |

## Built-in vs Extension Modes

**Built-in modes** (Boolean Feature Flag, Configuration Value) are included in the core `ExperimentFramework` package.

**Extension modes** (Variant Feature Flag, Sticky Routing, OpenFeature) are provided via separate NuGet packages. Install only what you need to keep your dependencies minimal.

## Boolean Feature Flag

Boolean feature flags provide simple on/off switching between two implementations based on feature flag state.

### When to Use

- Testing a new implementation against the current one
- Gradual rollout to a percentage of users
- Enabling features for specific user segments
- Quick rollback capability

### Configuration

Define the experiment using `UsingFeatureFlag()`:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IPaymentProcessor>(t => t
        .UsingFeatureFlag("UseNewPaymentProvider")
        .AddControl<StripePayment>("false")
        .AddVariant<NewPaymentProvider>("true"));

services.AddExperimentFramework(experiments);
```

Configure the feature flag in `appsettings.json`:

```json
{
  "FeatureManagement": {
    "UseNewPaymentProvider": false
  }
}
```

### Feature Management Integration

The framework integrates with Microsoft.FeatureManagement, which provides advanced capabilities:

**Percentage Rollout**: Enable for a percentage of users

```json
{
  "FeatureManagement": {
    "UseNewPaymentProvider": {
      "EnabledFor": [
        {
          "Name": "Microsoft.Percentage",
          "Parameters": {
            "Value": 25
          }
        }
      ]
    }
  }
}
```

**Time Windows**: Enable during specific time periods

```json
{
  "FeatureManagement": {
    "UseNewPaymentProvider": {
      "EnabledFor": [
        {
          "Name": "Microsoft.TimeWindow",
          "Parameters": {
            "Start": "2024-01-01T00:00:00Z",
            "End": "2024-01-31T23:59:59Z"
          }
        }
      ]
    }
  }
}
```

**Targeting**: Enable for specific users or groups

```json
{
  "FeatureManagement": {
    "UseNewPaymentProvider": {
      "EnabledFor": [
        {
          "Name": "Microsoft.Targeting",
          "Parameters": {
            "Audience": {
              "Users": ["alice@example.com", "bob@example.com"],
              "Groups": [
                {
                  "Name": "BetaTesters",
                  "RolloutPercentage": 50
                }
              ]
            }
          }
        }
      ]
    }
  }
}
```

### Request-Scoped Consistency

The framework uses `IFeatureManagerSnapshot` for scoped services, ensuring consistent feature evaluation within a request:

```csharp
using (var scope = serviceProvider.CreateScope())
{
    var payment = scope.ServiceProvider.GetRequiredService<IPaymentProcessor>();

    // All calls within this scope see the same feature flag value
    await payment.AuthorizeAsync(100m);
    await payment.ChargeAsync(100m);
    await payment.CaptureAsync();
}
```

## Configuration Value

Configuration values enable multi-variant selection based on a string configuration value.

### When to Use

- Testing more than two implementations
- Environment-specific selection (dev/staging/production)
- Runtime configuration changes
- Feature variations based on deployment

### Configuration

Define the experiment using `UsingConfigurationKey()`:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IRecommendationEngine>(t => t
        .UsingConfigurationKey("Recommendations:Algorithm")
        .AddControl<ContentBased>("")
        .AddCondition<CollaborativeFiltering>("collaborative")
        .AddCondition<HybridRecommendations>("hybrid")
        .AddCondition<MLRecommendations>("ml"));

services.AddExperimentFramework(experiments);
```

Configure the selection in `appsettings.json`:

```json
{
  "Recommendations": {
    "Algorithm": "collaborative"
  }
}
```

### Empty Value Behavior

When the configuration key is missing or empty, the control condition is used:

```csharp
.AddControl<ContentBased>("")  // Used when key is missing or empty
```

### Runtime Configuration Changes

If your configuration source supports reloading, changes take effect on the next method call:

```csharp
builder.Configuration.AddJsonFile("appsettings.json",
    optional: false,
    reloadOnChange: true);
```

The next method invocation will read the updated configuration value and select the appropriate condition.

### Environment-Specific Configuration

Use configuration value selection for environment-specific behavior:

```json
// appsettings.Development.json
{
  "Cache": {
    "Provider": "inmemory"
  }
}

// appsettings.Production.json
{
  "Cache": {
    "Provider": "redis"
  }
}
```

```csharp
.Trial<ICache>(t => t
    .UsingConfigurationKey("Cache:Provider")
    .AddControl<InMemoryCache>("inmemory")
    .AddCondition<RedisCache>("redis"))
```

## Variant Feature Flag

Variant feature flags integrate with `IVariantFeatureManager` to support multi-variant experiments with sophisticated targeting.

> **Package Required**: This selection mode requires the `ExperimentFramework.FeatureManagement` package.

### When to Use

- Multi-variant experiments (A/B/C/D testing)
- Gradual rollout across multiple variants
- Targeted delivery of specific variants to user segments
- Complex allocation strategies

### Installation

```bash
dotnet add package ExperimentFramework.FeatureManagement
dotnet add package Microsoft.FeatureManagement
```

### Configuration

Register the provider and define the experiment:

```csharp
// Register the variant feature flag provider
services.AddExperimentVariantFeatureFlags();
services.AddFeatureManagement();

// Define experiments
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IEmailSender>(t => t
        .UsingVariantFeatureFlag("EmailProvider")
        .AddControl<SmtpSender>("smtp")
        .AddVariant<SendGridSender>("sendgrid")
        .AddVariant<MailgunSender>("mailgun")
        .AddVariant<AmazonSesSender>("ses"));

services.AddExperimentFramework(experiments);
```

Configure variants in `appsettings.json`:

```json
{
  "FeatureManagement": {
    "EmailProvider": {
      "EnabledFor": [
        {
          "Name": "Microsoft.Targeting",
          "Parameters": {
            "Audience": {
              "Users": ["user1@example.com"],
              "Groups": [
                {
                  "Name": "BetaTesters",
                  "RolloutPercentage": 100
                }
              ],
              "DefaultRolloutPercentage": 0
            }
          }
        }
      ],
      "Variants": [
        {
          "Name": "smtp",
          "ConfigurationValue": "smtp",
          "StatusOverride": "Disabled"
        },
        {
          "Name": "sendgrid",
          "ConfigurationValue": "sendgrid",
          "ConfigurationReference": "EmailProvider-SendGrid"
        },
        {
          "Name": "mailgun",
          "ConfigurationValue": "mailgun",
          "ConfigurationReference": "EmailProvider-Mailgun"
        },
        {
          "Name": "ses",
          "ConfigurationValue": "ses",
          "ConfigurationReference": "EmailProvider-SES"
        }
      ],
      "Allocation": {
        "DefaultWhenEnabled": "sendgrid",
        "User": [
          {
            "Variant": "ses",
            "Users": ["alice@example.com"]
          }
        ],
        "Group": [
          {
            "Variant": "mailgun",
            "Groups": ["BetaTesters"],
            "RolloutPercentage": 50
          },
          {
            "Variant": "sendgrid",
            "Groups": ["BetaTesters"],
            "RolloutPercentage": 50
          }
        ]
      }
    }
  }
}
```

### Variant Allocation

The variant feature manager selects which variant a user receives based on:

- User-specific assignments
- Group membership and rollout percentages within groups
- Default variant when enabled but no specific allocation matches

### Graceful Degradation

If `IVariantFeatureManager` is not available or returns null, the experiment uses the control condition:

```csharp
// Variant manager not installed or returns null
// -> Uses SmtpSender (control condition)
```

This allows the framework to work without a hard dependency on variant support.

### CancellationToken Propagation

The framework automatically extracts `CancellationToken` from method parameters and passes it to the variant manager:

```csharp
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken);
}

// CancellationToken is automatically forwarded to IVariantFeatureManager
var result = await emailSender.SendAsync("user@example.com", "Subject", "Body", ct);
```

## Sticky Routing

Sticky routing provides deterministic condition selection based on user identity, ensuring the same user always sees the same condition.

> **Package Required**: This selection mode requires the `ExperimentFramework.StickyRouting` package.

### When to Use

- A/B testing where users must consistently see the same variant
- Session-based experiments
- User-segmented experiments
- Avoiding variant flipping during a user session

### Installation

```bash
dotnet add package ExperimentFramework.StickyRouting
```

### How It Works

Sticky routing uses a SHA256 hash of the user identity and selector name to deterministically select a condition:

1. Get user identity from `IExperimentIdentityProvider`
2. Compute: `hash = SHA256(identity + ":" + selectorName)`
3. Select condition: `conditions[hash % conditionCount]`

The same identity always produces the same hash, ensuring consistent condition selection.

### Identity Provider

Implement `IExperimentIdentityProvider` to provide user identity:

```csharp
using ExperimentFramework.StickyRouting;

public class UserIdentityProvider : IExperimentIdentityProvider
{
    private readonly IHttpContextAccessor _httpContext;

    public UserIdentityProvider(IHttpContextAccessor httpContext)
    {
        _httpContext = httpContext;
    }

    public bool TryGetIdentity(out string identity)
    {
        var userId = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            identity = userId;
            return true;
        }

        identity = string.Empty;
        return false;
    }
}
```

### Configuration

Register the provider and identity provider, then define the experiment:

```csharp
// Register sticky routing provider
services.AddExperimentStickyRouting();

// Register your identity provider
services.AddScoped<IExperimentIdentityProvider, UserIdentityProvider>();

// Define experiments
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IRecommendationEngine>(t => t
        .UsingStickyRouting("RecommendationExperiment")
        .AddControl<ContentBased>("control")
        .AddCondition<CollaborativeFiltering>("variant-a")
        .AddCondition<HybridRecommendations>("variant-b"));

services.AddExperimentFramework(experiments);
```

### Distribution Across Conditions

Sticky routing distributes users evenly across conditions based on hash distribution:

```
Users: user1, user2, user3, user4, user5, user6

Condition Keys (sorted): control, variant-a, variant-b

Distribution:
- user1 -> hash % 3 = 0 -> control
- user2 -> hash % 3 = 1 -> variant-a
- user3 -> hash % 3 = 2 -> variant-b
- user4 -> hash % 3 = 0 -> control
- user5 -> hash % 3 = 1 -> variant-a
- user6 -> hash % 3 = 2 -> variant-b
```

The distribution is approximately even across all conditions.

### Fallback Behavior

If `IExperimentIdentityProvider` is not registered or returns no identity, sticky routing falls back to the default (control) condition:

```csharp
// No identity provider registered or TryGetIdentity returns false
// -> Uses the control condition
```

### Consistency Guarantees

Sticky routing provides strong consistency guarantees:

- Same user + same experiment = same condition (always)
- Different users = distributed across conditions
- Changing condition keys or order will change user assignments

### Condition Key Ordering

Condition keys are sorted alphabetically before hashing to ensure deterministic behavior:

```csharp
// These produce the same results regardless of registration order
.AddControl<A>("alpha")
.AddCondition<B>("beta")
.AddCondition<C>("charlie")

// Internally sorted to: ["alpha", "beta", "charlie"]
```

### Multi-Tenant Scenarios

For multi-tenant applications, include tenant ID in the identity:

```csharp
public class TenantUserIdentityProvider : IExperimentIdentityProvider
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IHttpContextAccessor _httpContext;

    public bool TryGetIdentity(out string identity)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var userId = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(userId))
        {
            identity = $"{tenantId}:{userId}";
            return true;
        }

        identity = string.Empty;
        return false;
    }
}
```

This ensures users in different tenants can be assigned to different conditions.

## OpenFeature

OpenFeature integration allows routing based on any OpenFeature-compatible feature flag provider.

> **Package Required**: This selection mode requires the `ExperimentFramework.OpenFeature` package.

### When to Use

- Using external feature flag services (LaunchDarkly, Flagsmith, CloudBees, etc.)
- Standardized feature flag management across multiple platforms
- Vendor-agnostic feature flag evaluation
- Existing OpenFeature infrastructure

### Installation

```bash
dotnet add package ExperimentFramework.OpenFeature
dotnet add package OpenFeature
```

### Configuration

Register the provider, configure OpenFeature, and define experiments:

```csharp
// Register OpenFeature provider
services.AddExperimentOpenFeature();

// Configure OpenFeature provider at startup
await Api.Instance.SetProviderAsync(new YourOpenFeatureProvider());

// Define experiments
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IPaymentProcessor>(t => t
        .UsingOpenFeature("payment-processor")
        .AddControl<StripeProcessor>("stripe")
        .AddVariant<PayPalProcessor>("paypal")
        .AddVariant<SquareProcessor>("square"));

services.AddExperimentFramework(experiments);
```

### Flag Key Naming

When no flag key is specified, the framework generates a kebab-case name:

| Service Type | Generated Flag Key |
|--------------|-------------------|
| `IPaymentProcessor` | `payment-processor` |
| `IUserService` | `user-service` |

### Boolean vs String Flags

The framework automatically detects the flag type:

**Boolean flags** (conditions are "true" and "false"):
- Uses `GetBooleanValueAsync()`

**String flags** (multi-variant):
- Uses `GetStringValueAsync()`

### Fallback Behavior

If OpenFeature is not configured or evaluation fails, the control condition is used.

For detailed configuration and provider examples, see the [OpenFeature Integration Guide](openfeature.md).

## Choosing a Selection Mode

Use this decision tree to choose the right selection mode:

```
Do you need user-specific consistency?
├─ Yes: Use Sticky Routing
└─ No:
    └─ Using external feature flag service (LaunchDarkly, Flagsmith, etc.)?
        ├─ Yes: Use OpenFeature
        └─ No:
            └─ How many variants?
                ├─ Two: Use Boolean Feature Flag
                └─ More than two:
                    └─ Need advanced targeting (user segments, groups, etc.)?
                        ├─ Yes: Use Variant Feature Flag
                        └─ No: Use Configuration Value
```

## Combining Multiple Experiments

You can define multiple experiments on different services:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IDatabase>(t => t
        .UsingFeatureFlag("UseCloudDb")
        .AddControl<LocalDatabase>("false")
        .AddVariant<CloudDatabase>("true"))
    .Trial<ICache>(t => t
        .UsingConfigurationKey("Cache:Provider")
        .AddControl<InMemoryCache>("inmemory")
        .AddCondition<RedisCache>("redis"))
    .Trial<IRecommendationEngine>(t => t
        .UsingStickyRouting("RecommendationExperiment")
        .AddControl<ContentBased>("control")
        .AddCondition<CollaborativeFiltering>("variant-a"));

services.AddExperimentFramework(experiments);
```

Each experiment operates independently with its own selection mode and configuration.

## Custom Selection Modes

Need a selection mode that isn't built-in? Create your own with minimal boilerplate:

```csharp
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
        var value = await _redis.GetDatabase().StringGetAsync(context.SelectorName);
        return value.HasValue ? value.ToString() : null;
    }
}

// Register with one line
services.AddSelectionModeProvider<RedisSelectionProvider>();

// Use with UsingCustomMode
.Trial<ICache>(t => t
    .UsingCustomMode("Redis", "cache:provider")
    .AddControl<MemoryCache>()
    .AddCondition<RedisCache>("redis"))
```

See the [Extensibility Guide](extensibility.md) for complete details on creating custom providers.

## Next Steps

- [Error Handling](error-handling.md) - Handle failures in experimental implementations
- [Extensibility](extensibility.md) - Create custom selection mode providers
- [Naming Conventions](naming-conventions.md) - Customize how feature flags and config keys are named
- [Samples](samples.md) - See complete examples of each selection mode
