namespace ExperimentFramework.Science.Corrections;

/// <summary>
/// Holm-Bonferroni (Holm's step-down) correction for multiple comparisons.
/// </summary>
/// <remarks>
/// <para>
/// The Holm-Bonferroni method is uniformly more powerful than Bonferroni
/// while still controlling the family-wise error rate (FWER).
/// </para>
/// <para>
/// Procedure:
/// <list type="number">
/// <item><description>Order p-values from smallest to largest</description></item>
/// <item><description>For the i-th smallest p-value, compare to α/(m-i+1)</description></item>
/// <item><description>Reject all hypotheses up to the first non-rejected one</description></item>
/// </list>
/// </para>
/// <para>
/// Adjusted p-values: p'(i) = max(j ≤ i) { min((m-j+1) * p(j), 1) }
/// </para>
/// </remarks>
public sealed class HolmBonferroniCorrection : IMultipleComparisonCorrection
{
    /// <summary>
    /// The singleton instance.
    /// </summary>
    public static HolmBonferroniCorrection Instance { get; } = new();

    /// <inheritdoc />
    public string Name => "Holm-Bonferroni Correction";

    /// <inheritdoc />
    public string ControlsFor => "Family-wise Error Rate (FWER)";

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

        // Calculate adjusted p-values in sorted order
        var sortedAdjusted = new double[m];
        var maxSoFar = 0.0;

        for (var rank = 0; rank < m; rank++)
        {
            var originalIndex = indices[rank];
            var multiplier = m - rank;
            var adjustedP = Math.Min(pValues[originalIndex] * multiplier, 1.0);

            // Enforce monotonicity: adjusted p-values cannot decrease
            maxSoFar = Math.Max(maxSoFar, adjustedP);
            sortedAdjusted[rank] = maxSoFar;
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
            // For the i-th smallest p-value (0-indexed)
            thresholds[i] = alpha / (numberOfTests - i);
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

        // Step-down procedure
        for (var rank = 0; rank < m; rank++)
        {
            var originalIndex = indices[rank];
            var threshold = alpha / (m - rank);

            if (pValues[originalIndex] >= threshold)
            {
                // Stop: this and all subsequent tests are not significant
                break;
            }

            significant[originalIndex] = true;
        }

        return significant;
    }
}
