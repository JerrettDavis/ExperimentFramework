using ExperimentFramework.Science.Models.Results;
using MathNet.Numerics.Distributions;

namespace ExperimentFramework.Science.Statistics;

/// <summary>
/// Mann-Whitney U test (Wilcoxon rank-sum test) for comparing two independent samples.
/// </summary>
/// <remarks>
/// <para>
/// This is a non-parametric test that does not assume normal distributions.
/// It tests whether the distributions of two groups differ.
/// </para>
/// <para>
/// Use this test when:
/// <list type="bullet">
/// <item><description>Data is ordinal or continuous</description></item>
/// <item><description>Normal distribution assumption is violated</description></item>
/// <item><description>Sample sizes are small</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class MannWhitneyUTest : IStatisticalTest
{
    /// <summary>
    /// The singleton instance of the Mann-Whitney U test.
    /// </summary>
    public static MannWhitneyUTest Instance { get; } = new();

    /// <inheritdoc />
    public string Name => "Mann-Whitney U Test";

    /// <inheritdoc />
    public StatisticalTestResult Perform(
        IReadOnlyList<double> controlData,
        IReadOnlyList<double> treatmentData,
        double alpha = 0.05,
        AlternativeHypothesisType alternativeType = AlternativeHypothesisType.TwoSided)
    {
        ArgumentNullException.ThrowIfNull(controlData);
        ArgumentNullException.ThrowIfNull(treatmentData);

        if (controlData.Count < 1)
            throw new ArgumentException("Control data must have at least 1 observation.", nameof(controlData));
        if (treatmentData.Count < 1)
            throw new ArgumentException("Treatment data must have at least 1 observation.", nameof(treatmentData));
        if (alpha is <= 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(alpha), "Alpha must be between 0 and 1 (exclusive).");

        var n1 = controlData.Count;
        var n2 = treatmentData.Count;

        // Combine and rank all observations
        var combined = new List<(double Value, int Group)>(n1 + n2);
        foreach (var value in controlData)
            combined.Add((value, 0));
        foreach (var value in treatmentData)
            combined.Add((value, 1));

        combined.Sort((a, b) => a.Value.CompareTo(b.Value));

        // Assign ranks with tie handling (average rank method)
        var ranks = AssignRanks(combined);

        // Calculate rank sums
        double r1 = 0, r2 = 0;
        for (var i = 0; i < combined.Count; i++)
        {
            if (combined[i].Group == 0)
                r1 += ranks[i];
            else
                r2 += ranks[i];
        }

        // Calculate U statistics
        var u1 = r1 - n1 * (n1 + 1.0) / 2;
        var u2 = r2 - n2 * (n2 + 1.0) / 2;

        // Use smaller U for the test
        var u = Math.Min(u1, u2);

        // For large samples, use normal approximation
        var meanU = (double)n1 * n2 / 2;
        var stdU = Math.Sqrt((double)n1 * n2 * (n1 + n2 + 1) / 12);

        // Apply tie correction
        var tieCorrection = CalculateTieCorrection(combined);
        if (tieCorrection > 0)
        {
            var n = n1 + n2;
            // Cast to double before multiplication to avoid potential integer overflow
            stdU = Math.Sqrt((double)n1 * n2 / ((double)n * (n - 1)) *
                ((Math.Pow(n, 3) - n) / 12 - tieCorrection));
        }

        // Z-score (with continuity correction)
        var z = (u - meanU + 0.5) / stdU;

        // Calculate p-value based on alternative hypothesis
        var normal = new Normal(0, 1);
        double pValue;
        double zTest;

        // Determine direction based on which U we're using
        var treatmentHigher = u2 < u1;

        switch (alternativeType)
        {
            case AlternativeHypothesisType.TwoSided:
                zTest = Math.Abs(z);
                pValue = 2 * (1 - normal.CumulativeDistribution(zTest));
                break;
            case AlternativeHypothesisType.Greater:
                // Treatment greater means treatment ranks should be higher
                zTest = treatmentHigher ? -z : z;
                pValue = 1 - normal.CumulativeDistribution(zTest);
                break;
            case AlternativeHypothesisType.Less:
                zTest = treatmentHigher ? z : -z;
                pValue = 1 - normal.CumulativeDistribution(zTest);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(alternativeType));
        }

        // Effect size: rank-biserial correlation
        var effectSize = 1 - 2 * u / ((double)n1 * n2);

        // Confidence interval for the effect size using normal approximation
        var zCritical = alternativeType == AlternativeHypothesisType.TwoSided
            ? normal.InverseCumulativeDistribution(1 - alpha / 2)
            : normal.InverseCumulativeDistribution(1 - alpha);

        // Approximate SE for rank-biserial correlation
        var seEffect = Math.Sqrt((1 - effectSize * effectSize) / (n1 + n2 - 2));
        var marginOfError = zCritical * seEffect;

        double ciLower, ciUpper;
        switch (alternativeType)
        {
            case AlternativeHypothesisType.TwoSided:
                ciLower = Math.Max(-1, effectSize - marginOfError);
                ciUpper = Math.Min(1, effectSize + marginOfError);
                break;
            case AlternativeHypothesisType.Greater:
                ciLower = Math.Max(-1, effectSize - marginOfError);
                ciUpper = 1.0;
                break;
            case AlternativeHypothesisType.Less:
                ciLower = -1.0;
                ciUpper = Math.Min(1, effectSize + marginOfError);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(alternativeType));
        }

        return new StatisticalTestResult
        {
            TestName = Name,
            TestStatistic = u,
            PValue = pValue,
            Alpha = alpha,
            ConfidenceIntervalLower = ciLower,
            ConfidenceIntervalUpper = ciUpper,
            PointEstimate = effectSize,
            DegreesOfFreedom = null, // Non-parametric test
            AlternativeType = alternativeType,
            SampleSizes = new Dictionary<string, int>
            {
                ["control"] = n1,
                ["treatment"] = n2
            },
            Details = new Dictionary<string, object>
            {
                ["u1"] = u1,
                ["u2"] = u2,
                ["z_statistic"] = z,
                ["control_rank_sum"] = r1,
                ["treatment_rank_sum"] = r2,
                ["rank_biserial_correlation"] = effectSize
            }
        };
    }

    private static double[] AssignRanks(List<(double Value, int Group)> sorted)
    {
        var n = sorted.Count;
        var ranks = new double[n];
        var i = 0;

        while (i < n)
        {
            var j = i;
            // Find all tied values
            while (j < n && Math.Abs(sorted[j].Value - sorted[i].Value) < 1e-10)
                j++;

            // Average rank for ties
            var avgRank = (i + 1.0 + j) / 2;
            for (var k = i; k < j; k++)
                ranks[k] = avgRank;

            i = j;
        }

        return ranks;
    }

    private static double CalculateTieCorrection(List<(double Value, int Group)> sorted)
    {
        var correction = 0.0;
        var i = 0;

        while (i < sorted.Count)
        {
            var j = i;
            while (j < sorted.Count && Math.Abs(sorted[j].Value - sorted[i].Value) < 1e-10)
                j++;

            var tieSize = j - i;
            if (tieSize > 1)
            {
                correction += (Math.Pow(tieSize, 3) - tieSize) / 12;
            }

            i = j;
        }

        return correction;
    }
}
