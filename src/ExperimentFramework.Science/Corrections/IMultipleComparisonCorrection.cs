namespace ExperimentFramework.Science.Corrections;

/// <summary>
/// Defines the contract for multiple comparison correction methods.
/// </summary>
/// <remarks>
/// <para>
/// When testing multiple hypotheses, the probability of at least one false positive
/// increases. Multiple comparison corrections adjust p-values or significance thresholds
/// to control the family-wise error rate (FWER) or false discovery rate (FDR).
/// </para>
/// <para>
/// Common scenarios requiring correction:
/// <list type="bullet">
/// <item><description>Testing multiple endpoints</description></item>
/// <item><description>Comparing multiple treatment conditions</description></item>
/// <item><description>Interim analyses (sequential testing)</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IMultipleComparisonCorrection
{
    /// <summary>
    /// Gets the name of this correction method.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a description of what this method controls (FWER, FDR, etc.).
    /// </summary>
    string ControlsFor { get; }

    /// <summary>
    /// Adjusts p-values for multiple comparisons.
    /// </summary>
    /// <param name="pValues">The raw p-values to adjust.</param>
    /// <returns>The adjusted p-values.</returns>
    IReadOnlyList<double> AdjustPValues(IReadOnlyList<double> pValues);

    /// <summary>
    /// Calculates adjusted significance thresholds for each test.
    /// </summary>
    /// <param name="alpha">The overall significance level.</param>
    /// <param name="numberOfTests">The number of tests being performed.</param>
    /// <returns>The adjusted significance thresholds for each test.</returns>
    IReadOnlyList<double> AdjustThresholds(double alpha, int numberOfTests);

    /// <summary>
    /// Determines which hypotheses are significant after correction.
    /// </summary>
    /// <param name="pValues">The raw p-values.</param>
    /// <param name="alpha">The overall significance level.</param>
    /// <returns>Boolean array indicating which tests are significant.</returns>
    IReadOnlyList<bool> DetermineSignificance(IReadOnlyList<double> pValues, double alpha);
}

/// <summary>
/// Result of applying multiple comparison correction.
/// </summary>
public sealed class CorrectionResult
{
    /// <summary>
    /// Gets the correction method used.
    /// </summary>
    public required string Method { get; init; }

    /// <summary>
    /// Gets the original p-values.
    /// </summary>
    public required IReadOnlyList<double> OriginalPValues { get; init; }

    /// <summary>
    /// Gets the adjusted p-values.
    /// </summary>
    public required IReadOnlyList<double> AdjustedPValues { get; init; }

    /// <summary>
    /// Gets which tests are significant after correction.
    /// </summary>
    public required IReadOnlyList<bool> IsSignificant { get; init; }

    /// <summary>
    /// Gets the overall significance level used.
    /// </summary>
    public required double Alpha { get; init; }
}
