# Hypothesis Testing

Define and test scientific hypotheses with pre-registration support to ensure experimental rigor. The hypothesis testing framework helps prevent p-hacking and post-hoc rationalization.

## Overview

Scientific experimentation follows a structured process:

1. **Define hypothesis** before collecting data
2. **Specify endpoints** and success criteria
3. **Calculate sample size** for adequate power
4. **Run experiment** and collect outcomes
5. **Analyze results** against pre-registered hypothesis

## Why Pre-Register Hypotheses?

Pre-registration prevents:

- **P-hacking**: Testing multiple hypotheses until one is significant
- **HARKing**: Hypothesizing After Results are Known
- **Selective reporting**: Only reporting significant results

By defining hypotheses before analysis, you maintain scientific integrity and produce credible results.

## Hypothesis Types

### Superiority Test

Test if treatment is better than control:

```csharp
using ExperimentFramework.Science.Builders;
using ExperimentFramework.Data.Models;

var hypothesis = new HypothesisBuilder("checkout-optimization")
    .Superiority()
    .NullHypothesis("The new checkout has no effect on conversion")
    .AlternativeHypothesis("The new checkout increases conversion rate")
    .PrimaryEndpoint("purchase_completed", OutcomeType.Binary, ep => ep
        .Description("Purchase completion rate")
        .HigherIsBetter())
    .ExpectedEffectSize(0.05) // 5% improvement
    .WithSuccessCriteria(c => c
        .Alpha(0.05)
        .Power(0.80)
        .MinimumSampleSize(1000))
    .DefinedNow()
    .Build();
```

### Non-Inferiority Test

Test if treatment is not worse than control (by more than a margin):

```csharp
var hypothesis = new HypothesisBuilder("api-migration")
    .NonInferiority()
    .NullHypothesis("The new API is inferior to the current API")
    .AlternativeHypothesis("The new API is not worse than current by more than 50ms")
    .PrimaryEndpoint("response_time", OutcomeType.Duration, ep => ep
        .Description("API response latency")
        .Unit("milliseconds")
        .LowerIsBetter())
    .ExpectedEffectSize(-10) // Expect 10ms improvement
    .WithSuccessCriteria(c => c
        .Alpha(0.05)
        .Power(0.80)
        .NonInferiorityMargin(50)) // Acceptable if within 50ms
    .DefinedNow()
    .Build();
```

### Equivalence Test

Test if treatment and control are equivalent (within a margin):

```csharp
var hypothesis = new HypothesisBuilder("algorithm-validation")
    .Equivalence()
    .NullHypothesis("The algorithms produce different results")
    .AlternativeHypothesis("The algorithms produce equivalent results")
    .PrimaryEndpoint("accuracy", OutcomeType.Continuous, ep => ep
        .Description("Prediction accuracy")
        .HigherIsBetter())
    .ExpectedEffectSize(0.0) // Expect no difference
    .WithSuccessCriteria(c => c
        .Alpha(0.05)
        .Power(0.80)
        .EquivalenceMargin(0.02)) // Within 2% is equivalent
    .DefinedNow()
    .Build();
```

### Two-Sided Test

Test if there is any difference (direction unknown):

```csharp
var hypothesis = new HypothesisBuilder("layout-experiment")
    .TwoSided()
    .NullHypothesis("The new layout has no effect on engagement")
    .AlternativeHypothesis("The new layout affects engagement (positive or negative)")
    .PrimaryEndpoint("session_duration", OutcomeType.Duration, ep => ep
        .Description("Time spent on site")
        .HigherIsBetter())
    .ExpectedEffectSize(0.3) // Cohen's d = 0.3 (small effect)
    .WithSuccessCriteria(c => c
        .Alpha(0.05)
        .Power(0.80))
    .DefinedNow()
    .Build();
```

## Defining Endpoints

### Primary Endpoint

The main outcome that determines success:

```csharp
.PrimaryEndpoint("conversion", OutcomeType.Binary, ep => ep
    .Description("User completes purchase")
    .HigherIsBetter()
    .ExpectedBaseline(0.25) // Current 25% conversion
    .MinimumImportantDifference(0.03)) // 3pp lift is meaningful
```

### Secondary Endpoints

Additional outcomes that provide context:

```csharp
.PrimaryEndpoint("conversion", OutcomeType.Binary, ...)
.SecondaryEndpoint("revenue", OutcomeType.Continuous, ep => ep
    .Description("Order value")
    .Unit("USD")
    .HigherIsBetter()
    .ExpectedBaseline(50.0)
    .ExpectedVariance(625)) // Std dev = 25
.SecondaryEndpoint("cart_additions", OutcomeType.Count, ep => ep
    .Description("Items added to cart")
    .HigherIsBetter())
.SecondaryEndpoint("checkout_time", OutcomeType.Duration, ep => ep
    .Description("Time to complete checkout")
    .Unit("seconds")
    .LowerIsBetter())
```

## Success Criteria

### Basic Criteria

```csharp
.WithSuccessCriteria(c => c
    .Alpha(0.05)        // 5% significance level
    .Power(0.80)        // 80% power
    .MinimumSampleSize(500))
```

### Advanced Criteria

```csharp
.WithSuccessCriteria(c => c
    .Alpha(0.05)
    .Power(0.80)
    .MinimumSampleSize(1000)
    .MinimumEffectSize(0.02)                 // Reject if effect < 2%
    .PrimaryEndpointOnly()                   // Only primary must be significant
    .WithMultipleComparisonCorrection()      // Apply correction for multiple tests
    .MinimumDuration(TimeSpan.FromDays(14))  // Run at least 2 weeks
    .RequirePositiveEffect())                // Effect must be in expected direction
```

### Endpoint Requirements

```csharp
// Require only primary endpoint significant
.PrimaryEndpointOnly()

// Require all endpoints significant
.AllEndpoints()
```

## Complete Example

### Pre-Registration

```csharp
using ExperimentFramework.Science.Builders;
using ExperimentFramework.Data.Models;

public class ExperimentDefinition
{
    public static HypothesisDefinition CheckoutOptimization()
    {
        return new HypothesisBuilder("checkout-v2-superiority")
            .Description("Testing the streamlined checkout flow's impact on conversion")
            .Superiority()
            .NullHypothesis("The streamlined checkout has no effect on conversion rate")
            .AlternativeHypothesis("The streamlined checkout improves conversion by at least 5%")
            .Rationale("""
                Prior user research showed frustration with the current 5-step checkout.
                The new streamlined flow reduces steps to 2 and pre-fills shipping info.
                Similar changes at Company X showed a 7% conversion lift.
                """)
            .Control("legacy-checkout")
            .Treatment("streamlined-checkout")
            .PrimaryEndpoint("purchase_completed", OutcomeType.Binary, ep => ep
                .Description("User successfully completes a purchase")
                .HigherIsBetter()
                .ExpectedBaseline(0.25)
                .MinimumImportantDifference(0.03))
            .SecondaryEndpoint("checkout_time", OutcomeType.Duration, ep => ep
                .Description("Time from cart to order confirmation")
                .Unit("seconds")
                .LowerIsBetter()
                .ExpectedBaseline(180)
                .ExpectedVariance(3600))
            .SecondaryEndpoint("cart_abandonment", OutcomeType.Binary, ep => ep
                .Description("User abandons cart before purchase")
                .LowerIsBetter()
                .ExpectedBaseline(0.75))
            .ExpectedEffectSize(0.05)
            .WithSuccessCriteria(c => c
                .Alpha(0.05)
                .Power(0.80)
                .MinimumSampleSize(2000)
                .MinimumEffectSize(0.02)
                .PrimaryEndpointOnly()
                .WithMultipleComparisonCorrection()
                .MinimumDuration(TimeSpan.FromDays(14))
                .RequirePositiveEffect())
            .WithMetadata("analyst", "data-team@company.com")
            .WithMetadata("jira_ticket", "EXP-1234")
            .DefinedNow()
            .Build();
    }
}
```

### Running the Experiment

```csharp
using ExperimentFramework.Data;
using ExperimentFramework.Science;

public class ExperimentService
{
    private readonly IOutcomeRecorder _recorder;
    private readonly HypothesisDefinition _hypothesis;

    public ExperimentService(IOutcomeRecorder recorder)
    {
        _recorder = recorder;
        _hypothesis = ExperimentDefinition.CheckoutOptimization();
    }

    public async Task RecordCheckoutOutcome(
        string userId,
        string assignedTrial,
        bool purchaseCompleted,
        TimeSpan checkoutDuration)
    {
        var experimentName = _hypothesis.Name;

        // Record primary endpoint
        await _recorder.RecordBinaryAsync(
            experimentName, assignedTrial, userId,
            _hypothesis.PrimaryEndpoint.Name,
            purchaseCompleted);

        // Record secondary endpoints
        await _recorder.RecordDurationAsync(
            experimentName, assignedTrial, userId,
            "checkout_time",
            checkoutDuration);

        await _recorder.RecordBinaryAsync(
            experimentName, assignedTrial, userId,
            "cart_abandonment",
            !purchaseCompleted);
    }
}
```

### Analyzing Results

```csharp
using ExperimentFramework.Science.Analysis;
using ExperimentFramework.Science.Reporting;

public class AnalysisService
{
    private readonly IExperimentAnalyzer _analyzer;
    private readonly IExperimentReporter _reporter;

    public AnalysisService(
        IExperimentAnalyzer analyzer,
        IExperimentReporter reporter)
    {
        _analyzer = analyzer;
        _reporter = reporter;
    }

    public async Task<string> AnalyzeExperiment(HypothesisDefinition hypothesis)
    {
        var report = await _analyzer.AnalyzeAsync(
            hypothesis.Name,
            hypothesis,
            new AnalysisOptions
            {
                Alpha = hypothesis.SuccessCriteria.Alpha,
                TargetPower = hypothesis.SuccessCriteria.Power,
                ApplyMultipleComparisonCorrection =
                    hypothesis.SuccessCriteria.ApplyMultipleComparisonCorrection
            });

        // Check against success criteria
        var success = EvaluateSuccess(hypothesis, report);

        // Generate report
        return await _reporter.GenerateAsync(report);
    }

    private bool EvaluateSuccess(HypothesisDefinition hypothesis, ExperimentReport report)
    {
        var criteria = hypothesis.SuccessCriteria;
        var primaryResult = report.PrimaryResult;

        if (primaryResult == null)
            return false;

        // Check significance
        if (!primaryResult.IsSignificant)
            return false;

        // Check effect direction
        if (criteria.RequirePositiveEffect)
        {
            var effect = primaryResult.PointEstimate;
            if (hypothesis.PrimaryEndpoint.HigherIsBetter && effect <= 0)
                return false;
            if (!hypothesis.PrimaryEndpoint.HigherIsBetter && effect >= 0)
                return false;
        }

        // Check minimum effect size
        if (criteria.MinimumEffectSize.HasValue)
        {
            var absEffect = Math.Abs(primaryResult.PointEstimate);
            if (absEffect < criteria.MinimumEffectSize.Value)
                return false;
        }

        return true;
    }
}
```

## Storing Hypothesis Definitions

### Snapshot Store

Store hypothesis definitions for auditing:

```csharp
using ExperimentFramework.Science.Models.Snapshots;
using ExperimentFramework.Science.Snapshots;

public class PreRegistrationService
{
    private readonly ISnapshotStore _snapshots;

    public PreRegistrationService(ISnapshotStore snapshots)
    {
        _snapshots = snapshots;
    }

    public async Task PreRegisterHypothesis(HypothesisDefinition hypothesis)
    {
        var snapshot = new ExperimentSnapshot
        {
            Id = Guid.NewGuid().ToString(),
            ExperimentName = hypothesis.Name,
            Timestamp = DateTimeOffset.UtcNow,
            Type = SnapshotType.PreRegistration,
            Hypothesis = hypothesis,
            Environment = new EnvironmentInfo
            {
                ApplicationVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                RuntimeVersion = RuntimeInformation.FrameworkDescription,
                MachineName = Environment.MachineName
            }
        };

        await _snapshots.SaveAsync(snapshot);
    }
}
```

## Best Practices

### 1. Define Before Analysis

Always define hypotheses before looking at data:

```csharp
// Good - hypothesis defined at experiment start
var hypothesis = new HypothesisBuilder("experiment")
    .Superiority()
    .NullHypothesis("No effect")
    .AlternativeHypothesis("Treatment improves conversion by 5%")
    .ExpectedEffectSize(0.05)
    .DefinedNow() // Timestamp for audit trail
    .Build();

// Bad - defining after seeing results
var result = await analyzer.AnalyzeAsync("experiment");
if (result.PrimaryResult?.PValue > 0.05)
{
    // Don't change the hypothesis now!
}
```

### 2. Pre-Specify Effect Size

Base expected effect size on prior evidence:

```csharp
// Good - based on prior research
.ExpectedEffectSize(0.05)
.Rationale("Similar changes showed 5% lift in previous A/B test")

// Bad - arbitrary or optimistic
.ExpectedEffectSize(0.50) // Unrealistic, leads to underpowered tests
```

### 3. Use One Primary Endpoint

Avoid multiple primary endpoints to prevent multiple testing issues:

```csharp
// Good - one primary
.PrimaryEndpoint("conversion", OutcomeType.Binary)
.SecondaryEndpoint("revenue", OutcomeType.Continuous)
.SecondaryEndpoint("time_on_site", OutcomeType.Duration)

// Bad - multiple primaries inflate false positive rate
.PrimaryEndpoint("conversion")
.PrimaryEndpoint("revenue")  // This is really a secondary
```

### 4. Document Rationale

Explain why you expect the treatment to work:

```csharp
.Rationale("""
    Based on:
    1. User research showing checkout friction
    2. Competitor analysis of streamlined flows
    3. Previous test showing 3% lift from removing one step

    We expect the combined improvements to yield 5% conversion lift.
    """)
```

### 5. Set Minimum Sample Size

Ensure adequate sample before analyzing:

```csharp
.WithSuccessCriteria(c => c
    .MinimumSampleSize(2000)
    .MinimumDuration(TimeSpan.FromDays(14)))
```

## Interpreting Results

### Significant Result in Expected Direction

```
Primary Endpoint: purchase_completed
  Control: 25.0%, Treatment: 30.2%
  Difference: +5.2pp, 95% CI [3.1%, 7.3%]
  p-value: 0.0001
  Result: SIGNIFICANT, supports alternative hypothesis

Conclusion: The streamlined checkout increases conversion.
Recommend: Roll out to 100% of users.
```

### Significant Result in Unexpected Direction

```
Primary Endpoint: purchase_completed
  Control: 25.0%, Treatment: 22.1%
  Difference: -2.9pp, 95% CI [-5.1%, -0.7%]
  p-value: 0.0098
  Result: SIGNIFICANT, but effect is negative

Conclusion: The streamlined checkout DECREASES conversion.
Recommend: Investigate why. Do not roll out.
```

### Non-Significant Result

```
Primary Endpoint: purchase_completed
  Control: 25.0%, Treatment: 26.1%
  Difference: +1.1pp, 95% CI [-0.8%, 3.0%]
  p-value: 0.254
  Result: NOT SIGNIFICANT

Conclusion: No evidence the streamlined checkout affects conversion.
Recommend: Consider larger sample or different approach.
```

## See Also

- [Statistical Analysis](statistical-analysis.md) - Statistical test details
- [Power Analysis](power-analysis.md) - Sample size calculation
- [Data Collection](data-collection.md) - Recording outcomes
