using ExperimentFramework.Science.Reporting;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;

namespace ExperimentFramework.Science.EffectSize;

/// <summary>
/// Calculates Cohen's d effect size for continuous outcomes.
/// </summary>
/// <remarks>
/// <para>
/// Cohen's d measures the standardized difference between two means,
/// expressed in standard deviation units.
/// </para>
/// <para>
/// Formula: d = (M2 - M1) / Spooled
/// </para>
/// <para>
/// Interpretation (Cohen, 1988):
/// <list type="bullet">
/// <item><description>|d| &lt; 0.2: Negligible</description></item>
/// <item><description>0.2 ≤ |d| &lt; 0.5: Small</description></item>
/// <item><description>0.5 ≤ |d| &lt; 0.8: Medium</description></item>
/// <item><description>|d| ≥ 0.8: Large</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class CohensD : IEffectSizeCalculator
{
    /// <summary>
    /// The singleton instance.
    /// </summary>
    public static CohensD Instance { get; } = new();

    /// <inheritdoc />
    public string Name => "Cohen's d";

    /// <inheritdoc />
    public EffectSizeResult Calculate(IReadOnlyList<double> controlData, IReadOnlyList<double> treatmentData)
    {
        ArgumentNullException.ThrowIfNull(controlData);
        ArgumentNullException.ThrowIfNull(treatmentData);

        if (controlData.Count < 2)
            throw new ArgumentException("Control data must have at least 2 observations.", nameof(controlData));
        if (treatmentData.Count < 2)
            throw new ArgumentException("Treatment data must have at least 2 observations.", nameof(treatmentData));

        var n1 = controlData.Count;
        var n2 = treatmentData.Count;

        var mean1 = controlData.Mean();
        var mean2 = treatmentData.Mean();

        var var1 = controlData.Variance();
        var var2 = treatmentData.Variance();

        // Pooled standard deviation
        var pooledVar = ((n1 - 1) * var1 + (n2 - 1) * var2) / (n1 + n2 - 2);
        var pooledSd = Math.Sqrt(pooledVar);

        // Cohen's d
        var d = (mean2 - mean1) / pooledSd;

        // Standard error of d (approximation)
        // Cast to double before multiplication to avoid potential integer overflow
        var se = Math.Sqrt((n1 + n2) / ((double)n1 * n2) + d * d / (2.0 * (n1 + n2)));

        // 95% confidence interval
        var normal = new Normal(0, 1);
        var z = normal.InverseCumulativeDistribution(0.975);
        var ciLower = d - z * se;
        var ciUpper = d + z * se;

        return new EffectSizeResult
        {
            MeasureName = Name,
            Value = d,
            Magnitude = EffectSizeExtensions.InterpretCohensD(d),
            ConfidenceIntervalLower = ciLower,
            ConfidenceIntervalUpper = ciUpper
        };
    }
}
