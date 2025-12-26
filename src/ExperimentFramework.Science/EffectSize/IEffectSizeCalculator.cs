using ExperimentFramework.Science.Reporting;

namespace ExperimentFramework.Science.EffectSize;

/// <summary>
/// Defines the contract for calculating effect sizes.
/// </summary>
/// <remarks>
/// Effect sizes quantify the magnitude of a treatment effect, independent of sample size.
/// They are essential for:
/// <list type="bullet">
/// <item><description>Power analysis and sample size planning</description></item>
/// <item><description>Meta-analysis and cross-study comparisons</description></item>
/// <item><description>Practical significance assessment</description></item>
/// </list>
/// </remarks>
public interface IEffectSizeCalculator
{
    /// <summary>
    /// Gets the name of this effect size measure.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Calculates the effect size from two samples.
    /// </summary>
    /// <param name="controlData">The control group data.</param>
    /// <param name="treatmentData">The treatment group data.</param>
    /// <returns>The effect size result with magnitude interpretation.</returns>
    EffectSizeResult Calculate(IReadOnlyList<double> controlData, IReadOnlyList<double> treatmentData);
}

/// <summary>
/// Defines the contract for calculating effect sizes for binary outcomes.
/// </summary>
public interface IBinaryEffectSizeCalculator
{
    /// <summary>
    /// Gets the name of this effect size measure.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Calculates the effect size from contingency table data.
    /// </summary>
    /// <param name="controlSuccesses">Number of successes in control group.</param>
    /// <param name="controlTotal">Total observations in control group.</param>
    /// <param name="treatmentSuccesses">Number of successes in treatment group.</param>
    /// <param name="treatmentTotal">Total observations in treatment group.</param>
    /// <returns>The effect size result.</returns>
    EffectSizeResult Calculate(int controlSuccesses, int controlTotal, int treatmentSuccesses, int treatmentTotal);
}

/// <summary>
/// Extension methods for effect size interpretation.
/// </summary>
public static class EffectSizeExtensions
{
    /// <summary>
    /// Interprets Cohen's d magnitude following Cohen's conventions.
    /// </summary>
    /// <param name="cohensD">The absolute value of Cohen's d.</param>
    /// <returns>The magnitude interpretation.</returns>
    public static EffectSizeMagnitude InterpretCohensD(double cohensD)
    {
        var d = Math.Abs(cohensD);
        return d switch
        {
            < 0.2 => EffectSizeMagnitude.Negligible,
            < 0.5 => EffectSizeMagnitude.Small,
            < 0.8 => EffectSizeMagnitude.Medium,
            _ => EffectSizeMagnitude.Large
        };
    }

    /// <summary>
    /// Interprets odds ratio magnitude.
    /// </summary>
    /// <param name="oddsRatio">The odds ratio.</param>
    /// <returns>The magnitude interpretation.</returns>
    public static EffectSizeMagnitude InterpretOddsRatio(double oddsRatio)
    {
        // Convert to a symmetric scale (1 is no effect)
        var magnitude = oddsRatio > 1 ? oddsRatio : 1 / oddsRatio;
        return magnitude switch
        {
            < 1.5 => EffectSizeMagnitude.Negligible,
            < 2.0 => EffectSizeMagnitude.Small,
            < 3.0 => EffectSizeMagnitude.Medium,
            _ => EffectSizeMagnitude.Large
        };
    }

    /// <summary>
    /// Interprets relative risk magnitude.
    /// </summary>
    /// <param name="relativeRisk">The relative risk.</param>
    /// <returns>The magnitude interpretation.</returns>
    public static EffectSizeMagnitude InterpretRelativeRisk(double relativeRisk)
    {
        var magnitude = relativeRisk > 1 ? relativeRisk : 1 / relativeRisk;
        return magnitude switch
        {
            < 1.25 => EffectSizeMagnitude.Negligible,
            < 1.5 => EffectSizeMagnitude.Small,
            < 2.0 => EffectSizeMagnitude.Medium,
            _ => EffectSizeMagnitude.Large
        };
    }
}
