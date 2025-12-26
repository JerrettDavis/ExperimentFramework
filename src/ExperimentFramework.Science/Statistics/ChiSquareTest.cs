using ExperimentFramework.Science.Models.Results;
using MathNet.Numerics.Distributions;

namespace ExperimentFramework.Science.Statistics;

/// <summary>
/// Chi-square test for comparing proportions between two groups.
/// </summary>
/// <remarks>
/// <para>
/// This test is used to determine if there is a significant association between
/// the group (control/treatment) and a binary outcome (success/failure).
/// </para>
/// <para>
/// Assumptions:
/// <list type="bullet">
/// <item><description>Observations are independent</description></item>
/// <item><description>Expected frequencies in each cell ≥ 5</description></item>
/// </list>
/// </para>
/// <para>
/// For binary outcomes, the data should be encoded as 1.0 for success and 0.0 for failure.
/// </para>
/// </remarks>
public sealed class ChiSquareTest : IStatisticalTest
{
    /// <summary>
    /// The singleton instance of the chi-square test.
    /// </summary>
    public static ChiSquareTest Instance { get; } = new();

    /// <inheritdoc />
    public string Name => "Chi-Square Test for Independence";

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

        // Count successes and failures in each group
        var controlSuccesses = controlData.Count(x => x >= 0.5);
        var controlFailures = controlData.Count - controlSuccesses;
        var treatmentSuccesses = treatmentData.Count(x => x >= 0.5);
        var treatmentFailures = treatmentData.Count - treatmentSuccesses;

        var n1 = controlData.Count;
        var n2 = treatmentData.Count;
        var n = n1 + n2;

        // 2x2 contingency table:
        //                 Control  Treatment   Total
        // Success           a         b        a+b
        // Failure           c         d        c+d
        // Total            a+c       b+d        n
        double a = controlSuccesses;
        double b = treatmentSuccesses;
        double c = controlFailures;
        double d = treatmentFailures;

        // Expected frequencies
        var rowSuccess = a + b;
        var rowFailure = c + d;
        var colControl = a + c;
        var colTreatment = b + d;

        var expectedA = rowSuccess * colControl / n;
        var expectedB = rowSuccess * colTreatment / n;
        var expectedC = rowFailure * colControl / n;
        var expectedD = rowFailure * colTreatment / n;

        // Chi-square statistic with Yates' continuity correction for 2x2 tables
        var chiSquare = CalculateChiSquareWithYates(a, b, c, d, n);

        // Degrees of freedom for 2x2 table = 1
        const int df = 1;

        // P-value from chi-square distribution
        var chiDist = new ChiSquared(df);
        var pValue = 1 - chiDist.CumulativeDistribution(chiSquare);

        // For one-sided tests, halve the p-value and check direction
        if (alternativeType != AlternativeHypothesisType.TwoSided)
        {
            var proportionControl = a / n1;
            var proportionTreatment = b / n2;
            var diff = proportionTreatment - proportionControl;

            pValue = alternativeType switch
            {
                AlternativeHypothesisType.Greater when diff <= 0 => 1 - pValue / 2,
                AlternativeHypothesisType.Less when diff >= 0 => 1 - pValue / 2,
                _ => pValue / 2
            };
        }

        // Calculate proportions and confidence interval for difference
        var p1 = a / n1;
        var p2 = b / n2;
        var pointEstimate = p2 - p1;

        // Standard error for difference in proportions
        var se = Math.Sqrt(p1 * (1 - p1) / n1 + p2 * (1 - p2) / n2);

        // Z critical value
        var normal = new Normal(0, 1);
        var zCritical = alternativeType == AlternativeHypothesisType.TwoSided
            ? normal.InverseCumulativeDistribution(1 - alpha / 2)
            : normal.InverseCumulativeDistribution(1 - alpha);

        var marginOfError = zCritical * se;

        double ciLower, ciUpper;
        switch (alternativeType)
        {
            case AlternativeHypothesisType.TwoSided:
                ciLower = pointEstimate - marginOfError;
                ciUpper = pointEstimate + marginOfError;
                break;
            case AlternativeHypothesisType.Greater:
                ciLower = pointEstimate - marginOfError;
                ciUpper = 1.0; // Max possible difference
                break;
            case AlternativeHypothesisType.Less:
                ciLower = -1.0; // Min possible difference
                ciUpper = pointEstimate + marginOfError;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(alternativeType));
        }

        return new StatisticalTestResult
        {
            TestName = Name,
            TestStatistic = chiSquare,
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
                ["control_successes"] = controlSuccesses,
                ["control_failures"] = controlFailures,
                ["treatment_successes"] = treatmentSuccesses,
                ["treatment_failures"] = treatmentFailures,
                ["control_proportion"] = p1,
                ["treatment_proportion"] = p2,
                ["expected_control_success"] = expectedA,
                ["expected_treatment_success"] = expectedB,
                ["expected_control_failure"] = expectedC,
                ["expected_treatment_failure"] = expectedD
            }
        };
    }

    private static double CalculateChiSquareWithYates(double a, double b, double c, double d, double n)
    {
        // Yates' continuity correction
        var numerator = Math.Pow(Math.Abs(a * d - b * c) - n / 2, 2) * n;
        var denominator = (a + b) * (c + d) * (a + c) * (b + d);

        if (denominator < double.Epsilon)
            return 0;

        return numerator / denominator;
    }
}
