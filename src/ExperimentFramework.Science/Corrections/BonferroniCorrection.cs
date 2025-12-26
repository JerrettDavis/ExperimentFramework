namespace ExperimentFramework.Science.Corrections;

/// <summary>
/// Bonferroni correction for multiple comparisons.
/// </summary>
/// <remarks>
/// <para>
/// The Bonferroni correction is the simplest and most conservative method.
/// It divides the significance level by the number of tests.
/// </para>
/// <para>
/// Adjusted threshold: α' = α / m
/// Adjusted p-value: p' = min(p * m, 1)
/// </para>
/// <para>
/// Pros:
/// <list type="bullet">
/// <item><description>Simple to understand and apply</description></item>
/// <item><description>Strong control of family-wise error rate (FWER)</description></item>
/// <item><description>Valid for any dependency structure between tests</description></item>
/// </list>
/// </para>
/// <para>
/// Cons:
/// <list type="bullet">
/// <item><description>Very conservative, low power when many tests</description></item>
/// <item><description>May miss true effects (high Type II error)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class BonferroniCorrection : IMultipleComparisonCorrection
{
    /// <summary>
    /// The singleton instance.
    /// </summary>
    public static BonferroniCorrection Instance { get; } = new();

    /// <inheritdoc />
    public string Name => "Bonferroni Correction";

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

        for (var i = 0; i < m; i++)
        {
            adjusted[i] = Math.Min(pValues[i] * m, 1.0);
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

        var adjustedAlpha = alpha / numberOfTests;
        var thresholds = new double[numberOfTests];
        Array.Fill(thresholds, adjustedAlpha);

        return thresholds;
    }

    /// <inheritdoc />
    public IReadOnlyList<bool> DetermineSignificance(IReadOnlyList<double> pValues, double alpha)
    {
        ArgumentNullException.ThrowIfNull(pValues);

        if (pValues.Count == 0)
            return Array.Empty<bool>();

        var adjustedAlpha = alpha / pValues.Count;
        var significant = new bool[pValues.Count];

        for (var i = 0; i < pValues.Count; i++)
        {
            significant[i] = pValues[i] < adjustedAlpha;
        }

        return significant;
    }
}
