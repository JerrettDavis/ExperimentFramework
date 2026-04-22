# User Targeting

The Targeting package provides rule-based variant selection based on user attributes, enabling precise control over which users receive specific experiment variants.

## Installation

```bash
dotnet add package ExperimentFramework.Targeting
```

## Quick Start

```csharp
// 1. Register targeting support
services.AddExperimentTargeting();

// 2. Provide a targeting context provider
services.AddScoped<ITargetingContextProvider, HttpTargetingContextProvider>();

// 3. Configure targeting rules
services.AddExperimentTargetingRules(config =>
{
    config.ForExperiment("IPaymentProcessor")
        .WhenAttribute("plan", "enterprise").Use("enterprise-processor")
        .WhenAttribute("country", "US").Use("us-processor")
        .Otherwise().Use("default-processor");
});

// 4. Configure your experiment
var experiments = ExperimentFrameworkBuilder.Create()
    .Define<IPaymentProcessor>(exp => exp
        .UsingTargeting()
        .AddControl<DefaultPaymentProcessor>("default-processor")
        .AddCondition<EnterprisePaymentProcessor>("enterprise-processor")
        .AddCondition<USPaymentProcessor>("us-processor"));

services.AddExperimentFramework(experiments);
```

## How It Works

### Targeting Context

The targeting context provides user attributes for rule evaluation:

```csharp
public interface ITargetingContext
{
    string? UserId { get; }
    IReadOnlyDictionary<string, string> Attributes { get; }
}
```

### Context Provider

Implement `ITargetingContextProvider` to supply the targeting context:

```csharp
public class HttpTargetingContextProvider : ITargetingContextProvider
{
    private readonly IHttpContextAccessor _httpContext;

    public HttpTargetingContextProvider(IHttpContextAccessor httpContext)
    {
        _httpContext = httpContext;
    }

    public ValueTask<ITargetingContext?> GetContextAsync(CancellationToken cancellationToken = default)
    {
        var user = _httpContext.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return ValueTask.FromResult<ITargetingContext?>(null);

        var context = new SimpleTargetingContext
        {
            UserId = user.FindFirst("sub")?.Value,
            Attributes = new Dictionary<string, string>
            {
                ["plan"] = user.FindFirst("plan")?.Value ?? "free",
                ["country"] = user.FindFirst("country")?.Value ?? "unknown",
                ["role"] = user.FindFirst(ClaimTypes.Role)?.Value ?? "user"
            }
        };

        return ValueTask.FromResult<ITargetingContext?>(context);
    }
}
```

### Targeting Rules

Rules are evaluated in order - the first matching rule determines the variant:

```csharp
public interface ITargetingRule
{
    bool Evaluate(ITargetingContext context);
}
```

## Built-in Rules

### Attribute Matching

```csharp
services.AddExperimentTargetingRules(config =>
{
    config.ForExperiment("IFeatureService")
        // Exact match
        .WhenAttribute("tier", "premium").Use("premium-variant")
        // Multiple values (OR)
        .WhenAttribute("country", "US", "CA", "UK").Use("na-eu-variant")
        // Regex match
        .WhenAttributeMatches("email", @".*@company\.com$").Use("internal-variant")
        .Otherwise().Use("control");
});
```

### User ID Targeting

```csharp
config.ForExperiment("IBetaFeature")
    // Specific users
    .WhenUser("user-123", "user-456").Use("beta-variant")
    // Percentage of users
    .WhenUserPercentage(10).Use("beta-variant")
    .Otherwise().Use("control");
```

### Composite Rules

```csharp
config.ForExperiment("IAdvancedFeature")
    // AND logic
    .When(ctx => ctx.Attributes["plan"] == "enterprise"
               && ctx.Attributes["country"] == "US")
        .Use("us-enterprise")
    // Complex expressions
    .When(ctx =>
    {
        var signupDate = DateTime.Parse(ctx.Attributes["signup_date"]);
        return signupDate > DateTime.UtcNow.AddDays(-30);
    }).Use("new-user-variant")
    .Otherwise().Use("control");
```

## Configuration Options

### Programmatic Configuration

```csharp
services.AddExperimentTargeting(options =>
{
    options.DefaultVariantKey = "control";
    options.CacheRuleResults = true;
    options.CacheDuration = TimeSpan.FromMinutes(5);
});
```

### Fluent Builder

```csharp
.Define<IRecommendationEngine>(exp => exp
    .UsingTargeting(rules => rules
        .WhenAttribute("tier", "premium").Use("ml-recommendations")
        .WhenAttribute("tier", "pro").Use("collaborative-filtering")
        .Otherwise().Use("popular-items"))
    .AddControl<PopularItemsEngine>("popular-items")
    .AddCondition<CollaborativeFilteringEngine>("collaborative-filtering")
    .AddCondition<MLRecommendationEngine>("ml-recommendations"))
```

## Configuration File Support

Enable YAML/JSON configuration:

```csharp
services.AddExperimentTargetingConfiguration();
services.AddExperimentTargeting();
services.AddExperimentFrameworkFromConfiguration(configuration);
```

### YAML Configuration

```yaml
experimentFramework:
  trials:
    - serviceType: IFeatureService
      selectionMode:
        type: targeting
        rules:
          - attribute: plan
            value: enterprise
            variantKey: enterprise-features
          - attribute: country
            values: ["US", "CA"]
            variantKey: north-america
          - variantKey: default  # Otherwise clause
      variants:
        - key: default
          implementationType: StandardFeatureService
          isControl: true
        - key: enterprise-features
          implementationType: EnterpriseFeatureService
        - key: north-america
          implementationType: NorthAmericaFeatureService
```

## Real-World Examples

### Feature Gating by Plan

```csharp
services.AddExperimentTargetingRules(config =>
{
    config.ForExperiment("IAnalyticsService")
        .WhenAttribute("plan", "enterprise").Use("advanced-analytics")
        .WhenAttribute("plan", "pro").Use("standard-analytics")
        .Otherwise().Use("basic-analytics");
});

var experiments = ExperimentFrameworkBuilder.Create()
    .Define<IAnalyticsService>(exp => exp
        .UsingTargeting()
        .AddControl<BasicAnalyticsService>("basic-analytics")
        .AddCondition<StandardAnalyticsService>("standard-analytics")
        .AddCondition<AdvancedAnalyticsService>("advanced-analytics"));
```

### Regional Compliance

```csharp
services.AddExperimentTargetingRules(config =>
{
    config.ForExperiment("IDataProcessor")
        // GDPR regions
        .WhenAttribute("region", "EU", "UK").Use("gdpr-compliant")
        // CCPA
        .WhenAttribute("state", "CA").Use("ccpa-compliant")
        .Otherwise().Use("standard");
});
```

### Beta Testing

```csharp
public class BetaTesterRule : ITargetingRule
{
    private readonly HashSet<string> _betaTesters;

    public BetaTesterRule(IOptions<BetaConfig> config)
    {
        _betaTesters = config.Value.BetaTesterIds.ToHashSet();
    }

    public bool Evaluate(ITargetingContext context)
    {
        return context.UserId != null && _betaTesters.Contains(context.UserId);
    }
}

// Register custom rule
services.AddExperimentTargetingRules(config =>
{
    config.ForExperiment("INewFeature")
        .WhenRule<BetaTesterRule>().Use("beta")
        .Otherwise().Use("stable");
});
```

### A/B Testing by Cohort

```csharp
services.AddExperimentTargetingRules(config =>
{
    config.ForExperiment("ICheckoutFlow")
        // Internal testing first
        .WhenAttributeMatches("email", @".*@ourcompany\.com$").Use("new-checkout")
        // Then beta users
        .WhenAttribute("beta_enabled", "true").Use("new-checkout")
        // Then 20% of remaining traffic
        .WhenUserPercentage(20).Use("new-checkout")
        .Otherwise().Use("legacy-checkout");
});
```

## Combining with Other Selection Modes

Targeting can be combined with other selection modes for complex scenarios:

```csharp
// Use targeting first, fall back to rollout for unmatched users
.Define<ISearchService>(exp => exp
    .UsingTargeting(rules => rules
        .WhenAttribute("plan", "enterprise").Use("enterprise-search"))
    .WithFallback(fallback => fallback
        .UsingRollout(percentage: 25, includedKey: "new-search", excludedKey: "legacy"))
    .AddControl<LegacySearchService>("legacy")
    .AddCondition<NewSearchService>("new-search")
    .AddCondition<EnterpriseSearchService>("enterprise-search"))
```

## Best Practices

1. **Order rules by specificity**: More specific rules should come first
2. **Always include a fallback**: Use `.Otherwise()` to handle unmatched cases
3. **Cache context when possible**: Reduce overhead for frequently accessed attributes
4. **Validate attributes**: Ensure expected attributes are always present
5. **Monitor rule matches**: Track which rules are matching for debugging

## Troubleshooting

### Rules not matching as expected

**Symptom**: Users are falling through to the default variant unexpectedly.

**Cause**: Attribute names or values don't match exactly.

**Solution**: Log the targeting context to verify attribute values:

```csharp
public async ValueTask<ITargetingContext?> GetContextAsync(CancellationToken cancellationToken = default)
{
    var context = BuildContext();
    _logger.LogDebug("Targeting context: UserId={UserId}, Attributes={@Attributes}",
        context.UserId, context.Attributes);
    return context;
}
```

### Missing attributes

**Symptom**: NullReferenceException when evaluating rules.

**Cause**: Attribute not present in context.

**Solution**: Use safe dictionary access:

```csharp
// Instead of
ctx.Attributes["plan"] == "enterprise"

// Use
ctx.Attributes.TryGetValue("plan", out var plan) && plan == "enterprise"
```

### Context not available

**Symptom**: All requests use the default variant.

**Cause**: Context provider returning null.

**Solution**: Handle unauthenticated users with sensible defaults:

```csharp
public ValueTask<ITargetingContext?> GetContextAsync(CancellationToken cancellationToken = default)
{
    var user = _httpContext.HttpContext?.User;

    // Provide context even for anonymous users
    var context = new SimpleTargetingContext
    {
        UserId = user?.FindFirst("sub")?.Value ?? "anonymous",
        Attributes = new Dictionary<string, string>
        {
            ["authenticated"] = (user?.Identity?.IsAuthenticated ?? false).ToString().ToLower(),
            ["plan"] = user?.FindFirst("plan")?.Value ?? "free"
        }
    };

    return ValueTask.FromResult<ITargetingContext?>(context);
}
```
