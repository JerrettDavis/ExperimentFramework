using ExperimentFramework.Science.Models.Results;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;

namespace ExperimentFramework.Science.Statistics;

/// <summary>
/// Paired t-test for comparing means of two related samples.
/// </summary>
/// <remarks>
/// <para>
/// Use this test when you have paired observations (e.g., before/after measurements
/// on the same subjects, or matched pairs).
/// </para>
/// <para>
/// Assumptions:
/// <list type="bullet">
/// <item><description>Observations are paired</description></item>
/// <item><description>Differences are approximately normally distributed (or n > 30)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class PairedTTest : IPairedStatisticalTest
{
    /// <summary>
    /// The singleton instance of the paired t-test.
    /// </summary>
    public static PairedTTest Instance { get; } = new();

    /// <inheritdoc />
    public string Name => "Paired t-Test";

    /// <inheritdoc />
    public StatisticalTestResult Perform(
        IReadOnlyList<double> before,
        IReadOnlyList<double> after,
        double alpha = 0.05,
        AlternativeHypothesisType alternativeType = AlternativeHypothesisType.TwoSided)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        if (before.Count < 2)
            throw new ArgumentException("Before data must have at least 2 observations.", nameof(before));
        if (after.Count != before.Count)
            throw new ArgumentException("After data must have the same number of observations as before data.", nameof(after));
        if (alpha is <= 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(alpha), "Alpha must be between 0 and 1 (exclusive).");

        var n = before.Count;

        // Calculate differences (after - before)
        var differences = new double[n];
        for (var i = 0; i < n; i++)
        {
            differences[i] = after[i] - before[i];
        }

        var meanDiff = differences.Mean();
        var stdDiff = differences.StandardDeviation();
        var stdError = stdDiff / Math.Sqrt(n);

        // t-statistic
        var tStatistic = meanDiff / stdError;
        var df = n - 1;

        // Calculate p-value based on alternative hypothesis type
        var tDist = new StudentT(0, 1, df);
        var pValue = alternativeType switch
        {
            AlternativeHypothesisType.TwoSided => 2 * (1 - tDist.CumulativeDistribution(Math.Abs(tStatistic))),
            AlternativeHypothesisType.Greater => 1 - tDist.CumulativeDistribution(tStatistic),
            AlternativeHypothesisType.Less => tDist.CumulativeDistribution(tStatistic),
            _ => throw new ArgumentOutOfRangeException(nameof(alternativeType))
        };

        // Confidence interval for the mean difference
        var tCritical = alternativeType == AlternativeHypothesisType.TwoSided
            ? tDist.InverseCumulativeDistribution(1 - alpha / 2)
            : tDist.InverseCumulativeDistribution(1 - alpha);

        var marginOfError = tCritical * stdError;

        double ciLower, ciUpper;
        switch (alternativeType)
        {
            case AlternativeHypothesisType.TwoSided:
                ciLower = meanDiff - marginOfError;
                ciUpper = meanDiff + marginOfError;
                break;
            case AlternativeHypothesisType.Greater:
                ciLower = meanDiff - marginOfError;
                ciUpper = double.PositiveInfinity;
                break;
            case AlternativeHypothesisType.Less:
                ciLower = double.NegativeInfinity;
                ciUpper = meanDiff + marginOfError;
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
            PointEstimate = meanDiff,
            DegreesOfFreedom = df,
            AlternativeType = alternativeType,
            SampleSizes = new Dictionary<string, int>
            {
                ["pairs"] = n
            },
            Details = new Dictionary<string, object>
            {
                ["mean_difference"] = meanDiff,
                ["std_difference"] = stdDiff,
                ["standard_error"] = stdError,
                ["before_mean"] = before.Mean(),
                ["after_mean"] = after.Mean()
            }
        };
    }
}
