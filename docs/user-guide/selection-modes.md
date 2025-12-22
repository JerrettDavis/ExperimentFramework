# Selection Modes

Selection modes determine how the framework chooses which trial to execute for each method call. The framework supports four selection modes, each suited to different use cases.

## Overview

| Mode | Use Case | Selection Criteria |
|------|----------|-------------------|
| Boolean Feature Flag | Simple on/off experiments | IFeatureManager enabled state |
| Configuration Value | Multi-variant selection | IConfiguration key value |
| Variant Feature Flag | Targeted rollouts | IVariantFeatureManager variant name |
| Sticky Routing | A/B testing by user | Hash of user identity |

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
    .Define<IPaymentProcessor>(c => c
        .UsingFeatureFlag("UseNewPaymentProvider")
        .AddDefaultTrial<StripePayment>("false")
        .AddTrial<NewPaymentProvider>("true"));

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
    .Define<IRecommendationEngine>(c => c
        .UsingConfigurationKey("Recommendations:Algorithm")
        .AddDefaultTrial<ContentBased>("")
        .AddTrial<CollaborativeFiltering>("collaborative")
        .AddTrial<HybridRecommendations>("hybrid")
        .AddTrial<MLRecommendations>("ml"));

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

When the configuration key is missing or empty, the default trial is used:

```csharp
.AddDefaultTrial<ContentBased>("")  // Used when key is missing or empty
```

### Runtime Configuration Changes

If your configuration source supports reloading, changes take effect on the next method call:

```csharp
builder.Configuration.AddJsonFile("appsettings.json",
    optional: false,
    reloadOnChange: true);
```

The next method invocation will read the updated configuration value and select the appropriate trial.

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
.Define<ICache>(c => c
    .UsingConfigurationKey("Cache:Provider")
    .AddDefaultTrial<InMemoryCache>("inmemory")
    .AddTrial<RedisCache>("redis"))
```

## Variant Feature Flag

Variant feature flags integrate with `IVariantFeatureManager` to support multi-variant experiments with sophisticated targeting.

### When to Use

- Multi-variant experiments (A/B/C/D testing)
- Gradual rollout across multiple variants
- Targeted delivery of specific variants to user segments
- Complex allocation strategies

### Prerequisites

Variant feature flags require Microsoft.FeatureManagement with variant support:

```bash
dotnet add package Microsoft.FeatureManagement
```

The framework detects `IVariantFeatureManager` via reflection. If it's not available, the experiment falls back to the default trial.

### Configuration

Define the experiment using `UsingVariantFeatureFlag()`:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .Define<IEmailSender>(c => c
        .UsingVariantFeatureFlag("EmailProvider")
        .AddDefaultTrial<SmtpSender>("smtp")
        .AddTrial<SendGridSender>("sendgrid")
        .AddTrial<MailgunSender>("mailgun")
        .AddTrial<AmazonSesSender>("ses"));

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

If `IVariantFeatureManager` is not available or returns null, the experiment uses the default trial:

```csharp
// Variant manager not installed or returns null
// -> Uses SmtpSender (default trial)
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

Sticky routing provides deterministic trial selection based on user identity, ensuring the same user always sees the same trial.

### When to Use

- A/B testing where users must consistently see the same variant
- Session-based experiments
- User-segmented experiments
- Avoiding variant flipping during a user session

### How It Works

Sticky routing uses a SHA256 hash of the user identity and selector name to deterministically select a trial:

1. Get user identity from `IExperimentIdentityProvider`
2. Compute: `hash = SHA256(identity + ":" + selectorName)`
3. Select trial: `trials[hash % trialCount]`

The same identity always produces the same hash, ensuring consistent trial selection.

### Identity Provider

Implement `IExperimentIdentityProvider` to provide user identity:

```csharp
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

Register the identity provider:

```csharp
services.AddScoped<IExperimentIdentityProvider, UserIdentityProvider>();
```

### Configuration

Define the experiment using `UsingStickyRouting()`:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .Define<IRecommendationEngine>(c => c
        .UsingStickyRouting("RecommendationExperiment")
        .AddDefaultTrial<ContentBased>("control")
        .AddTrial<CollaborativeFiltering>("variant-a")
        .AddTrial<HybridRecommendations>("variant-b"));

services.AddExperimentFramework(experiments);
```

### Distribution Across Trials

Sticky routing distributes users evenly across trials based on hash distribution:

```
Users: user1, user2, user3, user4, user5, user6

Trial Keys (sorted): control, variant-a, variant-b

Distribution:
- user1 -> hash % 3 = 0 -> control
- user2 -> hash % 3 = 1 -> variant-a
- user3 -> hash % 3 = 2 -> variant-b
- user4 -> hash % 3 = 0 -> control
- user5 -> hash % 3 = 1 -> variant-a
- user6 -> hash % 3 = 2 -> variant-b
```

The distribution is approximately even across all trials.

### Fallback Behavior

If `IExperimentIdentityProvider` is not registered or returns no identity, sticky routing falls back to boolean feature flag selection:

```csharp
// No identity provider registered
// -> Falls back to checking feature flag "RecommendationExperiment"
// -> Uses trial based on flag state (true/false)
```

Configure the fallback feature flag:

```json
{
  "FeatureManagement": {
    "RecommendationExperiment": true
  }
}
```

### Consistency Guarantees

Sticky routing provides strong consistency guarantees:

- Same user + same experiment = same trial (always)
- Different users = distributed across trials
- Changing trial keys or order will change user assignments

### Trial Key Ordering

Trial keys are sorted alphabetically before hashing to ensure deterministic behavior:

```csharp
// These produce the same results regardless of registration order
.AddDefaultTrial<A>("alpha")
.AddTrial<B>("beta")
.AddTrial<C>("charlie")

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

This ensures users in different tenants can be assigned to different trials.

## Choosing a Selection Mode

Use this decision tree to choose the right selection mode:

```
Do you need user-specific consistency?
├─ Yes: Use Sticky Routing
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
    .Define<IDatabase>(c => c
        .UsingFeatureFlag("UseCloudDb")
        .AddDefaultTrial<LocalDatabase>("false")
        .AddTrial<CloudDatabase>("true"))
    .Define<ICache>(c => c
        .UsingConfigurationKey("Cache:Provider")
        .AddDefaultTrial<InMemoryCache>("inmemory")
        .AddTrial<RedisCache>("redis"))
    .Define<IRecommendationEngine>(c => c
        .UsingStickyRouting("RecommendationExperiment")
        .AddDefaultTrial<ContentBased>("control")
        .AddTrial<CollaborativeFiltering>("variant-a"));

services.AddExperimentFramework(experiments);
```

Each experiment operates independently with its own selection mode and configuration.

## Next Steps

- [Error Handling](error-handling.md) - Handle failures in experimental implementations
- [Naming Conventions](naming-conventions.md) - Customize how feature flags and config keys are named
- [Samples](samples.md) - See complete examples of each selection mode
