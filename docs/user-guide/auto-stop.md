# Automatic Stopping Rules

The AutoStop package provides intelligent stopping rules that automatically determine when an experiment has reached statistical significance or other stopping criteria. This prevents both premature conclusions and unnecessarily long experiments.

## Installation

```bash
dotnet add package ExperimentFramework.AutoStop
```

## Quick Start

```csharp
services.AddExperimentAutoStop(options =>
{
    options.MinimumSampleSize = 1000;
    options.ConfidenceLevel = 0.95;
    options.CheckInterval = TimeSpan.FromMinutes(5);
});
```

## Core Concepts

### Stopping Decision

Each rule evaluation returns a decision:

```csharp
public readonly record struct StoppingDecision(
    bool ShouldStop,           // Whether to stop the experiment
    string? Reason,            // Explanation for the decision
    string? WinningVariant,    // The winning variant (if determined)
    double? Confidence         // Confidence level achieved
);
```

### Experiment Data

Rules evaluate experiment data:

```csharp
public sealed class ExperimentData
{
    public required string ExperimentName { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required IReadOnlyList<VariantData> Variants { get; init; }
}

public sealed class VariantData
{
    public required string Key { get; init; }
    public bool IsControl { get; init; }
    public long SampleSize { get; set; }
    public long Successes { get; set; }
    public double ConversionRate { get; }  // Successes / SampleSize
    public double ValueSum { get; set; }   // For continuous metrics
    public double Mean { get; }            // For continuous metrics
    public double Variance { get; }        // For continuous metrics
}
```

## Built-in Rules

### Minimum Sample Size Rule

Ensures sufficient data before any stopping decision:

```csharp
services.AddStoppingRule<MinimumSampleSizeRule>();

// Or with custom threshold
services.AddExperimentAutoStop(options =>
{
    options.MinimumSampleSize = 1000;  // Per variant
    options.Rules.Add(new MinimumSampleSizeRule(1000));
});
```

### Statistical Significance Rule

Uses z-test for proportion comparison:

```csharp
services.AddExperimentAutoStop(options =>
{
    options.ConfidenceLevel = 0.95;  // 95% confidence
    options.Rules.Add(new StatisticalSignificanceRule(0.95, minSampleSize: 100));
});
```

**How it works:**
1. Computes z-score for the difference in conversion rates
2. Calculates p-value from z-score
3. Determines if p-value < (1 - confidence level)
4. Identifies the winning variant

## Configuration

### Full Configuration Example

```csharp
services.AddExperimentAutoStop(options =>
{
    // Sample requirements
    options.MinimumSampleSize = 1000;

    // Statistical settings
    options.ConfidenceLevel = 0.95;

    // Evaluation frequency
    options.CheckInterval = TimeSpan.FromMinutes(5);

    // Maximum runtime (optional)
    options.MaxDuration = TimeSpan.FromDays(14);

    // Custom rules
    options.Rules.Clear();  // Remove defaults
    options.Rules.Add(new MinimumSampleSizeRule(1000));
    options.Rules.Add(new StatisticalSignificanceRule(0.95, 1000));
    options.Rules.Add(new MaxDurationRule(TimeSpan.FromDays(14)));
});
```

### Per-Experiment Configuration

```csharp
.Define<ICheckoutFlow>(exp => exp
    .WithAutoStop(stop =>
    {
        stop.MinimumSampleSize = 5000;
        stop.ConfidenceLevel = 0.99;  // Higher for critical paths
        stop.MaxDuration = TimeSpan.FromDays(7);
    })
    .AddControl<CurrentCheckout>("current")
    .AddCondition<NewCheckout>("new"))
```

## Custom Stopping Rules

Implement `IStoppingRule` for custom logic:

### Practical Significance Rule

Stop only if the effect size is meaningful:

```csharp
public class PracticalSignificanceRule : IStoppingRule
{
    private readonly double _minimumEffectSize;

    public PracticalSignificanceRule(double minimumEffectSize)
    {
        _minimumEffectSize = minimumEffectSize;
    }

    public string Name => "PracticalSignificance";

    public StoppingDecision Evaluate(ExperimentData data)
    {
        var control = data.Variants.FirstOrDefault(v => v.IsControl);
        var treatment = data.Variants.FirstOrDefault(v => !v.IsControl);

        if (control == null || treatment == null)
            return new StoppingDecision(false, "Missing control or treatment");

        var controlRate = control.ConversionRate;
        var treatmentRate = treatment.ConversionRate;

        // Calculate relative lift
        var relativeLift = controlRate > 0
            ? (treatmentRate - controlRate) / controlRate
            : 0;

        var absoluteLift = Math.Abs(relativeLift);

        if (absoluteLift >= _minimumEffectSize)
        {
            var winner = relativeLift > 0 ? treatment.Key : control.Key;
            return new StoppingDecision(
                true,
                $"Practical significance achieved: {absoluteLift:P1} lift (min: {_minimumEffectSize:P1})",
                winner,
                absoluteLift);
        }

        return new StoppingDecision(
            false,
            $"Effect size {absoluteLift:P1} below threshold {_minimumEffectSize:P1}");
    }
}

// Register
services.AddStoppingRule<PracticalSignificanceRule>();
```

### Maximum Duration Rule

Stop after a fixed time regardless of significance:

```csharp
public class MaxDurationRule : IStoppingRule
{
    private readonly TimeSpan _maxDuration;

    public MaxDurationRule(TimeSpan maxDuration)
    {
        _maxDuration = maxDuration;
    }

    public string Name => "MaxDuration";

    public StoppingDecision Evaluate(ExperimentData data)
    {
        var elapsed = DateTimeOffset.UtcNow - data.StartedAt;

        if (elapsed >= _maxDuration)
        {
            // Find best performer even without significance
            var best = data.Variants.MaxBy(v => v.ConversionRate);
            return new StoppingDecision(
                true,
                $"Maximum duration ({_maxDuration.TotalDays:F1} days) reached",
                best?.Key);
        }

        var remaining = _maxDuration - elapsed;
        return new StoppingDecision(false, $"Time remaining: {remaining.TotalDays:F1} days");
    }
}
```

### Sequential Analysis Rule

Implements group sequential analysis for early stopping:

```csharp
public class SequentialAnalysisRule : IStoppingRule
{
    private readonly int[] _checkpoints;
    private readonly double[] _boundaries;

    public SequentialAnalysisRule()
    {
        // O'Brien-Fleming spending function boundaries
        _checkpoints = new[] { 500, 1000, 2000, 5000 };
        _boundaries = new[] { 4.56, 3.23, 2.63, 2.29 };
    }

    public string Name => "SequentialAnalysis";

    public StoppingDecision Evaluate(ExperimentData data)
    {
        var totalSamples = data.Variants.Sum(v => v.SampleSize);

        // Find current checkpoint
        for (int i = 0; i < _checkpoints.Length; i++)
        {
            if (totalSamples >= _checkpoints[i])
            {
                var zScore = CalculateZScore(data);
                if (Math.Abs(zScore) >= _boundaries[i])
                {
                    var winner = DetermineWinner(data, zScore);
                    return new StoppingDecision(
                        true,
                        $"Sequential boundary crossed at n={totalSamples}, z={zScore:F2}",
                        winner);
                }
            }
        }

        return new StoppingDecision(false, "Sequential boundaries not crossed");
    }

    private double CalculateZScore(ExperimentData data) { /* ... */ }
    private string DetermineWinner(ExperimentData data, double zScore) { /* ... */ }
}
```

## Evaluation Service

### Automatic Evaluation

```csharp
public class AutoStopBackgroundService : BackgroundService
{
    private readonly IAutoStopEvaluator _evaluator;
    private readonly IExperimentRegistry _registry;
    private readonly IOptions<AutoStopOptions> _options;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var experiment in _registry.GetActiveExperiments())
            {
                var data = await LoadExperimentDataAsync(experiment.Name);
                var decision = await _evaluator.EvaluateAsync(data);

                if (decision.ShouldStop)
                {
                    await HandleStoppingDecisionAsync(experiment.Name, decision);
                }
            }

            await Task.Delay(_options.Value.CheckInterval, stoppingToken);
        }
    }

    private async Task HandleStoppingDecisionAsync(string experimentName, StoppingDecision decision)
    {
        _logger.LogInformation(
            "Experiment {Experiment} should stop: {Reason}, Winner: {Winner}",
            experimentName, decision.Reason, decision.WinningVariant);

        // Notify stakeholders
        await _notificationService.NotifyAsync(new ExperimentStoppedNotification
        {
            ExperimentName = experimentName,
            Decision = decision
        });

        // Optionally auto-deactivate
        if (_options.Value.AutoDeactivate)
        {
            await _registry.SetExperimentActiveAsync(experimentName, false);
        }
    }
}
```

### Manual Evaluation

```csharp
public class ExperimentAnalysisController
{
    private readonly IAutoStopEvaluator _evaluator;
    private readonly IExperimentDataService _dataService;

    [HttpGet("{name}/should-stop")]
    public async Task<IActionResult> ShouldStop(string name)
    {
        var data = await _dataService.GetExperimentDataAsync(name);
        var decision = await _evaluator.EvaluateAsync(data);

        return Ok(new
        {
            experimentName = name,
            shouldStop = decision.ShouldStop,
            reason = decision.Reason,
            winner = decision.WinningVariant,
            confidence = decision.Confidence
        });
    }
}
```

## Integration

### With Audit Logging

```csharp
public class AuditingAutoStopEvaluator : IAutoStopEvaluator
{
    private readonly IAutoStopEvaluator _inner;
    private readonly IAuditSink _auditSink;

    public async Task<StoppingDecision> EvaluateAsync(ExperimentData data)
    {
        var decision = await _inner.EvaluateAsync(data);

        if (decision.ShouldStop)
        {
            await _auditSink.RecordAsync(new AuditEvent
            {
                EventType = "AutoStopTriggered",
                ExperimentName = data.ExperimentName,
                Timestamp = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["Reason"] = decision.Reason ?? "",
                    ["WinningVariant"] = decision.WinningVariant ?? "",
                    ["Confidence"] = decision.Confidence ?? 0
                }
            });
        }

        return decision;
    }
}
```

### With Notifications

```csharp
services.AddExperimentAutoStop(options =>
{
    options.OnStopDecision = async (decision, data) =>
    {
        if (decision.ShouldStop)
        {
            await _slackNotifier.SendAsync(
                $"Experiment *{data.ExperimentName}* should stop!\n" +
                $"Reason: {decision.Reason}\n" +
                $"Winner: {decision.WinningVariant ?? "None"}");
        }
    };
});
```

## Monitoring Dashboard

```csharp
public class AutoStopDashboardService
{
    public async Task<ExperimentStatus> GetStatusAsync(string experimentName)
    {
        var data = await _dataService.GetExperimentDataAsync(experimentName);
        var decisions = new List<RuleDecision>();

        foreach (var rule in _rules)
        {
            var decision = rule.Evaluate(data);
            decisions.Add(new RuleDecision
            {
                RuleName = rule.Name,
                ShouldStop = decision.ShouldStop,
                Reason = decision.Reason
            });
        }

        return new ExperimentStatus
        {
            ExperimentName = experimentName,
            StartedAt = data.StartedAt,
            TotalSamples = data.Variants.Sum(v => v.SampleSize),
            Variants = data.Variants.Select(v => new VariantStatus
            {
                Key = v.Key,
                SampleSize = v.SampleSize,
                ConversionRate = v.ConversionRate,
                IsLeading = v.ConversionRate == data.Variants.Max(x => x.ConversionRate)
            }).ToList(),
            RuleDecisions = decisions,
            OverallShouldStop = decisions.Any(d => d.ShouldStop)
        };
    }
}
```

## Best Practices

1. **Combine rules**: Use multiple rules (minimum sample + significance + practical significance)
2. **Set realistic minimums**: Don't check for significance with too few samples
3. **Consider business impact**: Higher stakes = higher confidence requirements
4. **Monitor false positives**: Track experiments that "won" but didn't deliver
5. **Document stopping criteria**: Make rules clear to stakeholders before starting

## Common Pitfalls

### Peeking Problem

**Issue**: Checking results too frequently inflates false positive rate.

**Solution**: Use sequential analysis or predefined checkpoints:

```csharp
options.CheckInterval = TimeSpan.FromHours(24);  // Daily checks only
// Or use SequentialAnalysisRule with spending functions
```

### Multiple Comparisons

**Issue**: Testing many variants increases false positive rate.

**Solution**: Apply Bonferroni correction:

```csharp
public class BonferroniCorrectedSignificanceRule : IStoppingRule
{
    private readonly double _baseAlpha;

    public StoppingDecision Evaluate(ExperimentData data)
    {
        var numComparisons = data.Variants.Count(v => !v.IsControl);
        var correctedAlpha = _baseAlpha / numComparisons;

        // Use corrected alpha for significance testing
        // ...
    }
}
```

### Ignoring Practical Significance

**Issue**: Stopping on statistical significance with tiny effect sizes.

**Solution**: Require minimum effect size:

```csharp
options.Rules.Add(new PracticalSignificanceRule(minimumEffectSize: 0.05));  // 5% lift
```

## Troubleshooting

### Experiments never stop

**Symptom**: Experiments run indefinitely without reaching significance.

**Cause**: Effect size is too small for the sample size.

**Solution**: Either increase traffic, accept smaller confidence, or use a maximum duration rule:

```csharp
options.Rules.Add(new MaxDurationRule(TimeSpan.FromDays(30)));
```

### Experiments stop too quickly

**Symptom**: Winners declared with suspiciously high frequency.

**Cause**: Minimum sample size too low or checking too frequently.

**Solution**: Increase minimum requirements:

```csharp
options.MinimumSampleSize = 5000;
options.CheckInterval = TimeSpan.FromHours(24);
```

### Conflicting rule decisions

**Symptom**: Different rules give different recommendations.

**Cause**: Rules have different criteria.

**Solution**: Define clear precedence:

```csharp
public async Task<StoppingDecision> EvaluateAsync(ExperimentData data)
{
    // Minimum sample must pass first
    var minSampleDecision = _minSampleRule.Evaluate(data);
    if (!minSampleDecision.ShouldStop) return minSampleDecision;

    // Then check significance
    var significanceDecision = _significanceRule.Evaluate(data);
    if (significanceDecision.ShouldStop) return significanceDecision;

    // Finally check max duration
    return _maxDurationRule.Evaluate(data);
}
```
