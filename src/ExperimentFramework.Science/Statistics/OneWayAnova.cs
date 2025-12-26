using ExperimentFramework.Science.Models.Results;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;

namespace ExperimentFramework.Science.Statistics;

/// <summary>
/// One-way Analysis of Variance (ANOVA) for comparing means of multiple groups.
/// </summary>
/// <remarks>
/// <para>
/// ANOVA tests whether there are statistically significant differences between the means
/// of three or more independent groups. It does not identify which specific groups differ.
/// </para>
/// <para>
/// Assumptions:
/// <list type="bullet">
/// <item><description>Observations are independent</description></item>
/// <item><description>Each group is approximately normally distributed</description></item>
/// <item><description>Groups have approximately equal variances (homoscedasticity)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class OneWayAnova : IMultiGroupStatisticalTest
{
    /// <summary>
    /// The singleton instance of the one-way ANOVA test.
    /// </summary>
    public static OneWayAnova Instance { get; } = new();

    /// <inheritdoc />
    public string Name => "One-Way ANOVA";

    /// <inheritdoc />
    public StatisticalTestResult Perform(
        IReadOnlyDictionary<string, IReadOnlyList<double>> groups,
        double alpha = 0.05)
    {
        ArgumentNullException.ThrowIfNull(groups);

        if (groups.Count < 2)
            throw new ArgumentException("At least 2 groups are required for ANOVA.", nameof(groups));
        if (groups.Values.Any(g => g.Count < 1))
            throw new ArgumentException("Each group must have at least 1 observation.", nameof(groups));
        if (alpha is <= 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(alpha), "Alpha must be between 0 and 1 (exclusive).");

        var k = groups.Count; // Number of groups
        var n = groups.Values.Sum(g => g.Count); // Total observations

        // Calculate grand mean
        var allValues = groups.Values.SelectMany(g => g).ToList();
        var grandMean = allValues.Mean();

        // Calculate group means
        var groupMeans = groups.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Mean());

        // Calculate Between-Group Sum of Squares (SSB)
        var ssb = groups.Sum(kvp =>
            kvp.Value.Count * Math.Pow(groupMeans[kvp.Key] - grandMean, 2));

        // Calculate Within-Group Sum of Squares (SSW)
        var ssw = groups.Sum(kvp =>
            kvp.Value.Sum(x => Math.Pow(x - groupMeans[kvp.Key], 2)));

        // Total Sum of Squares (SST)
        var sst = ssb + ssw;

        // Degrees of freedom
        var dfBetween = k - 1;
        var dfWithin = n - k;
        var dfTotal = n - 1;

        // Mean squares
        var msBetween = ssb / dfBetween;
        var msWithin = dfWithin > 0 ? ssw / dfWithin : 0;

        // F-statistic
        var fStatistic = msWithin > 0 ? msBetween / msWithin : double.PositiveInfinity;

        // P-value from F-distribution
        var fDist = new FisherSnedecor(dfBetween, dfWithin);
        var pValue = 1 - fDist.CumulativeDistribution(fStatistic);

        // Effect size: Eta-squared (η²) and Omega-squared (ω²)
        var etaSquared = ssb / sst;
        var omegaSquared = (ssb - dfBetween * msWithin) / (sst + msWithin);

        // Sample sizes
        var sampleSizes = groups.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Count);

        return new StatisticalTestResult
        {
            TestName = Name,
            TestStatistic = fStatistic,
            PValue = pValue,
            Alpha = alpha,
            ConfidenceIntervalLower = 0, // Effect size lower bound (η² is always positive)
            ConfidenceIntervalUpper = etaSquared, // Using η² as the effect size estimate
            PointEstimate = etaSquared,
            DegreesOfFreedom = dfWithin, // Within-group df (error df)
            AlternativeType = AlternativeHypothesisType.TwoSided, // ANOVA is inherently two-sided
            SampleSizes = sampleSizes,
            Details = new Dictionary<string, object>
            {
                ["ss_between"] = ssb,
                ["ss_within"] = ssw,
                ["ss_total"] = sst,
                ["df_between"] = dfBetween,
                ["df_within"] = dfWithin,
                ["ms_between"] = msBetween,
                ["ms_within"] = msWithin,
                ["eta_squared"] = etaSquared,
                ["omega_squared"] = Math.Max(0, omegaSquared), // Can be negative, clip to 0
                ["grand_mean"] = grandMean,
                ["group_means"] = groupMeans,
                ["number_of_groups"] = k
            }
        };
    }
}
