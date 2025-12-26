# Scientific Experimentation Cheat Sheet

Quick reference for ExperimentFramework.Data and ExperimentFramework.Science packages.

## Setup

```csharp
// Install packages
dotnet add package ExperimentFramework.Data
dotnet add package ExperimentFramework.Science

// Register services
services.AddExperimentDataCollection();
services.AddExperimentScience();
```

## Data Collection

### Record Outcomes

```csharp
// Binary (success/failure)
await recorder.RecordBinaryAsync(experiment, trial, userId, "converted", true);

// Continuous (numeric)
await recorder.RecordContinuousAsync(experiment, trial, userId, "revenue", 149.99);

// Count (integer)
await recorder.RecordCountAsync(experiment, trial, userId, "items", 3);

// Duration (time)
await recorder.RecordDurationAsync(experiment, trial, userId, "load_time", TimeSpan.FromMilliseconds(250));
```

### Query Data

```csharp
var outcomes = await store.QueryAsync(new OutcomeQuery { ExperimentName = "test" });
var aggregations = await store.GetAggregationsAsync("test", "converted");
```

## Power Analysis

```csharp
var power = PowerAnalyzer.Instance;

// Sample size for 80% power to detect d=0.5 at alpha=0.05
var n = power.CalculateSampleSize(0.5, 0.80, 0.05);  // ~64 per group

// Power with current sample
var p = power.CalculatePower(100, 0.5, 0.05);  // ~94%

// Minimum detectable effect
var mde = power.CalculateMinimumDetectableEffect(100, 0.80, 0.05);  // ~0.40

// Complete analysis
var result = power.Analyze(100, 0.5, 0.80, 0.05);
```

### Binary Outcomes

```csharp
var opts = new PowerOptions { OutcomeType = PowerOutcomeType.Binary, BaselineProportion = 0.25 };
var n = power.CalculateSampleSize(0.05, 0.80, 0.05, opts);  // Detect 5pp improvement
```

## Statistical Tests

### Continuous Data

```csharp
// Two-sample t-test (Welch's)
var result = TwoSampleTTest.Instance.Perform(control, treatment, 0.05);

// Paired t-test (before/after)
var result = PairedTTest.Instance.Perform(before, after, 0.05);

// Non-parametric (Mann-Whitney U)
var result = MannWhitneyUTest.Instance.Perform(control, treatment, 0.05);

// Multiple groups (ANOVA)
var result = OneWayAnova.Instance.Perform(groups, 0.05);
```

### Binary Data

```csharp
// Chi-square test (1.0=success, 0.0=failure)
var result = ChiSquareTest.Instance.Perform(controlBinary, treatmentBinary, 0.05);
```

### Result Properties

```csharp
result.TestStatistic      // Test statistic (t, chi-square, etc.)
result.PValue             // p-value
result.IsSignificant      // p < alpha
result.PointEstimate      // Mean difference or proportion diff
result.ConfidenceIntervalLower  // CI lower bound
result.ConfidenceIntervalUpper  // CI upper bound
result.DegreesOfFreedom   // Degrees of freedom
```

## Effect Size

### Cohen's d (Continuous)

```csharp
var effect = CohensD.Instance.Calculate(control, treatment);
effect.Value       // Cohen's d value
effect.Magnitude   // Negligible, Small, Medium, Large
```

| d | Magnitude |
|---|-----------|
| < 0.2 | Negligible |
| 0.2-0.5 | Small |
| 0.5-0.8 | Medium |
| > 0.8 | Large |

### Odds Ratio & Relative Risk (Binary)

```csharp
var or = OddsRatio.Instance.Calculate(ctrlSuccess, ctrlN, txSuccess, txN);
var rr = RelativeRisk.Instance.Calculate(ctrlSuccess, ctrlN, txSuccess, txN);
```

## Multiple Comparison Corrections

```csharp
var pValues = new[] { 0.01, 0.03, 0.04 };

// Bonferroni (most conservative)
var adjusted = BonferroniCorrection.Instance.AdjustPValues(pValues);
var significant = BonferroniCorrection.Instance.DetermineSignificance(pValues, 0.05);

// Holm-Bonferroni (step-down)
var adjusted = HolmBonferroniCorrection.Instance.AdjustPValues(pValues);

// Benjamini-Hochberg (FDR control, recommended)
var adjusted = BenjaminiHochbergCorrection.Instance.AdjustPValues(pValues);
```

| Method | Controls For | Power |
|--------|-------------|-------|
| Bonferroni | FWER | Lowest |
| Holm-Bonferroni | FWER | Medium |
| Benjamini-Hochberg | FDR | Highest |

## Hypothesis Definition

```csharp
var hypothesis = new HypothesisBuilder("experiment-name")
    .Superiority()  // or .NonInferiority() or .Equivalence()
    .NullHypothesis("No effect")
    .AlternativeHypothesis("Treatment improves outcome")
    .PrimaryEndpoint("conversion", OutcomeType.Binary, ep => ep.HigherIsBetter())
    .SecondaryEndpoint("revenue", OutcomeType.Continuous, ep => ep.HigherIsBetter())
    .ExpectedEffectSize(0.05)
    .WithSuccessCriteria(c => c
        .Alpha(0.05)
        .Power(0.80)
        .MinimumSampleSize(1000))
    .DefinedNow()
    .Build();
```

## Report Generation

```csharp
var report = new ExperimentReport { ... };

// Markdown
var markdown = await new MarkdownReporter().GenerateAsync(report);

// JSON
var json = await new JsonReporter().GenerateAsync(report);
```

## Sample Size Quick Reference

| Effect Size (d) | n per group | Total |
|-----------------|-------------|-------|
| 0.2 (small) | 394 | 788 |
| 0.3 | 176 | 352 |
| 0.5 (medium) | 64 | 128 |
| 0.8 (large) | 26 | 52 |

*At 80% power, alpha=0.05, two-sided*

## Common Patterns

### Full Experiment Workflow

```csharp
// 1. Power analysis
var n = PowerAnalyzer.Instance.CalculateSampleSize(0.05, 0.80, 0.05, binaryOptions);

// 2. Define hypothesis
var hypothesis = new HypothesisBuilder("test").Superiority()...Build();

// 3. Collect data
await recorder.RecordBinaryAsync("test", trial, userId, "converted", success);

// 4. Analyze when n reached
var testResult = ChiSquareTest.Instance.Perform(control, treatment, 0.05);
var effectSize = OddsRatio.Instance.Calculate(ctrlSuccess, ctrlN, txSuccess, txN);

// 5. Generate report
var report = new ExperimentReport { ... };
var markdown = await reporter.GenerateAsync(report);
```

### Interpreting Results

```csharp
if (result.IsSignificant && result.PointEstimate > 0)
    Console.WriteLine("Treatment significantly better than control");
else if (result.IsSignificant && result.PointEstimate < 0)
    Console.WriteLine("Treatment significantly worse than control");
else
    Console.WriteLine("No significant difference detected");
```

## Best Practices

1. **Calculate sample size before starting**
2. **Pre-register hypothesis before analysis**
3. **Run to predetermined sample size** (don't peek and stop early)
4. **Report effect sizes**, not just p-values
5. **Apply multiple comparison corrections** when testing multiple endpoints
6. **Use one primary endpoint** to avoid multiple testing issues

## See Also

- [Data Collection](data-collection.md)
- [Statistical Analysis](statistical-analysis.md)
- [Hypothesis Testing](hypothesis-testing.md)
- [Power Analysis](power-analysis.md)
