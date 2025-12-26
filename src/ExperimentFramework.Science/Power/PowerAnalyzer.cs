using ExperimentFramework.Science.Reporting;
using MathNet.Numerics.Distributions;

namespace ExperimentFramework.Science.Power;

/// <summary>
/// Provides statistical power analysis for experiment design.
/// </summary>
/// <remarks>
/// Uses standard formulas for two-sample comparisons:
/// <list type="bullet">
/// <item><description>Continuous outcomes: Based on t-test assumptions</description></item>
/// <item><description>Binary outcomes: Based on z-test for proportions</description></item>
/// </list>
/// </remarks>
public sealed class PowerAnalyzer : IPowerAnalyzer
{
    /// <summary>
    /// The singleton instance of the power analyzer.
    /// </summary>
    public static PowerAnalyzer Instance { get; } = new();

    /// <inheritdoc />
    public int CalculateSampleSize(
        double effectSize,
        double power = 0.80,
        double alpha = 0.05,
        PowerOptions? options = null)
    {
        ValidateInputs(effectSize, power, alpha);

        options ??= new PowerOptions();
        var normal = new Normal(0, 1);

        // Z values
        var zAlpha = options.OneSided
            ? normal.InverseCumulativeDistribution(1 - alpha)
            : normal.InverseCumulativeDistribution(1 - alpha / 2);
        var zBeta = normal.InverseCumulativeDistribution(power);

        // Sample size calculation: binary proportions vs continuous means
        var n = options.OutcomeType == PowerOutcomeType.Binary && options.BaselineProportion.HasValue
            ? CalculateBinarySampleSize(effectSize, options.BaselineProportion.Value, zAlpha, zBeta, options.AllocationRatio)
            : CalculateContinuousSampleSize(effectSize, zAlpha, zBeta, options.AllocationRatio);

        return (int)Math.Ceiling(n);
    }

    /// <inheritdoc />
    public double CalculatePower(
        int sampleSizePerGroup,
        double effectSize,
        double alpha = 0.05,
        PowerOptions? options = null)
    {
        if (sampleSizePerGroup < 2)
            throw new ArgumentOutOfRangeException(nameof(sampleSizePerGroup), "Sample size must be at least 2.");
        ValidateEffectSize(effectSize);
        ValidateAlpha(alpha);

        options ??= new PowerOptions();
        var normal = new Normal(0, 1);

        var zAlpha = options.OneSided
            ? normal.InverseCumulativeDistribution(1 - alpha)
            : normal.InverseCumulativeDistribution(1 - alpha / 2);

        // Power calculation: binary proportions vs continuous means
        var power = options.OutcomeType == PowerOutcomeType.Binary && options.BaselineProportion.HasValue
            ? CalculateBinaryPower(sampleSizePerGroup, effectSize, options.BaselineProportion.Value, zAlpha, options.AllocationRatio)
            : CalculateContinuousPower(sampleSizePerGroup, effectSize, zAlpha, options.AllocationRatio);

        return Math.Min(1.0, Math.Max(0.0, power));
    }

    /// <inheritdoc />
    public double CalculateMinimumDetectableEffect(
        int sampleSizePerGroup,
        double power = 0.80,
        double alpha = 0.05,
        PowerOptions? options = null)
    {
        if (sampleSizePerGroup < 2)
            throw new ArgumentOutOfRangeException(nameof(sampleSizePerGroup), "Sample size must be at least 2.");
        ValidatePower(power);
        ValidateAlpha(alpha);

        options ??= new PowerOptions();
        var normal = new Normal(0, 1);

        var zAlpha = options.OneSided
            ? normal.InverseCumulativeDistribution(1 - alpha)
            : normal.InverseCumulativeDistribution(1 - alpha / 2);
        var zBeta = normal.InverseCumulativeDistribution(power);

        // For continuous outcomes
        // effect = (z_alpha + z_beta) * sqrt(2/n)
        var k = options.AllocationRatio;
        var mde = (zAlpha + zBeta) * Math.Sqrt((1 + 1 / k) / sampleSizePerGroup);

        return mde;
    }

    /// <inheritdoc />
    public PowerAnalysisResult Analyze(
        int currentSampleSizePerGroup,
        double effectSize,
        double targetPower = 0.80,
        double alpha = 0.05,
        PowerOptions? options = null)
    {
        if (currentSampleSizePerGroup < 1)
            throw new ArgumentOutOfRangeException(nameof(currentSampleSizePerGroup), "Sample size must be at least 1.");

        options ??= new PowerOptions();

        var achievedPower = currentSampleSizePerGroup >= 2
            ? CalculatePower(currentSampleSizePerGroup, effectSize, alpha, options)
            : 0.0;

        var requiredSampleSize = effectSize > 0
            ? CalculateSampleSize(effectSize, targetPower, alpha, options)
            : (int?)null;

        var mde = currentSampleSizePerGroup >= 2
            ? CalculateMinimumDetectableEffect(currentSampleSizePerGroup, targetPower, alpha, options)
            : (double?)null;

        return new PowerAnalysisResult
        {
            AchievedPower = achievedPower,
            RequiredSampleSize = requiredSampleSize,
            CurrentSampleSize = currentSampleSizePerGroup,
            IsAdequatelyPowered = achievedPower >= targetPower,
            MinimumDetectableEffect = mde,
            AssumedEffectSize = effectSize,
            Alpha = alpha,
            TargetPower = targetPower
        };
    }

    private static double CalculateContinuousSampleSize(double effectSize, double zAlpha, double zBeta, double k)
    {
        // n = (z_alpha + z_beta)^2 * (1 + 1/k) / d^2
        // where d is Cohen's d effect size
        var numerator = Math.Pow(zAlpha + zBeta, 2) * (1 + 1 / k);
        var denominator = Math.Pow(effectSize, 2);
        return numerator / denominator;
    }

    private static double CalculateBinarySampleSize(double effectSize, double p1, double zAlpha, double zBeta, double k)
    {
        // p2 = p1 + effectSize
        var p2 = p1 + effectSize;

        // Pooled proportion
        var pBar = (p1 + k * p2) / (1 + k);

        // Sample size formula for comparing proportions
        var numerator = Math.Pow(
            zAlpha * Math.Sqrt((1 + 1 / k) * pBar * (1 - pBar)) +
            zBeta * Math.Sqrt(p1 * (1 - p1) + p2 * (1 - p2) / k), 2);
        var denominator = Math.Pow(p2 - p1, 2);

        return numerator / denominator;
    }

    private static double CalculateContinuousPower(int n, double effectSize, double zAlpha, double k)
    {
        var normal = new Normal(0, 1);

        // z_beta = d * sqrt(n / (1 + 1/k)) - z_alpha
        var zBeta = effectSize * Math.Sqrt(n / (1 + 1 / k)) - zAlpha;

        return normal.CumulativeDistribution(zBeta);
    }

    private static double CalculateBinaryPower(int n, double effectSize, double p1, double zAlpha, double k)
    {
        var normal = new Normal(0, 1);
        var p2 = p1 + effectSize;
        var pBar = (p1 + k * p2) / (1 + k);

        var se0 = Math.Sqrt((1 + 1 / k) * pBar * (1 - pBar) / n);
        var se1 = Math.Sqrt((p1 * (1 - p1) + p2 * (1 - p2) / k) / n);

        var zBeta = (Math.Abs(p2 - p1) - zAlpha * se0) / se1;

        return normal.CumulativeDistribution(zBeta);
    }

    private static void ValidateInputs(double effectSize, double power, double alpha)
    {
        ValidateEffectSize(effectSize);
        ValidatePower(power);
        ValidateAlpha(alpha);
    }

    private static void ValidateEffectSize(double effectSize)
    {
        if (effectSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(effectSize), "Effect size must be positive.");
    }

    private static void ValidatePower(double power)
    {
        if (power is <= 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(power), "Power must be between 0 and 1 (exclusive).");
    }

    private static void ValidateAlpha(double alpha)
    {
        if (alpha is <= 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(alpha), "Alpha must be between 0 and 1 (exclusive).");
    }
}
