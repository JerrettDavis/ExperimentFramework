namespace ExperimentFramework.Science.Corrections;

/// <summary>
/// Benjamini-Hochberg procedure for controlling False Discovery Rate (FDR).
/// </summary>
/// <remarks>
/// <para>
/// The Benjamini-Hochberg procedure controls the expected proportion of false
/// positives among rejected hypotheses (False Discovery Rate), rather than
/// the probability of any false positive (FWER).
/// </para>
/// <para>
/// Procedure:
/// <list type="number">
/// <item><description>Order p-values from smallest to largest: p(1) ≤ p(2) ≤ ... ≤ p(m)</description></item>
/// <item><description>Find the largest k such that p(k) ≤ (k/m) * α</description></item>
/// <item><description>Reject all hypotheses H(1), ..., H(k)</description></item>
/// </list>
/// </para>
/// <para>
/// FDR is more appropriate than FWER when:
/// <list type="bullet">
/// <item><description>Many tests are performed (e.g., genomics)</description></item>
/// <item><description>Some false positives are acceptable</description></item>
/// <item><description>Higher power is needed</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class BenjaminiHochbergCorrection : IMultipleComparisonCorrection
{
    /// <summary>
    /// The singleton instance.
    /// </summary>
    public static BenjaminiHochbergCorrection Instance { get; } = new();

    /// <inheritdoc />
    public string Name => "Benjamini-Hochberg Procedure";

    /// <inheritdoc />
    public string ControlsFor => "False Discovery Rate (FDR)";

    /// <inheritdoc />
    public IReadOnlyList<double> AdjustPValues(IReadOnlyList<double> pValues)
    {
        ArgumentNullException.ThrowIfNull(pValues);

        if (pValues.Count == 0)
            return Array.Empty<double>();

        var m = pValues.Count;
        var adjusted = new double[m];

        // Create sorted indices
        var indices = Enumerable.Range(0, m)
            .OrderBy(i => pValues[i])
            .ToArray();

        // Calculate adjusted p-values (q-values)
        // Work backwards to enforce monotonicity
        var sortedAdjusted = new double[m];
        var minSoFar = 1.0;

        for (var rank = m - 1; rank >= 0; rank--)
        {
            var originalIndex = indices[rank];
            var adjustedP = pValues[originalIndex] * m / (rank + 1);
            adjustedP = Math.Min(adjustedP, 1.0);

            // Enforce monotonicity: adjusted p-values cannot increase going down
            minSoFar = Math.Min(minSoFar, adjustedP);
            sortedAdjusted[rank] = minSoFar;
        }

        // Map back to original order
        for (var rank = 0; rank < m; rank++)
        {
            adjusted[indices[rank]] = sortedAdjusted[rank];
        }

        return adjusted;
    }

    /// <inheritdoc />
    public IReadOnlyList<double> AdjustThresholds(double alpha, int numberOfTests)
    {
        if (alpha is <= 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(alpha), "Alpha must be between 0 and 1.");
        if (numberOfTests < 1)
            throw new ArgumentOutOfRangeException(nameof(numberOfTests), "Must have at least 1 test.");

        var thresholds = new double[numberOfTests];
        for (var i = 0; i < numberOfTests; i++)
        {
            // For the i-th smallest p-value (0-indexed, so use i+1)
            thresholds[i] = alpha * (i + 1) / numberOfTests;
        }

        return thresholds;
    }

    /// <inheritdoc />
    public IReadOnlyList<bool> DetermineSignificance(IReadOnlyList<double> pValues, double alpha)
    {
        ArgumentNullException.ThrowIfNull(pValues);

        if (pValues.Count == 0)
            return Array.Empty<bool>();

        var m = pValues.Count;
        var significant = new bool[m];

        // Create sorted indices
        var indices = Enumerable.Range(0, m)
            .OrderBy(i => pValues[i])
            .ToArray();

        // Find the largest k where p(k) ≤ (k/m) * α
        var largestK = -1;
        for (var rank = 0; rank < m; rank++)
        {
            var originalIndex = indices[rank];
            var threshold = alpha * (rank + 1) / m;

            if (pValues[originalIndex] <= threshold)
            {
                largestK = rank;
            }
        }

        if (largestK < 0)
            return significant;
        
        // Reject all hypotheses up to and including k
        for (var rank = 0; rank <= largestK; rank++)
        {
            significant[indices[rank]] = true;
        }

        return significant;
    }
}
