# Statistical Analysis

Analyze experiment outcomes with rigorous statistical methods using the `ExperimentFramework.Science` package. Get publication-ready results with effect sizes, confidence intervals, and multiple comparison corrections.

## Overview

The Science package provides:

- **Statistical Tests**: t-test, chi-square, Mann-Whitney U, ANOVA
- **Effect Size Calculators**: Cohen's d, odds ratio, relative risk
- **Multiple Comparison Corrections**: Bonferroni, Holm-Bonferroni, Benjamini-Hochberg
- **Reporters**: Markdown and JSON report generation

## Installation

```bash
dotnet add package ExperimentFramework.Science
```

Requires MathNet.Numerics (included as a dependency).

## Quick Start

```csharp
using ExperimentFramework.Data;
using ExperimentFramework.Science;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddExperimentDataCollection();
builder.Services.AddExperimentScience();

var app = builder.Build();
```

## Statistical Tests

### Choosing the Right Test

| Data Type | Groups | Test | Use Case |
|-----------|--------|------|----------|
| Continuous | 2 independent | Two-sample t-test | Comparing means (revenue, scores) |
| Continuous | 2 paired | Paired t-test | Before/after comparisons |
| Binary | 2 | Chi-square test | Comparing proportions (conversion rates) |
| Continuous | 2 (non-normal) | Mann-Whitney U | Non-parametric mean comparison |
| Continuous | 3+ | One-way ANOVA | Comparing multiple groups |

### Two-Sample t-Test (Welch's)

Compare means between two independent groups:

```csharp
using ExperimentFramework.Science.Statistics;

var control = new double[] { 10.2, 12.5, 9.8, 11.3, 10.9 };
var treatment = new double[] { 14.1, 15.3, 13.7, 14.8, 15.0 };

var result = TwoSampleTTest.Instance.Perform(control, treatment, alpha: 0.05);

Console.WriteLine($"Test: {result.TestName}");
Console.WriteLine($"t-statistic: {result.TestStatistic:F3}");
Console.WriteLine($"p-value: {result.PValue:F4}");
Console.WriteLine($"Significant: {result.IsSignificant}");
Console.WriteLine($"95% CI: [{result.ConfidenceIntervalLower:F2}, {result.ConfidenceIntervalUpper:F2}]");
Console.WriteLine($"Effect: {result.PointEstimate:F2}");

// Output:
// Test: Welch's Two-Sample t-Test
// t-statistic: -7.234
// p-value: 0.0001
// Significant: True
// 95% CI: [2.41, 4.79]
// Effect: 3.60
```

#### One-Sided Tests

```csharp
// Test if treatment is greater than control
var result = TwoSampleTTest.Instance.Perform(
    control, treatment,
    alpha: 0.05,
    alternativeType: AlternativeHypothesisType.Greater);

// Test if treatment is less than control
var result = TwoSampleTTest.Instance.Perform(
    control, treatment,
    alpha: 0.05,
    alternativeType: AlternativeHypothesisType.Less);
```

### Chi-Square Test

Compare proportions for binary outcomes:

```csharp
using ExperimentFramework.Science.Statistics;

// Binary data: 1.0 = success, 0.0 = failure
var control = new double[] { 1, 0, 0, 1, 0, 0, 1, 0, 0, 0 };  // 30% success
var treatment = new double[] { 1, 1, 0, 1, 1, 0, 1, 1, 0, 1 }; // 70% success

var result = ChiSquareTest.Instance.Perform(control, treatment, alpha: 0.05);

Console.WriteLine($"Chi-square: {result.TestStatistic:F3}");
Console.WriteLine($"p-value: {result.PValue:F4}");
Console.WriteLine($"Difference in proportions: {result.PointEstimate:P1}");

// Access detailed results
var details = result.Details;
Console.WriteLine($"Control rate: {details["control_proportion"]:P1}");
Console.WriteLine($"Treatment rate: {details["treatment_proportion"]:P1}");
```

### Paired t-Test

Compare paired observations (same subjects, before/after):

```csharp
using ExperimentFramework.Science.Statistics;

// Each index represents the same subject
var before = new double[] { 100, 105, 98, 102, 110 };
var after = new double[] { 95, 98, 92, 97, 103 };

var result = PairedTTest.Instance.Perform(before, after, alpha: 0.05);

Console.WriteLine($"Mean difference: {result.PointEstimate:F2}");
Console.WriteLine($"p-value: {result.PValue:F4}");
```

### Mann-Whitney U Test

Non-parametric alternative when data isn't normally distributed:

```csharp
using ExperimentFramework.Science.Statistics;

var control = new double[] { 1, 2, 3, 100, 5 };     // Contains outlier
var treatment = new double[] { 10, 12, 15, 11, 14 };

var result = MannWhitneyUTest.Instance.Perform(control, treatment, alpha: 0.05);

Console.WriteLine($"U-statistic: {result.TestStatistic:F1}");
Console.WriteLine($"p-value: {result.PValue:F4}");
```

### One-Way ANOVA

Compare three or more groups:

```csharp
using ExperimentFramework.Science.Statistics;

var groups = new Dictionary<string, IReadOnlyList<double>>
{
    ["control"] = new double[] { 10, 12, 11, 9, 10 },
    ["variant-a"] = new double[] { 14, 15, 13, 14, 15 },
    ["variant-b"] = new double[] { 18, 17, 19, 18, 20 }
};

var result = OneWayAnova.Instance.Perform(groups, alpha: 0.05);

Console.WriteLine($"F-statistic: {result.TestStatistic:F2}");
Console.WriteLine($"p-value: {result.PValue:F4}");
Console.WriteLine($"Significant difference between groups: {result.IsSignificant}");

// Access group means from details
var details = result.Details;
foreach (var (group, mean) in (Dictionary<string, double>)details["group_means"])
{
    Console.WriteLine($"  {group}: {mean:F2}");
}
```

## Effect Size

Effect size quantifies the magnitude of differences independent of sample size.

### Cohen's d

Standard effect size for continuous data:

```csharp
using ExperimentFramework.Science.EffectSize;

var control = new double[] { 100, 102, 98, 101, 99 };
var treatment = new double[] { 110, 112, 108, 111, 109 };

var effect = CohensD.Instance.Calculate(control, treatment);

Console.WriteLine($"Cohen's d: {effect.Value:F2}");
Console.WriteLine($"Magnitude: {effect.Magnitude}");
Console.WriteLine($"95% CI: [{effect.ConfidenceIntervalLower:F2}, {effect.ConfidenceIntervalUpper:F2}]");

// Output:
// Cohen's d: 3.16
// Magnitude: Large
// 95% CI: [1.52, 4.80]
```

Effect size interpretation:

| Cohen's d | Magnitude | Interpretation |
|-----------|-----------|----------------|
| < 0.2 | Negligible | Trivial difference |
| 0.2 - 0.5 | Small | Minor difference |
| 0.5 - 0.8 | Medium | Moderate difference |
| > 0.8 | Large | Substantial difference |

### Odds Ratio

For binary outcomes (comparing odds of success):

```csharp
using ExperimentFramework.Science.EffectSize;

// Control: 20 successes out of 100
// Treatment: 35 successes out of 100
var effect = OddsRatio.Instance.Calculate(
    controlSuccesses: 20, controlTotal: 100,
    treatmentSuccesses: 35, treatmentTotal: 100);

Console.WriteLine($"Odds Ratio: {effect.Value:F2}");
Console.WriteLine($"95% CI: [{effect.ConfidenceIntervalLower:F2}, {effect.ConfidenceIntervalUpper:F2}]");

// Output:
// Odds Ratio: 2.15
// 95% CI: [1.14, 4.07]
// Interpretation: Treatment has 2.15x higher odds of success
```

Odds ratio interpretation:

| Value | Interpretation |
|-------|----------------|
| 1.0 | No difference |
| > 1.0 | Treatment increases odds |
| < 1.0 | Treatment decreases odds |

### Relative Risk

For binary outcomes (risk ratio):

```csharp
using ExperimentFramework.Science.EffectSize;

var effect = RelativeRisk.Instance.Calculate(
    controlSuccesses: 20, controlTotal: 100,
    treatmentSuccesses: 35, treatmentTotal: 100);

Console.WriteLine($"Relative Risk: {effect.Value:F2}");

// Output:
// Relative Risk: 1.75
// Interpretation: Treatment has 75% higher success rate
```

## Multiple Comparison Corrections

When running multiple tests, apply corrections to control false discovery rate.

### Bonferroni Correction

Most conservative - controls family-wise error rate:

```csharp
using ExperimentFramework.Science.Corrections;

var pValues = new double[] { 0.01, 0.03, 0.04 };

// Adjust p-values (multiply by number of tests)
var adjusted = BonferroniCorrection.Instance.AdjustPValues(pValues);
// [0.03, 0.09, 0.12]

// Or determine significance directly
var significant = BonferroniCorrection.Instance.DetermineSignificance(pValues, alpha: 0.05);
// [true, false, false] - only first is significant at adjusted threshold
```

### Holm-Bonferroni (Step-Down)

Less conservative than Bonferroni, more power:

```csharp
using ExperimentFramework.Science.Corrections;

var pValues = new double[] { 0.01, 0.03, 0.04 };

var adjusted = HolmBonferroniCorrection.Instance.AdjustPValues(pValues);
var significant = HolmBonferroniCorrection.Instance.DetermineSignificance(pValues, alpha: 0.05);
```

### Benjamini-Hochberg (FDR)

Controls false discovery rate - recommended for exploratory analysis:

```csharp
using ExperimentFramework.Science.Corrections;

var pValues = new double[] { 0.01, 0.03, 0.04 };

var adjusted = BenjaminiHochbergCorrection.Instance.AdjustPValues(pValues);
var significant = BenjaminiHochbergCorrection.Instance.DetermineSignificance(pValues, alpha: 0.05);

Console.WriteLine($"Correction: {BenjaminiHochbergCorrection.Instance.Name}");
Console.WriteLine($"Controls for: {BenjaminiHochbergCorrection.Instance.ControlsFor}");
```

### Correction Comparison

| Method | Controls For | Power | Use Case |
|--------|-------------|-------|----------|
| Bonferroni | Family-wise error | Lowest | Critical decisions, few tests |
| Holm-Bonferroni | Family-wise error | Medium | Confirmatory analysis |
| Benjamini-Hochberg | False discovery rate | Highest | Exploratory analysis |

## Experiment Analyzer

Analyze complete experiments with the analyzer service:

```csharp
using ExperimentFramework.Science.Analysis;

public class ExperimentService
{
    private readonly IExperimentAnalyzer _analyzer;

    public ExperimentService(IExperimentAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    public async Task<ExperimentReport> AnalyzeCheckoutExperiment()
    {
        return await _analyzer.AnalyzeAsync("checkout-v2", new AnalysisOptions
        {
            Alpha = 0.05,
            TargetPower = 0.80,
            ApplyMultipleComparisonCorrection = true,
            CorrectionMethod = MultipleComparisonMethod.BenjaminiHochberg
        });
    }
}
```

## Report Generation

### Markdown Reports

```csharp
using ExperimentFramework.Science.Reporting;

public class ReportService
{
    private readonly IExperimentReporter _reporter;

    public ReportService(IExperimentReporter reporter)
    {
        _reporter = reporter;
    }

    public async Task<string> GenerateReport(ExperimentReport report)
    {
        return await _reporter.GenerateAsync(report);
    }
}
```

Example output:

```markdown
# Experiment Report: checkout-v2

## Summary
- **Status**: Completed
- **Duration**: 14 days
- **Total Subjects**: 10,000

## Results

### Primary Endpoint: purchase_completed

| Metric | Control | Streamlined | Difference |
|--------|---------|-------------|------------|
| Conversion Rate | 29.0% | 37.4% | +8.4pp |
| Sample Size | 5,000 | 5,000 | - |

**Statistical Test**: Chi-Square Test for Independence
- Chi-square: 76.23
- p-value: < 0.0001
- 95% CI: [6.1%, 10.7%]
- **Result**: Statistically significant

**Effect Size**:
- Odds Ratio: 1.47 [1.31, 1.65]
- Relative Risk: 1.29 [1.19, 1.40]

## Conclusion

The streamlined checkout shows a statistically significant improvement
in conversion rate compared to control (37.4% vs 29.0%, p < 0.0001).
```

### JSON Reports

```csharp
var jsonReporter = serviceProvider.GetRequiredService<JsonReporter>();
var json = await jsonReporter.GenerateAsync(report);

// Returns structured JSON for integration with dashboards
```

## Dependency Injection

Register all services at once:

```csharp
services.AddExperimentScience();
```

This registers:

| Service | Implementation |
|---------|----------------|
| `IStatisticalTest` | `TwoSampleTTest` |
| `IPairedStatisticalTest` | `PairedTTest` |
| `IMultiGroupStatisticalTest` | `OneWayAnova` |
| `IEffectSizeCalculator` | `CohensD` |
| `IBinaryEffectSizeCalculator` | `OddsRatio` |
| `IPowerAnalyzer` | `PowerAnalyzer` |
| `IMultipleComparisonCorrection` | `BenjaminiHochbergCorrection` |
| `IExperimentAnalyzer` | `ExperimentAnalyzer` |
| `IExperimentReporter` | `MarkdownReporter` |

## Best Practices

### 1. Define Hypotheses Before Analysis

Specify your hypothesis before looking at data:

```csharp
// Pre-register hypothesis
var hypothesis = new HypothesisDefinition
{
    Name = "Checkout Optimization",
    NullHypothesis = "No difference in conversion between variants",
    AlternativeHypothesis = "Streamlined checkout improves conversion",
    Type = HypothesisType.Superiority,
    PrimaryEndpoint = new Endpoint
    {
        Name = "purchase_completed",
        OutcomeType = OutcomeType.Binary,
        HigherIsBetter = true
    },
    ExpectedEffectSize = 0.05,
    SuccessCriteria = new SuccessCriteria
    {
        Alpha = 0.05,
        Power = 0.80,
        MinimumSampleSize = 1000
    }
};
```

### 2. Check Assumptions

Verify test assumptions before interpreting results:

```csharp
// For t-test: check sample size
if (controlData.Count < 30 || treatmentData.Count < 30)
{
    // Consider Mann-Whitney U instead, or verify normality
    result = MannWhitneyUTest.Instance.Perform(controlData, treatmentData);
}
else
{
    result = TwoSampleTTest.Instance.Perform(controlData, treatmentData);
}

// For chi-square: check expected frequencies
var minExpected = Math.Min(
    (double)details["expected_control_success"],
    (double)details["expected_treatment_success"]);

if (minExpected < 5)
{
    Console.WriteLine("Warning: Expected frequency < 5, consider Fisher's exact test");
}
```

### 3. Report Effect Sizes

Always report effect sizes alongside p-values:

```csharp
var testResult = TwoSampleTTest.Instance.Perform(control, treatment);
var effectSize = CohensD.Instance.Calculate(control, treatment);

Console.WriteLine($"Mean difference: {testResult.PointEstimate:F2}");
Console.WriteLine($"p-value: {testResult.PValue:F4}");
Console.WriteLine($"Effect size (d): {effectSize.Value:F2} ({effectSize.Magnitude})");
Console.WriteLine($"95% CI: [{testResult.ConfidenceIntervalLower:F2}, {testResult.ConfidenceIntervalUpper:F2}]");
```

### 4. Apply Multiple Comparison Corrections

When testing multiple hypotheses:

```csharp
var pValues = results.Select(r => r.PValue).ToArray();
var correctedSignificance = BenjaminiHochbergCorrection.Instance
    .DetermineSignificance(pValues, alpha: 0.05);

for (int i = 0; i < results.Count; i++)
{
    Console.WriteLine($"{results[i].TestName}: " +
        $"p={pValues[i]:F4}, significant={correctedSignificance[i]}");
}
```

### 5. Use Appropriate Sample Sizes

See [Power Analysis](power-analysis.md) for calculating required sample sizes.

## Common Pitfalls

### Peeking at Results

Don't stop an experiment early when you see significance:

```csharp
// Bad - stopping early inflates false positive rate
if (result.IsSignificant)
{
    StopExperiment(); // DON'T DO THIS
}

// Good - run to predetermined sample size
if (currentSamples >= requiredSampleSize)
{
    var result = AnalyzeExperiment();
}
```

### P-Hacking

Don't test multiple metrics until you find significance:

```csharp
// Bad - testing many metrics inflates false positives
foreach (var metric in allMetrics)
{
    var result = Test(metric);
    if (result.IsSignificant)
    {
        Report(result); // Cherry-picking
    }
}

// Good - pre-specify primary endpoint, correct for multiple tests
var primaryResult = Test(primaryMetric);
var secondaryPValues = secondaryMetrics.Select(m => Test(m).PValue).ToArray();
var corrected = BenjaminiHochbergCorrection.Instance.DetermineSignificance(secondaryPValues, 0.05);
```

### Ignoring Effect Size

A significant result doesn't mean a meaningful difference:

```csharp
// Statistically significant but practically meaningless
// p = 0.01, but effect size = 0.05 (negligible)
if (result.IsSignificant && effectSize.Magnitude == EffectSizeMagnitude.Negligible)
{
    Console.WriteLine("Warning: Statistically significant but trivial effect");
}
```

## See Also

- [Data Collection](data-collection.md) - Collecting experiment outcomes
- [Hypothesis Testing](hypothesis-testing.md) - Defining and testing hypotheses
- [Power Analysis](power-analysis.md) - Sample size calculation
- [Metrics](metrics.md) - Real-time operational metrics
