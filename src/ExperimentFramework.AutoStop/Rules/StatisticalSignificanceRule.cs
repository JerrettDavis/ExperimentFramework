namespace ExperimentFramework.AutoStop.Rules;

/// <summary>
/// Stopping rule based on statistical significance using z-test.
/// </summary>
public sealed class StatisticalSignificanceRule : IStoppingRule
{
    private readonly double _confidenceLevel;
    private readonly long _minimumSamples;

    /// <summary>
    /// Creates a statistical significance rule.
    /// </summary>
    /// <param name="confidenceLevel">The required confidence level (e.g., 0.95 for 95%).</param>
    /// <param name="minimumSamples">Minimum samples per variant before checking significance.</param>
    public StatisticalSignificanceRule(double confidenceLevel = 0.95, long minimumSamples = 100)
    {
        _confidenceLevel = confidenceLevel;
        _minimumSamples = minimumSamples;
    }

    /// <inheritdoc />
    public string Name => "StatisticalSignificance";

    /// <inheritdoc />
    public StoppingDecision Evaluate(ExperimentData data)
    {
        if (data.Variants.Count < 2)
        {
            return new StoppingDecision(false, "Need at least 2 variants for comparison");
        }

        var control = data.Variants.FirstOrDefault(v => v.IsControl);
        if (control == null)
        {
            control = data.Variants[0];
        }

        // Check minimum samples
        if (data.Variants.Any(v => v.SampleSize < _minimumSamples))
        {
            return new StoppingDecision(false, "Minimum sample size not reached");
        }

        // Find the best performing variant vs control
        VariantData? bestVariant = null;
        double highestZScore = 0;

        foreach (var variant in data.Variants.Where(v => !v.IsControl))
        {
            var zScore = CalculateZScore(control, variant);
            if (Math.Abs(zScore) > Math.Abs(highestZScore))
            {
                highestZScore = zScore;
                bestVariant = variant;
            }
        }

        if (bestVariant == null)
        {
            return new StoppingDecision(false, "No treatment variants found");
        }

        var pValue = CalculatePValue(highestZScore);
        var confidence = 1 - pValue;
        var isSignificant = confidence >= _confidenceLevel;

        if (isSignificant)
        {
            var winner = highestZScore > 0 ? bestVariant.Key : control.Key;
            return new StoppingDecision(
                true,
                $"Statistical significance reached (p={pValue:F4})",
                winner,
                confidence);
        }

        return new StoppingDecision(
            false,
            $"Not yet significant (p={pValue:F4}, need {1 - _confidenceLevel:F4})");
    }

    private static double CalculateZScore(VariantData control, VariantData treatment)
    {
        var p1 = control.ConversionRate;
        var p2 = treatment.ConversionRate;
        var n1 = control.SampleSize;
        var n2 = treatment.SampleSize;

        // Pooled probability
        var pPooled = (double)(control.Successes + treatment.Successes) / (n1 + n2);
        var se = Math.Sqrt(pPooled * (1 - pPooled) * (1.0 / n1 + 1.0 / n2));

        if (se < 0.0001) return 0;

        return (p2 - p1) / se;
    }

    private static double CalculatePValue(double zScore)
    {
        // Two-tailed p-value using error function approximation
        var absZ = Math.Abs(zScore);
        return 2 * (1 - NormalCdf(absZ));
    }

    private static double NormalCdf(double x)
    {
        // Approximation of the standard normal CDF
        const double a1 = 0.254829592;
        const double a2 = -0.284496736;
        const double a3 = 1.421413741;
        const double a4 = -1.453152027;
        const double a5 = 1.061405429;
        const double p = 0.3275911;

        var sign = x < 0 ? -1 : 1;
        x = Math.Abs(x) / Math.Sqrt(2);

        var t = 1.0 / (1.0 + p * x);
        var y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

        return 0.5 * (1.0 + sign * y);
    }
}
