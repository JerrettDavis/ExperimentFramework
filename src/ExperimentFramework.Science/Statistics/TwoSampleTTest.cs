using ExperimentFramework.Science.Models.Results;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;

namespace ExperimentFramework.Science.Statistics;

/// <summary>
/// Welch's t-test for comparing means of two independent samples.
/// </summary>
/// <remarks>
/// <para>
/// This test does not assume equal variances (heteroscedastic test) and is more robust
/// than Student's t-test when sample sizes or variances differ between groups.
/// </para>
/// <para>
/// Assumptions:
/// <list type="bullet">
/// <item><description>Both samples are independent</description></item>
/// <item><description>Both samples are approximately normally distributed (or n > 30)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class TwoSampleTTest : IStatisticalTest
{
    /// <summary>
    /// The singleton instance of the Welch's t-test.
    /// </summary>
    public static TwoSampleTTest Instance { get; } = new();

    /// <inheritdoc />
    public string Name => "Welch's Two-Sample t-Test";

    /// <inheritdoc />
    public StatisticalTestResult Perform(
        IReadOnlyList<double> controlData,
        IReadOnlyList<double> treatmentData,
        double alpha = 0.05,
        AlternativeHypothesisType alternativeType = AlternativeHypothesisType.TwoSided)
    {
        ArgumentNullException.ThrowIfNull(controlData);
        ArgumentNullException.ThrowIfNull(treatmentData);

        if (controlData.Count < 2)
            throw new ArgumentException("Control data must have at least 2 observations.", nameof(controlData));
        if (treatmentData.Count < 2)
            throw new ArgumentException("Treatment data must have at least 2 observations.", nameof(treatmentData));
        if (alpha is <= 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(alpha), "Alpha must be between 0 and 1 (exclusive).");

        var n1 = controlData.Count;
        var n2 = treatmentData.Count;

        var mean1 = controlData.Mean();
        var mean2 = treatmentData.Mean();

        var var1 = controlData.Variance();
        var var2 = treatmentData.Variance();

        // Welch's t-statistic
        var pooledStdError = Math.Sqrt(var1 / n1 + var2 / n2);
        var tStatistic = (mean2 - mean1) / pooledStdError;

        // Welch-Satterthwaite degrees of freedom
        var numerator = Math.Pow(var1 / n1 + var2 / n2, 2);
        var denominator = Math.Pow(var1 / n1, 2) / (n1 - 1) + Math.Pow(var2 / n2, 2) / (n2 - 1);
        var df = numerator / denominator;

        // Calculate p-value based on alternative hypothesis type
        var tDist = new StudentT(0, 1, df);
        var pValue = alternativeType switch
        {
            AlternativeHypothesisType.TwoSided => 2 * (1 - tDist.CumulativeDistribution(Math.Abs(tStatistic))),
            AlternativeHypothesisType.Greater => 1 - tDist.CumulativeDistribution(tStatistic),
            AlternativeHypothesisType.Less => tDist.CumulativeDistribution(tStatistic),
            _ => throw new ArgumentOutOfRangeException(nameof(alternativeType))
        };

        // Confidence interval for the difference in means
        var tCritical = alternativeType == AlternativeHypothesisType.TwoSided
            ? tDist.InverseCumulativeDistribution(1 - alpha / 2)
            : tDist.InverseCumulativeDistribution(1 - alpha);

        var pointEstimate = mean2 - mean1;
        var marginOfError = tCritical * pooledStdError;

        double ciLower, ciUpper;
        switch (alternativeType)
        {
            case AlternativeHypothesisType.TwoSided:
                ciLower = pointEstimate - marginOfError;
                ciUpper = pointEstimate + marginOfError;
                break;
            case AlternativeHypothesisType.Greater:
                ciLower = pointEstimate - marginOfError;
                ciUpper = double.PositiveInfinity;
                break;
            case AlternativeHypothesisType.Less:
                ciLower = double.NegativeInfinity;
                ciUpper = pointEstimate + marginOfError;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(alternativeType));
        }

        return new StatisticalTestResult
        {
            TestName = Name,
            TestStatistic = tStatistic,
            PValue = pValue,
            Alpha = alpha,
            ConfidenceIntervalLower = ciLower,
            ConfidenceIntervalUpper = ciUpper,
            PointEstimate = pointEstimate,
            DegreesOfFreedom = df,
            AlternativeType = alternativeType,
            SampleSizes = new Dictionary<string, int>
            {
                ["control"] = n1,
                ["treatment"] = n2
            },
            Details = new Dictionary<string, object>
            {
                ["control_mean"] = mean1,
                ["treatment_mean"] = mean2,
                ["control_variance"] = var1,
                ["treatment_variance"] = var2,
                ["pooled_standard_error"] = pooledStdError
            }
        };
    }
}
