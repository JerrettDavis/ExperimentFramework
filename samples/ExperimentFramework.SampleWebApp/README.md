# ExperimentFramework Web Application Sample

This sample demonstrates using the ExperimentFramework in an ASP.NET Core web application with:
- **Sticky A/B testing** for consistent user experiences across sessions (requires `ExperimentFramework.StickyRouting` package)
- **Fluent API source generation** using `.UseSourceGenerators()`
- **Session-based routing** for anonymous users
- **Feature flag experiments** for gradual rollouts

## Prerequisites

This sample requires the sticky routing extension package:

```bash
dotnet add package ExperimentFramework.StickyRouting
```

## Running the Sample

```bash
cd samples/ExperimentFramework.SampleWebApp
dotnet run
```

Then open your browser to `https://localhost:5001` or `http://localhost:5000`

## Experiments Included

### 1. Recommendation Algorithm (Sticky Routing)

**Endpoint:** `GET /api/recommendations`

Demonstrates sticky A/B testing where each user consistently sees recommendations from the same algorithm across all sessions.

**Three Variants:**
- **Control:** Popularity-based recommendations
- **Variant A:** Machine learning-powered recommendations
- **Variant B:** Collaborative filtering recommendations

**Try it:**
```bash
curl https://localhost:5001/api/recommendations

curl https://localhost:5001/api/recommendations/algorithm
```

**How it works:**
1. `SessionIdentityProvider` extracts user identity from:
   - Authenticated user ID (if logged in)
   - Session ID (for anonymous users)
   - IP + User-Agent hash (fallback)

2. Sticky routing hashes the identity and maps it to a variant

3. Same identity → Same variant (every time!)

**Test sticky behavior:**
- Open the endpoint in your browser
- Note which algorithm you see
- Refresh multiple times - you'll always see the same algorithm
- Open in a different browser/incognito - you'll likely see a different algorithm

### 2. Checkout Flow (Feature Flag)

**Endpoint:** `GET /api/checkout/flow`

Demonstrates feature flag-based experiments for gradual rollouts.

**Two Variants:**
- **Control (EnableExpressCheckout = false):** Standard 5-step checkout
- **Variant (EnableExpressCheckout = true):** Express 3-step checkout

**Try it:**
```bash
# View current checkout flow
curl https://localhost:5001/api/checkout/flow

# Simulate completing checkout
curl -X POST https://localhost:5001/api/checkout/complete \
  -H "Content-Type: application/json" \
  -d '{"items":["product1","product2"],"total":99.99}'
```

**Toggle the experiment:**
1. Edit `appsettings.json`
2. Change `EnableExpressCheckout` from `false` to `true`
3. Save the file (app will reload)
4. Call the endpoint again - you'll see Express checkout!

## Source Generation via Fluent API

This sample uses **`.UseSourceGenerators()`** to trigger compile-time proxy generation.

**Program.cs (Provider Registration):**
```csharp
// Register sticky routing provider (from ExperimentFramework.StickyRouting package)
builder.Services.AddExperimentStickyRouting();

// Register identity provider for sticky routing
builder.Services.AddScoped<IExperimentIdentityProvider, SessionIdentityProvider>();
```

**Configuration (ExperimentConfiguration.cs):**
```csharp
public static ExperimentFrameworkBuilder ConfigureWebExperiments()
{
    return ExperimentFrameworkBuilder.Create()
        .Trial<IRecommendationEngine>(t => t
            .UsingCustomMode("StickyRouting")  // Uses sticky routing via custom mode
            .AddControl<PopularityRecommendationEngine>("control")
            .AddVariant<MLRecommendationEngine>("ml")
            .AddVariant<CollaborativeRecommendationEngine>("collaborative"))
        .Trial<ICheckoutFlow>(t => t
            .UsingFeatureFlag("EnableExpressCheckout")
            .AddControl<StandardCheckoutFlow>("false")
            .AddVariant<ExpressCheckoutFlow>("true"))
        .UseSourceGenerators(); // Triggers compile-time proxy generation!
}
```

**No attribute needed!** The `.UseSourceGenerators()` call triggers the source generator to create optimized proxies at compile time.

**View generated proxies:**
```
obj/Debug/net10.0/generated/ExperimentFramework.Generators/
  ├── RecommendationEngineExperimentProxy.g.cs
  └── CheckoutFlowExperimentProxy.g.cs
```

## Architecture

```
User Request
     ↓
SessionIdentityProvider (extracts user/session ID)
     ↓
IRecommendationEngine (Source-Generated Proxy)
     ↓
Sticky Routing (hash identity → variant)
     ↓
PopularityRecommendationEngine
     or
MLRecommendationEngine
     or
CollaborativeRecommendationEngine
     ↓
Return Results
```

## Key Files

| File | Description |
|------|-------------|
| `ExperimentConfiguration.cs` | Configures experiments using fluent API |
| `Services/SessionIdentityProvider.cs` | Extracts user identity for sticky routing |
| `Services/IRecommendationEngine.cs` | Recommendation algorithm interfaces |
| `Services/ICheckoutFlow.cs` | Checkout flow interfaces |
| `Controllers/RecommendationsController.cs` | API endpoint for recommendations |
| `Controllers/CheckoutController.cs` | API endpoint for checkout |
| `Program.cs` | Web app startup and DI configuration |

## Testing Sticky Routing

To see sticky routing in action:

1. **Same Session = Same Algorithm:**
   ```bash
   # Call 5 times in a row
   for i in {1..5}; do
     curl -s https://localhost:5001/api/recommendations/algorithm | jq .Algorithm
   done
   # You'll see the same algorithm 5 times!
   ```

2. **Different Sessions = Different Algorithms:**
   ```bash
   # Different cookies = different sessions
   curl -s -c cookie1.txt https://localhost:5001/api/recommendations/algorithm | jq .Algorithm
   curl -s -c cookie2.txt https://localhost:5001/api/recommendations/algorithm | jq .Algorithm
   curl -s -c cookie3.txt https://localhost:5001/api/recommendations/algorithm | jq .Algorithm
   # You'll likely see different algorithms!
   ```

## Feature Flag Rollout Strategy

Edit `appsettings.json` to gradually roll out Express checkout:

```json
{
  "FeatureManagement": {
    "EnableExpressCheckout": false  // 0% of traffic
  }
}
```

→ Test with internal team

```json
{
  "FeatureManagement": {
    "EnableExpressCheckout": true   // 100% of traffic
  }
}
```

→ Full rollout!

In production, you'd use more sophisticated targeting (percentage rollout, user segments, etc.) with Azure App Configuration or similar.

## Source Generation Approaches

The framework supports two ways to trigger compile-time proxy generation:

**Fluent API** (used in this sample):
```csharp
.UseSourceGenerators() // Explicit marker
```

**Attribute approach**:
```csharp
[ExperimentCompositionRoot]
public static ExperimentFrameworkBuilder Configure() { ... }
```

Both approaches generate identical proxies. Choose based on your preference:
- **Attribute:** More discoverable, clearer intent
- **Fluent API:** Consistent with builder pattern

## Performance

Generated proxies use **direct method calls**:

```csharp
// Direct invocation with no runtime overhead
var impl = (IRecommendationEngine)sp.GetRequiredService(implType);
return await impl.GetRecommendationsAsync(userId, ct);
```

**Result:** <100ns overhead per call using compile-time source generation

## Next Steps

- Add OpenTelemetry tracking: `services.AddOpenTelemetryExperimentTracking()`
- Implement custom decorators for caching, circuit breakers, etc.
- Add variant feature flags for multi-way A/B tests
- Integrate with Azure App Configuration for centralized feature management
