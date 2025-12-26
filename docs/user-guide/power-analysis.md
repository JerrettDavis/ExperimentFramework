# Power Analysis

Calculate required sample sizes and statistical power for your experiments. Power analysis ensures you collect enough data to detect meaningful effects while avoiding waste from oversized experiments.

## Overview

Power analysis answers three key questions:

1. **Sample Size**: How many subjects do I need?
2. **Power**: What's my chance of detecting a true effect?
3. **Minimum Detectable Effect**: What's the smallest effect I can reliably detect?

## Why Power Analysis Matters

| Problem | Cause | Consequence |
|---------|-------|-------------|
| Underpowered | Too few subjects | Miss real effects (false negatives) |
| Overpowered | Too many subjects | Waste resources, detect trivial effects |

**Standard targets:**
- Power: 80% (0.80) - conventional minimum
- Alpha: 5% (0.05) - conventional significance level

## Quick Start

```csharp
using ExperimentFramework.Science.Power;

var analyzer = PowerAnalyzer.Instance;

// Calculate required sample size
var sampleSize = analyzer.CalculateSampleSize(
    effectSize: 0.5,   // Cohen's d = 0.5 (medium effect)
    power: 0.80,       // 80% power
    alpha: 0.05);      // 5% significance level

Console.WriteLine($"Required sample size per group: {sampleSize}");
// Output: Required sample size per group: 64
```

## Calculating Sample Size

### Continuous Outcomes (Cohen's d)

For metrics like revenue, scores, or duration:

```csharp
// Small effect (d = 0.2) - subtle difference
var smallEffect = analyzer.CalculateSampleSize(0.2, 0.80, 0.05);
// ~394 per group

// Medium effect (d = 0.5) - noticeable difference
var mediumEffect = analyzer.CalculateSampleSize(0.5, 0.80, 0.05);
// ~64 per group

// Large effect (d = 0.8) - obvious difference
var largeEffect = analyzer.CalculateSampleSize(0.8, 0.80, 0.05);
// ~26 per group
```

### Binary Outcomes (Proportions)

For metrics like conversion rate or click-through rate:

```csharp
// Detect 5% improvement in 25% baseline conversion
var options = new PowerOptions
{
    OutcomeType = PowerOutcomeType.Binary,
    BaselineProportion = 0.25  // Current 25% conversion
};

var sampleSize = analyzer.CalculateSampleSize(
    effectSize: 0.05,  // 5 percentage point improvement (25% -> 30%)
    power: 0.80,
    alpha: 0.05,
    options: options);

Console.WriteLine($"Required sample size per group: {sampleSize}");
// Output: Required sample size per group: ~580
```

### One-Sided Tests

When you only care if treatment is better (not worse):

```csharp
var options = new PowerOptions { OneSided = true };

var oneSided = analyzer.CalculateSampleSize(0.5, 0.80, 0.05, options);
var twoSided = analyzer.CalculateSampleSize(0.5, 0.80, 0.05);

Console.WriteLine($"One-sided: {oneSided}, Two-sided: {twoSided}");
// One-sided requires fewer subjects (but can't detect negative effects)
```

### Unequal Allocation

When control and treatment have different sizes:

```csharp
// 2:1 allocation (2 treatment for every 1 control)
var options = new PowerOptions { AllocationRatio = 2.0 };

var sampleSize = analyzer.CalculateSampleSize(0.5, 0.80, 0.05, options);
Console.WriteLine($"Control: {sampleSize}, Treatment: {sampleSize * 2}");
```

## Calculating Power

Given your current sample size, what's the probability of detecting an effect?

```csharp
// Current sample: 50 per group, looking for medium effect
var power = analyzer.CalculatePower(
    sampleSizePerGroup: 50,
    effectSize: 0.5,
    alpha: 0.05);

Console.WriteLine($"Power: {power:P1}");
// Output: Power: 69.7%

// Large sample: essentially guaranteed detection
var largeSamplePower = analyzer.CalculatePower(1000, 0.5, 0.05);
Console.WriteLine($"Large sample power: {largeSamplePower:P1}");
// Output: Large sample power: >99.9%
```

### Interpreting Power

| Power | Interpretation |
|-------|----------------|
| < 50% | Very likely to miss a real effect |
| 50-80% | Reasonable but risky |
| 80% | Conventional minimum |
| 90%+ | Well-powered |
| >99% | Possibly overpowered (wasting resources) |

## Minimum Detectable Effect

Given your sample size, what's the smallest effect you can reliably detect?

```csharp
// With 100 subjects per group, at 80% power
var mde = analyzer.CalculateMinimumDetectableEffect(
    sampleSizePerGroup: 100,
    power: 0.80,
    alpha: 0.05);

Console.WriteLine($"Minimum detectable effect: {mde:F2}");
// Output: Minimum detectable effect: 0.40 (Cohen's d)

// With 1000 subjects per group
var mdeLarge = analyzer.CalculateMinimumDetectableEffect(1000, 0.80, 0.05);
Console.WriteLine($"MDE with large sample: {mdeLarge:F2}");
// Output: MDE with large sample: 0.13
```

### MDE for Binary Outcomes

```csharp
var options = new PowerOptions
{
    OutcomeType = PowerOutcomeType.Binary,
    BaselineProportion = 0.10  // 10% baseline
};

// With 500 per group, what improvement can we detect?
var mde = analyzer.CalculateMinimumDetectableEffect(500, 0.80, 0.05, options);
Console.WriteLine($"Minimum detectable improvement: {mde:P1}");
// Can detect ~2-3pp improvement
```

## Comprehensive Analysis

Get a complete power analysis report:

```csharp
var result = analyzer.Analyze(
    currentSampleSizePerGroup: 100,
    effectSize: 0.3,
    targetPower: 0.80,
    alpha: 0.05);

Console.WriteLine($"Current sample size: {result.CurrentSampleSize}");
Console.WriteLine($"Achieved power: {result.AchievedPower:P1}");
Console.WriteLine($"Required sample size: {result.RequiredSampleSize}");
Console.WriteLine($"Adequately powered: {result.IsAdequatelyPowered}");
Console.WriteLine($"MDE at current size: {result.MinimumDetectableEffect:F2}");

// Output:
// Current sample size: 100
// Achieved power: 55.2%
// Required sample size: 176
// Adequately powered: False
// MDE at current size: 0.40
```

### Using Analysis Results

```csharp
var result = analyzer.Analyze(
    currentSampleSizePerGroup: experimentSampleSize,
    effectSize: expectedEffect,
    targetPower: 0.80,
    alpha: 0.05);

if (result.IsAdequatelyPowered)
{
    Console.WriteLine("Experiment is adequately powered. Proceed with analysis.");
}
else
{
    var needed = result.RequiredSampleSize - result.CurrentSampleSize;
    Console.WriteLine($"Need {needed} more subjects per group.");
    Console.WriteLine($"Alternative: Can detect effects >= {result.MinimumDetectableEffect:F2}");
}
```

## Effect Size Guidelines

### Cohen's d (Continuous Outcomes)

| d | Magnitude | Example |
|---|-----------|---------|
| 0.2 | Small | Subtle improvement |
| 0.5 | Medium | Noticeable improvement |
| 0.8 | Large | Obvious improvement |

### Proportion Differences (Binary Outcomes)

| Baseline | Small Effect | Medium Effect | Large Effect |
|----------|--------------|---------------|--------------|
| 5% | +1pp | +2.5pp | +4pp |
| 25% | +5pp | +12.5pp | +20pp |
| 50% | +10pp | +25pp | +40pp |

## Common Scenarios

### E-commerce Conversion

```csharp
// Current conversion: 3%, expecting 0.5pp improvement
var options = new PowerOptions
{
    OutcomeType = PowerOutcomeType.Binary,
    BaselineProportion = 0.03
};

var sampleSize = analyzer.CalculateSampleSize(
    effectSize: 0.005,  // 3% -> 3.5%
    power: 0.80,
    alpha: 0.05,
    options: options);

Console.WriteLine($"Need {sampleSize} visitors per variant");
// ~50,000+ per variant for small conversion lift
```

### Revenue per User

```csharp
// Expected 10% revenue increase on $50 average (std dev $40)
// Effect size = ($5 improvement) / ($40 std dev) = 0.125

var sampleSize = analyzer.CalculateSampleSize(
    effectSize: 0.125,  // Small-medium effect
    power: 0.80,
    alpha: 0.05);

Console.WriteLine($"Need {sampleSize} users per variant");
// ~1,000+ per variant
```

### Page Load Time

```csharp
// Current: 3.0s average, 1.0s std dev
// Goal: Detect 200ms improvement (d = 0.2)

var sampleSize = analyzer.CalculateSampleSize(
    effectSize: 0.2,  // Small effect
    power: 0.80,
    alpha: 0.05);

Console.WriteLine($"Need {sampleSize} page loads per variant");
// ~394 per variant
```

### Click-Through Rate

```csharp
// Current CTR: 2%, expecting 0.4pp improvement
var options = new PowerOptions
{
    OutcomeType = PowerOutcomeType.Binary,
    BaselineProportion = 0.02
};

var sampleSize = analyzer.CalculateSampleSize(
    effectSize: 0.004,  // 2% -> 2.4%
    power: 0.80,
    alpha: 0.05,
    options: options);

Console.WriteLine($"Need {sampleSize} impressions per variant");
// ~25,000+ per variant
```

## Sample Size Tables

### Two-Sided Tests, Alpha = 0.05, Power = 0.80

| Effect Size (d) | Sample per Group | Total Sample |
|-----------------|------------------|--------------|
| 0.10 | 1,571 | 3,142 |
| 0.20 | 394 | 788 |
| 0.30 | 176 | 352 |
| 0.40 | 100 | 200 |
| 0.50 | 64 | 128 |
| 0.60 | 45 | 90 |
| 0.70 | 33 | 66 |
| 0.80 | 26 | 52 |

### Binary Outcomes (Baseline = 25%)

| Improvement | Sample per Group |
|-------------|------------------|
| +2pp (25% -> 27%) | 2,599 |
| +3pp (25% -> 28%) | 1,159 |
| +5pp (25% -> 30%) | 421 |
| +7pp (25% -> 32%) | 219 |
| +10pp (25% -> 35%) | 113 |

## Best Practices

### 1. Plan Before Starting

Calculate sample size during experiment design, not after:

```csharp
// Good - calculate upfront
var requiredN = analyzer.CalculateSampleSize(expectedEffect, 0.80, 0.05);
var daysNeeded = requiredN * 2 / dailyTraffic;
Console.WriteLine($"Experiment will need {daysNeeded} days to complete");

// Bad - run underpowered experiment, hope for significance
StartExperiment();  // Without knowing if sample is adequate
```

### 2. Use Realistic Effect Sizes

Base expectations on prior evidence, not optimism:

```csharp
// Good - based on prior tests
var priorEffect = 0.03;  // Previous similar test showed 3% lift
var sampleSize = analyzer.CalculateSampleSize(priorEffect, 0.80, 0.05);

// Bad - optimistic guessing
var hopedEffect = 0.20;  // "I hope for 20% improvement"
var sampleSize = analyzer.CalculateSampleSize(hopedEffect, 0.80, 0.05);
// Will be underpowered if true effect is smaller
```

### 3. Consider Practical Significance

A detectable effect isn't always a meaningful effect:

```csharp
var result = analyzer.Analyze(10000, effectSize: 0.05, targetPower: 0.80, alpha: 0.05);

if (result.IsAdequatelyPowered)
{
    Console.WriteLine("Can detect very small effects.");
    Console.WriteLine("But is a 0.05d effect worth acting on?");
}
```

### 4. Account for Attrition

Add buffer for dropouts and data quality issues:

```csharp
var requiredN = analyzer.CalculateSampleSize(0.3, 0.80, 0.05);
var expectedAttrition = 0.15;  // 15% dropout rate
var recruitmentTarget = (int)(requiredN / (1 - expectedAttrition));

Console.WriteLine($"Need to recruit {recruitmentTarget} to end with {requiredN}");
```

### 5. Run Power Checks Mid-Experiment

Monitor if you're on track:

```csharp
public async Task CheckExperimentProgress()
{
    var currentN = await GetCurrentSampleSize();
    var result = analyzer.Analyze(currentN, expectedEffect, 0.80, 0.05);

    if (result.IsAdequatelyPowered)
    {
        Console.WriteLine("Ready for analysis");
    }
    else
    {
        var progress = (double)currentN / result.RequiredSampleSize!.Value;
        Console.WriteLine($"Progress: {progress:P0}");
        Console.WriteLine($"Current power: {result.AchievedPower:P1}");
    }
}
```

## Dependency Injection

```csharp
services.AddExperimentScience();

public class ExperimentDesigner
{
    private readonly IPowerAnalyzer _power;

    public ExperimentDesigner(IPowerAnalyzer power)
    {
        _power = power;
    }

    public ExperimentPlan Design(double expectedEffect, int dailyTraffic)
    {
        var sampleSize = _power.CalculateSampleSize(expectedEffect, 0.80, 0.05);
        var daysNeeded = (sampleSize * 2) / dailyTraffic;

        return new ExperimentPlan
        {
            SampleSizePerGroup = sampleSize,
            ExpectedDuration = TimeSpan.FromDays(daysNeeded)
        };
    }
}
```

## Common Mistakes

### 1. Stopping When Significant

Don't peek and stop early when you see p < 0.05:

```csharp
// Bad - inflates false positive rate
while (true)
{
    var result = AnalyzeCurrentData();
    if (result.IsSignificant) break;  // p-hacking!
    await Task.Delay(TimeSpan.FromHours(1));
}

// Good - run to predetermined sample size
var requiredN = analyzer.CalculateSampleSize(effect, 0.80, 0.05);
await RunUntilSampleSize(requiredN);
var result = AnalyzeFinalData();
```

### 2. Ignoring Multiple Comparisons

Testing multiple variants requires more samples:

```csharp
// 3 variants means 3 comparisons (A vs B, A vs C, B vs C)
// Apply Bonferroni correction
var adjustedAlpha = 0.05 / 3;  // ~0.017
var sampleSize = analyzer.CalculateSampleSize(effect, 0.80, adjustedAlpha);
// Needs more subjects than single comparison
```

### 3. Assuming Equal Variance

For very different groups, use separate calculations:

```csharp
// If treatment has higher variance, need larger sample
// Consider using Mann-Whitney U test instead of t-test
```

## See Also

- [Statistical Analysis](statistical-analysis.md) - Statistical tests
- [Hypothesis Testing](hypothesis-testing.md) - Defining hypotheses
- [Data Collection](data-collection.md) - Recording outcomes
