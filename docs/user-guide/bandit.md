# Multi-Armed Bandit Algorithms

The Bandit package provides adaptive traffic allocation algorithms that automatically shift traffic toward better-performing variants. Unlike traditional A/B testing with fixed traffic splits, bandit algorithms balance exploration (learning) and exploitation (winning).

## Installation

```bash
dotnet add package ExperimentFramework.Bandit
```

## Quick Start

```csharp
// Register bandit support with Thompson Sampling
services.AddExperimentBandit<ThompsonSampling>();

// Configure experiment
var experiments = ExperimentFrameworkBuilder.Create()
    .Define<IRecommendationEngine>(exp => exp
        .UsingBandit()
        .AddControl<CollaborativeFilteringEngine>("collaborative")
        .AddCondition<ContentBasedEngine>("content-based")
        .AddCondition<HybridEngine>("hybrid"));

services.AddExperimentFramework(experiments);
```

## When to Use Bandits

### Use Bandits When:
- **Opportunity cost matters**: You want to minimize exposure to underperforming variants
- **Quick adaptation is needed**: You need to respond to changing conditions
- **Exploration budget is limited**: You can't afford extended A/B test periods
- **Winner is unknown**: You're genuinely uncertain which variant is best

### Use Traditional A/B Testing When:
- **Statistical rigor required**: You need precise confidence intervals
- **Compliance/regulatory needs**: Audit trails require fixed allocation
- **Long-term effects matter**: You're measuring effects that take time to manifest
- **Sample size is known**: You've pre-calculated required sample size

## Available Algorithms

### Epsilon-Greedy

The simplest bandit algorithm. Exploits the best-known arm most of the time, but randomly explores with probability epsilon.

```csharp
services.AddExperimentBandit<EpsilonGreedy>(options =>
{
    options.Epsilon = 0.1;  // 10% exploration rate
});
```

**Characteristics:**
- Simple to understand and implement
- Fixed exploration rate
- Works well when reward distributions are stable

**Best for:** Simple optimization problems, baseline comparison

### Thompson Sampling

Bayesian approach that samples from the posterior distribution of each arm's reward probability.

```csharp
services.AddExperimentBandit<ThompsonSampling>(options =>
{
    options.Seed = 42;  // For reproducibility
});
```

**Characteristics:**
- Naturally balances exploration/exploitation
- Performs well in practice
- Handles uncertainty elegantly

**Best for:** Most production use cases, A/B testing replacement

### Upper Confidence Bound (UCB)

Selects arms optimistically based on their upper confidence bound, favoring arms with high uncertainty.

```csharp
services.AddExperimentBandit<UpperConfidenceBound>(options =>
{
    options.ExplorationParameter = 2.0;  // Higher = more exploration
});
```

**Characteristics:**
- Deterministic (given same state)
- Strong theoretical guarantees
- Explicit uncertainty handling

**Best for:** When reproducibility is important, theoretical analysis

## Core Concepts

### Arm Statistics

Each variant (arm) tracks its performance:

```csharp
public sealed class ArmStatistics
{
    public required string Key { get; init; }      // Variant identifier
    public long Pulls { get; set; }                // Times selected
    public double TotalReward { get; set; }        // Sum of rewards
    public double AverageReward { get; }           // TotalReward / Pulls
    public long Successes { get; set; }            // For binary outcomes
    public long Failures { get; set; }             // For binary outcomes
}
```

### Reward Recording

After each user interaction, record the outcome:

```csharp
public class CheckoutController
{
    private readonly IBanditRewardRecorder _recorder;

    public async Task<IActionResult> CompleteCheckout(CheckoutRequest request)
    {
        // ... process checkout ...

        // Record reward (1.0 for conversion, 0.0 for no conversion)
        await _recorder.RecordRewardAsync(
            experimentName: "ICheckoutFlow",
            variantKey: request.VariantKey,
            reward: 1.0);

        return Ok();
    }
}
```

## Integration Patterns

### With Outcome Collection

```csharp
services.AddExperimentBandit<ThompsonSampling>();
services.AddExperimentData(options =>
{
    options.EnableOutcomeCollection = true;
});

// The data package automatically updates bandit statistics
```

### With Distributed State

For multi-instance deployments:

```csharp
services.AddExperimentDistributedRedis(options =>
{
    options.ConnectionString = "localhost:6379";
});

services.AddExperimentBandit<ThompsonSampling>(options =>
{
    options.UseDistributedState = true;
    options.StateKeyPrefix = "bandit:";
});
```

### Manual State Management

```csharp
public class BanditService
{
    private readonly IBanditAlgorithm _algorithm;
    private readonly IBanditStateStore _store;

    public async Task<string> SelectVariantAsync(string experimentName)
    {
        // Load current arm statistics
        var arms = await _store.GetArmsAsync(experimentName);

        // Select arm using algorithm
        var selectedIndex = _algorithm.SelectArm(arms);
        var selectedArm = arms[selectedIndex];

        // Update pull count
        selectedArm.Pulls++;
        await _store.UpdateArmAsync(experimentName, selectedArm);

        return selectedArm.Key;
    }

    public async Task RecordRewardAsync(
        string experimentName, string variantKey, double reward)
    {
        var arm = await _store.GetArmAsync(experimentName, variantKey);

        _algorithm.UpdateArm(arm, reward);

        await _store.UpdateArmAsync(experimentName, arm);
    }
}
```

## Configuration

### Global Options

```csharp
services.AddExperimentBandit<ThompsonSampling>(options =>
{
    // Initial exploration
    options.MinPullsBeforeExploitation = 100;  // Explore until 100 pulls each

    // State management
    options.UseDistributedState = false;
    options.StatePersistenceInterval = TimeSpan.FromSeconds(30);

    // Warm-up
    options.InitialArms = new Dictionary<string, ArmStatistics>
    {
        ["control"] = new() { Successes = 10, Failures = 90 },  // Prior belief
        ["treatment"] = new() { Successes = 10, Failures = 90 }
    };
});
```

### Per-Experiment Options

```csharp
.Define<ISearchRanker>(exp => exp
    .UsingBandit(bandit =>
    {
        bandit.Algorithm = BanditAlgorithm.ThompsonSampling;
        bandit.RewardType = RewardType.Binary;  // 0/1 outcomes
        bandit.ExplorationWindow = TimeSpan.FromHours(24);
    })
    .AddControl<BM25Ranker>("bm25")
    .AddCondition<SemanticRanker>("semantic"))
```

## Reward Types

### Binary Rewards

Simple success/failure outcomes:

```csharp
// Conversion events
await recorder.RecordRewardAsync(experiment, variant, reward: conversion ? 1.0 : 0.0);
```

### Continuous Rewards

Numeric values (revenue, engagement time, etc.):

```csharp
// Revenue optimization
await recorder.RecordRewardAsync(experiment, variant, reward: orderTotal);

// Engagement time (normalized)
var normalizedTime = Math.Min(sessionDuration.TotalMinutes / 30.0, 1.0);
await recorder.RecordRewardAsync(experiment, variant, reward: normalizedTime);
```

### Delayed Rewards

Handle outcomes that occur after initial interaction:

```csharp
// Store assignment for later reward
var assignment = new UserAssignment
{
    UserId = userId,
    ExperimentName = "ISubscriptionFlow",
    VariantKey = selectedVariant,
    AssignedAt = DateTimeOffset.UtcNow
};
await _assignmentStore.SaveAsync(assignment);

// Days later, when subscription converts:
public async Task HandleSubscriptionConversion(string userId)
{
    var assignment = await _assignmentStore.GetAsync(userId, "ISubscriptionFlow");
    if (assignment != null)
    {
        await _recorder.RecordRewardAsync(
            assignment.ExperimentName,
            assignment.VariantKey,
            reward: 1.0);
    }
}
```

## Monitoring & Analysis

### Real-time Statistics

```csharp
public class BanditDashboardController
{
    private readonly IBanditStateStore _store;

    public async Task<IActionResult> GetStats(string experimentName)
    {
        var arms = await _store.GetArmsAsync(experimentName);

        return Ok(new
        {
            arms = arms.Select(arm => new
            {
                key = arm.Key,
                pulls = arm.Pulls,
                successRate = arm.Successes / (double)(arm.Successes + arm.Failures),
                averageReward = arm.AverageReward,
                exploitationShare = arm.Pulls / (double)arms.Sum(a => a.Pulls)
            }),
            totalPulls = arms.Sum(a => a.Pulls),
            estimatedBestArm = arms.MaxBy(a => a.AverageReward)?.Key
        });
    }
}
```

### Convergence Detection

```csharp
public class ConvergenceDetector
{
    public bool HasConverged(IReadOnlyList<ArmStatistics> arms)
    {
        // Check if one arm is receiving >95% of traffic
        var totalPulls = arms.Sum(a => a.Pulls);
        if (totalPulls < 1000) return false;  // Need minimum samples

        var topArm = arms.MaxBy(a => a.Pulls);
        var topShare = topArm.Pulls / (double)totalPulls;

        return topShare > 0.95;
    }

    public string? GetWinner(IReadOnlyList<ArmStatistics> arms, double confidenceThreshold = 0.95)
    {
        if (!HasConverged(arms)) return null;

        var topArm = arms.MaxBy(a => a.AverageReward);
        // Could add statistical significance check here

        return topArm?.Key;
    }
}
```

## Real-World Examples

### Headline Optimization

```csharp
public class HeadlineService
{
    private readonly IBanditSelector _bandit;
    private readonly IBanditRewardRecorder _recorder;

    public async Task<Headline> GetHeadlineAsync(Article article)
    {
        var variants = article.HeadlineVariants;

        // Select headline using bandit
        var selectedKey = await _bandit.SelectAsync($"article:{article.Id}:headline", variants.Keys);
        var headline = variants[selectedKey];

        return headline;
    }

    public async Task RecordClickAsync(string articleId, string headlineKey)
    {
        await _recorder.RecordRewardAsync(
            $"article:{articleId}:headline",
            headlineKey,
            reward: 1.0);
    }
}
```

### Pricing Optimization

```csharp
public class PricingBandit
{
    public async Task<decimal> GetOptimalPriceAsync(string productId)
    {
        var pricePoints = new[] { 9.99m, 14.99m, 19.99m, 24.99m };
        var arms = pricePoints.Select(p => p.ToString()).ToList();

        var selectedKey = await _bandit.SelectAsync($"pricing:{productId}", arms);
        return decimal.Parse(selectedKey);
    }

    public async Task RecordPurchaseAsync(string productId, decimal price, decimal revenue)
    {
        // Use revenue as reward (normalize by max expected)
        var normalizedReward = Math.Min(revenue / 100.0, 1.0);

        await _recorder.RecordRewardAsync(
            $"pricing:{productId}",
            price.ToString(),
            reward: (double)normalizedReward);
    }
}
```

## Best Practices

1. **Start with Thompson Sampling**: It's robust and works well in most scenarios
2. **Use sufficient initial exploration**: Require minimum pulls before full exploitation
3. **Normalize rewards**: Keep rewards between 0 and 1 when possible
4. **Monitor convergence**: Watch for premature convergence on suboptimal arms
5. **Consider delayed effects**: Some outcomes take time to materialize
6. **Handle cold starts**: Initialize with priors based on domain knowledge

## Troubleshooting

### Premature convergence

**Symptom**: Algorithm quickly converges to one arm that may not be optimal.

**Cause**: Early lucky streak or insufficient exploration.

**Solution**: Increase minimum exploration:

```csharp
options.MinPullsBeforeExploitation = 500;  // More initial exploration
```

### Reward drift

**Symptom**: Performance degrades over time after initial convergence.

**Cause**: User preferences or external factors changed.

**Solution**: Implement sliding window or decay:

```csharp
// Use only recent data for arm statistics
options.RewardWindow = TimeSpan.FromDays(7);
options.RewardDecay = 0.99;  // Decay factor per day
```

### High variance in rewards

**Symptom**: Arm selection is erratic, never converges.

**Cause**: Reward signal is too noisy.

**Solution**: Aggregate rewards or use longer evaluation periods:

```csharp
// Instead of individual conversions, use session-level metrics
public async Task RecordSessionRewardAsync(string sessionId)
{
    var metrics = await _sessionStore.GetMetricsAsync(sessionId);
    var compositeReward =
        0.3 * metrics.PageViews / 10.0 +
        0.3 * metrics.TimeOnSite.TotalMinutes / 30.0 +
        0.4 * (metrics.Converted ? 1.0 : 0.0);

    await _recorder.RecordRewardAsync(experiment, variant, compositeReward);
}
```
